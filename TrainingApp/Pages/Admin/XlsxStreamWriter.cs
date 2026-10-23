using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace TrainingApp.Pages.Admin
{
    // Un formato de número de una columna del .xlsx (el valor es el índice del estilo).
    public enum XlsxFormat
    {
        General = 0,
        Header  = 1,
        Date    = 2,   // dd/MM/yyyy HH:mm
        Time    = 3,   // [h]:mm:ss
        Percent = 4    // 0.0%
    }

    // Una columna: encabezado, ancho en caracteres y formato.
    public sealed class XlsxColumn
    {
        public string Header { get; }
        public double Width { get; }
        public XlsxFormat Format { get; }

        public XlsxColumn(string header, double width, XlsxFormat format = XlsxFormat.General)
        {
            Header = header; Width = width; Format = format;
        }
    }

    // Escribe un .xlsx fila por fila, directo al stream de salida (spec 018, E7, D-148: la
    // descarga de sesiones con todo el historial). El paquete se arma con ZipArchive: el SDK de
    // OpenXML en .NET Framework usa System.IO.Packaging, que guarda cada parte en memoria y pasados
    // ~10 MB la manda a IsolatedStorage (falla en un app pool de IIS sin perfil). Mismo aspecto
    // que ExcelReports con ClosedXML: Calibri 11, encabezado en negrita y fijo, autofiltro, anchos
    // y formatos por columna. Los textos van como cadenas en línea (sin tabla compartida).
    public sealed class XlsxStreamWriter : IDisposable
    {
        private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelNs  = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string PkgRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        private static readonly XmlWriterSettings XmlSettings = new XmlWriterSettings { Encoding = new UTF8Encoding(false) };

        private readonly ZipArchive _zip;
        private readonly List<string> _sheets = new List<string>();
        private readonly List<string> _filters = new List<string>();
        private bool _faulted;

        public XlsxStreamWriter(Stream output)
        {
            _zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        }

        // El código de formato de Excel de cada XlsxFormat (lo usa también ExcelReports con
        // ClosedXML, así las cuatro descargas tienen los mismos formatos); null = general.
        public static string FormatCode(XlsxFormat format)
        {
            switch (format)
            {
                case XlsxFormat.Date:    return "dd/MM/yyyy HH:mm";
                case XlsxFormat.Time:    return "[h]:mm:ss";
                case XlsxFormat.Percent: return "0.0%";
                default:                 return null;
            }
        }

        public void WriteSheet(string name, IList<XlsxColumn> columns, IEnumerable<object[]> rows)
        {
            int index = _sheets.Count + 1;
            var letters = new string[columns.Count];
            for (int c = 0; c < columns.Count; c++) letters[c] = Column(c);
            uint lastRow = 1;

            try
            {
                using (var stream = _zip.CreateEntry("xl/worksheets/sheet" + index + ".xml", CompressionLevel.Fastest).Open())
                using (var xml = XmlWriter.Create(stream, XmlSettings))
                {
                    xml.WriteStartDocument(true);
                    xml.WriteStartElement("worksheet", MainNs);

                    // Encabezado fijo.
                    xml.WriteStartElement("sheetViews", MainNs);
                    xml.WriteStartElement("sheetView", MainNs);
                    if (index == 1) xml.WriteAttributeString("tabSelected", "1");
                    xml.WriteAttributeString("workbookViewId", "0");
                    xml.WriteStartElement("pane", MainNs);
                    xml.WriteAttributeString("ySplit", "1");
                    xml.WriteAttributeString("topLeftCell", "A2");
                    xml.WriteAttributeString("activePane", "bottomLeft");
                    xml.WriteAttributeString("state", "frozen");
                    xml.WriteEndElement();
                    xml.WriteEndElement();
                    xml.WriteEndElement();

                    // Ancho y formato por columna (una celda vacía o escrita después también lo usa).
                    xml.WriteStartElement("cols", MainNs);
                    for (int c = 0; c < columns.Count; c++)
                    {
                        string n = (c + 1).ToString(CultureInfo.InvariantCulture);
                        xml.WriteStartElement("col", MainNs);
                        xml.WriteAttributeString("min", n);
                        xml.WriteAttributeString("max", n);
                        xml.WriteAttributeString("width", columns[c].Width.ToString(CultureInfo.InvariantCulture));
                        if (columns[c].Format != XlsxFormat.General)
                            xml.WriteAttributeString("style", ((int)columns[c].Format).ToString(CultureInfo.InvariantCulture));
                        xml.WriteAttributeString("customWidth", "1");
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement();

                    xml.WriteStartElement("sheetData", MainNs);
                    StartRow(xml, 1);
                    for (int c = 0; c < columns.Count; c++)
                        WriteText(xml, letters[c] + "1", columns[c].Header, XlsxFormat.Header);
                    xml.WriteEndElement();

                    foreach (var row in rows)
                    {
                        lastRow++;
                        StartRow(xml, lastRow);
                        string number = lastRow.ToString(CultureInfo.InvariantCulture);
                        for (int c = 0; c < row.Length; c++)
                            WriteValue(xml, letters[c] + number, row[c], columns[c].Format);
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement(); // sheetData

                    if (lastRow > 1)
                    {
                        xml.WriteStartElement("autoFilter", MainNs);
                        xml.WriteAttributeString("ref", "A1:" + letters[columns.Count - 1] + lastRow.ToString(CultureInfo.InvariantCulture));
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement(); // worksheet
                }
            }
            catch
            {
                // Un archivo a medias no se termina de armar (Dispose no escribe el libro).
                _faulted = true;
                throw;
            }

            // Excel guarda el rango del autofiltro también como nombre oculto de la hoja.
            if (lastRow > 1)
                _filters.Add(string.Format(CultureInfo.InvariantCulture, "{0}|'{1}'!$A$1:${2}${3}",
                    index - 1, name.Replace("'", "''"), letters[columns.Count - 1], lastRow));
            _sheets.Add(name);
        }

        public void Dispose()
        {
            if (!_faulted)
            {
                WritePart("xl/styles.xml", WriteStyles);
                WritePart("xl/workbook.xml", WriteWorkbook);
                WritePart("xl/_rels/workbook.xml.rels", WriteWorkbookRels);
                WritePart("_rels/.rels", WriteRootRels);
                WritePart("[Content_Types].xml", WriteContentTypes);
            }
            _zip.Dispose();
        }

        private void WritePart(string path, Action<XmlWriter> write)
        {
            using (var stream = _zip.CreateEntry(path, CompressionLevel.Fastest).Open())
            using (var xml = XmlWriter.Create(stream, XmlSettings))
            {
                xml.WriteStartDocument(true);
                write(xml);
            }
        }

        private void WriteWorkbook(XmlWriter xml)
        {
            xml.WriteStartElement("workbook", MainNs);
            xml.WriteAttributeString("xmlns", "r", null, RelNs);
            xml.WriteStartElement("sheets", MainNs);
            for (int i = 0; i < _sheets.Count; i++)
            {
                xml.WriteStartElement("sheet", MainNs);
                xml.WriteAttributeString("name", Clean(_sheets[i]));
                xml.WriteAttributeString("sheetId", (i + 1).ToString(CultureInfo.InvariantCulture));
                xml.WriteAttributeString("id", RelNs, "rId" + (i + 1));
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            if (_filters.Count > 0)
            {
                xml.WriteStartElement("definedNames", MainNs);
                foreach (var filter in _filters)
                {
                    var parts = filter.Split(new[] { '|' }, 2);
                    xml.WriteStartElement("definedName", MainNs);
                    xml.WriteAttributeString("name", "_xlnm._FilterDatabase");
                    xml.WriteAttributeString("localSheetId", parts[0]);
                    xml.WriteAttributeString("hidden", "1");
                    xml.WriteString(Clean(parts[1]));
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }

        private void WriteWorkbookRels(XmlWriter xml)
        {
            xml.WriteStartElement("Relationships", PkgRelNs);
            for (int i = 1; i <= _sheets.Count; i++)
                Relationship(xml, "rId" + i, RelNs + "/worksheet", "worksheets/sheet" + i + ".xml");
            Relationship(xml, "rId" + (_sheets.Count + 1), RelNs + "/styles", "styles.xml");
            xml.WriteEndElement();
        }

        private static void WriteRootRels(XmlWriter xml)
        {
            xml.WriteStartElement("Relationships", PkgRelNs);
            Relationship(xml, "rId1", RelNs + "/officeDocument", "xl/workbook.xml");
            xml.WriteEndElement();
        }

        private void WriteContentTypes(XmlWriter xml)
        {
            const string ns = "http://schemas.openxmlformats.org/package/2006/content-types";
            const string sheetml = "application/vnd.openxmlformats-officedocument.spreadsheetml.";
            xml.WriteStartElement("Types", ns);
            ContentType(xml, ns, "Default", "Extension", "rels", "application/vnd.openxmlformats-package.relationships+xml");
            ContentType(xml, ns, "Default", "Extension", "xml", "application/xml");
            ContentType(xml, ns, "Override", "PartName", "/xl/workbook.xml", sheetml + "sheet.main+xml");
            ContentType(xml, ns, "Override", "PartName", "/xl/styles.xml", sheetml + "styles+xml");
            for (int i = 1; i <= _sheets.Count; i++)
                ContentType(xml, ns, "Override", "PartName", "/xl/worksheets/sheet" + i + ".xml", sheetml + "worksheet+xml");
            xml.WriteEndElement();
        }

        // Los estilos en el orden de XlsxFormat (s = el valor del enum). Calibri 11, como ClosedXML.
        private static void WriteStyles(XmlWriter xml)
        {
            xml.WriteStartElement("styleSheet", MainNs);

            xml.WriteStartElement("numFmts", MainNs);
            xml.WriteAttributeString("count", "3");
            NumberFormat(xml, 164, FormatCode(XlsxFormat.Date));
            NumberFormat(xml, 165, FormatCode(XlsxFormat.Time));
            NumberFormat(xml, 166, FormatCode(XlsxFormat.Percent));
            xml.WriteEndElement();

            xml.WriteStartElement("fonts", MainNs);
            xml.WriteAttributeString("count", "2");
            Font(xml, bold: false);
            Font(xml, bold: true);
            xml.WriteEndElement();

            xml.WriteStartElement("fills", MainNs);
            xml.WriteAttributeString("count", "2");
            foreach (var pattern in new[] { "none", "gray125" })
            {
                xml.WriteStartElement("fill", MainNs);
                xml.WriteStartElement("patternFill", MainNs);
                xml.WriteAttributeString("patternType", pattern);
                xml.WriteEndElement();
                xml.WriteEndElement();
            }
            xml.WriteEndElement();

            xml.WriteStartElement("borders", MainNs);
            xml.WriteAttributeString("count", "1");
            xml.WriteStartElement("border", MainNs);
            foreach (var side in new[] { "left", "right", "top", "bottom", "diagonal" })
                xml.WriteElementString(side, MainNs, string.Empty);
            xml.WriteEndElement();
            xml.WriteEndElement();

            xml.WriteStartElement("cellStyleXfs", MainNs);
            xml.WriteAttributeString("count", "1");
            CellFormat(xml, 0, 0, inStyle: true);
            xml.WriteEndElement();

            xml.WriteStartElement("cellXfs", MainNs);
            xml.WriteAttributeString("count", "5");
            CellFormat(xml, 0, 0);     // General
            CellFormat(xml, 0, 1);     // Header
            CellFormat(xml, 164, 0);   // Date
            CellFormat(xml, 165, 0);   // Time
            CellFormat(xml, 166, 0);   // Percent
            xml.WriteEndElement();

            xml.WriteStartElement("cellStyles", MainNs);
            xml.WriteAttributeString("count", "1");
            xml.WriteStartElement("cellStyle", MainNs);
            xml.WriteAttributeString("name", "Normal");
            xml.WriteAttributeString("xfId", "0");
            xml.WriteAttributeString("builtinId", "0");
            xml.WriteEndElement();
            xml.WriteEndElement();

            xml.WriteEndElement();
        }

        private static void NumberFormat(XmlWriter xml, int id, string code)
        {
            xml.WriteStartElement("numFmt", MainNs);
            xml.WriteAttributeString("numFmtId", id.ToString(CultureInfo.InvariantCulture));
            xml.WriteAttributeString("formatCode", code);
            xml.WriteEndElement();
        }

        private static void Font(XmlWriter xml, bool bold)
        {
            xml.WriteStartElement("font", MainNs);
            if (bold) xml.WriteElementString("b", MainNs, string.Empty);
            Attribute(xml, "sz", "val", "11");
            Attribute(xml, "name", "val", "Calibri");
            Attribute(xml, "family", "val", "2");
            xml.WriteEndElement();
        }

        private static void CellFormat(XmlWriter xml, int numberFormat, int font, bool inStyle = false)
        {
            xml.WriteStartElement("xf", MainNs);
            xml.WriteAttributeString("numFmtId", numberFormat.ToString(CultureInfo.InvariantCulture));
            xml.WriteAttributeString("fontId", font.ToString(CultureInfo.InvariantCulture));
            xml.WriteAttributeString("fillId", "0");
            xml.WriteAttributeString("borderId", "0");
            if (!inStyle)
            {
                xml.WriteAttributeString("xfId", "0");
                if (numberFormat != 0) xml.WriteAttributeString("applyNumberFormat", "1");
                if (font != 0) xml.WriteAttributeString("applyFont", "1");
            }
            xml.WriteEndElement();
        }

        private static void Attribute(XmlWriter xml, string element, string attribute, string value)
        {
            xml.WriteStartElement(element, MainNs);
            xml.WriteAttributeString(attribute, value);
            xml.WriteEndElement();
        }

        private static void Relationship(XmlWriter xml, string id, string type, string target)
        {
            xml.WriteStartElement("Relationship", PkgRelNs);
            xml.WriteAttributeString("Id", id);
            xml.WriteAttributeString("Type", type);
            xml.WriteAttributeString("Target", target);
            xml.WriteEndElement();
        }

        private static void ContentType(XmlWriter xml, string ns, string element, string key, string value, string type)
        {
            xml.WriteStartElement(element, ns);
            xml.WriteAttributeString(key, value);
            xml.WriteAttributeString("ContentType", type);
            xml.WriteEndElement();
        }

        private static void StartRow(XmlWriter xml, uint index)
        {
            xml.WriteStartElement("row", MainNs);
            xml.WriteAttributeString("r", index.ToString(CultureInfo.InvariantCulture));
        }

        // Una celda vacía (null) o un número que no es finito no se escriben.
        private static void WriteValue(XmlWriter xml, string reference, object value, XlsxFormat format)
        {
            switch (value)
            {
                case null:        return;
                case string s:    WriteText(xml, reference, s, format); return;
                case int i:       WriteNumber(xml, reference, i, format); return;
                case decimal d:   WriteNumber(xml, reference, (double)d, format); return;
                case double d:    WriteNumber(xml, reference, d, format); return;
                case DateTime dt: WriteNumber(xml, reference, dt.ToOADate(), format); return;
                case TimeSpan ts: WriteNumber(xml, reference, ts.TotalDays, format); return;
                default:          WriteText(xml, reference, value.ToString(), format); return;
            }
        }

        private static void WriteText(XmlWriter xml, string reference, string text, XlsxFormat format)
        {
            xml.WriteStartElement("c", MainNs);
            xml.WriteAttributeString("r", reference);
            if (format != XlsxFormat.General) xml.WriteAttributeString("s", ((int)format).ToString(CultureInfo.InvariantCulture));
            xml.WriteAttributeString("t", "inlineStr");
            xml.WriteStartElement("is", MainNs);
            xml.WriteStartElement("t", MainNs);
            xml.WriteAttributeString("xml", "space", null, "preserve");
            xml.WriteString(Clean(text));
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndElement();
        }

        private static void WriteNumber(XmlWriter xml, string reference, double number, XlsxFormat format)
        {
            if (double.IsNaN(number) || double.IsInfinity(number)) return;
            xml.WriteStartElement("c", MainNs);
            xml.WriteAttributeString("r", reference);
            if (format != XlsxFormat.General) xml.WriteAttributeString("s", ((int)format).ToString(CultureInfo.InvariantCulture));
            xml.WriteElementString("v", MainNs, number.ToString("R", CultureInfo.InvariantCulture));
            xml.WriteEndElement();
        }

        // Quita los caracteres que XML no admite (de control, sustitutos sueltos): un nombre con
        // uno de ellos haría fallar toda la descarga.
        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            StringBuilder clean = null;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                bool valid;
                if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    clean?.Append(ch).Append(text[i + 1]);
                    i++;
                    continue;
                }
                valid = XmlConvert.IsXmlChar(ch);
                if (!valid && clean == null) clean = new StringBuilder(text, 0, i, text.Length);
                else if (valid) clean?.Append(ch);
            }
            return clean?.ToString() ?? text;
        }

        // 0 → A, 25 → Z, 26 → AA.
        private static string Column(int index)
        {
            string name = string.Empty;
            for (int n = index + 1; n > 0; n = (n - 1) / 26)
                name = (char)('A' + (n - 1) % 26) + name;
            return name;
        }
    }
}
