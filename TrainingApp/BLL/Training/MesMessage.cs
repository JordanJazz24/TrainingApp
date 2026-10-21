using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TrainingApp.BLL.Training
{
    // Mensajes de error del MES real (spec 017, tabla "Catálogo de mensajes"). El número del
    // enum es el M-n de la spec; M-10 (19973) y M-18 (12901) quedaron fuera (D-118).
    public enum MesMessageId
    {
        ResourceSetupMissing   = 1,   // M-1 · provisional (C-10, D-111)
        LineClearanceNotDone   = 2,   // M-2 · 22501
        LineClearanceStatusNo  = 3,   // M-3 · 22501
        SfcMissingStart        = 4,   // M-4 · 22506 (Start, Complete)
        SfcMissing             = 5,   // M-5 · 12906 (Assemble, DC Collect, Log NC, SFC Split)
        InvalidSfc             = 6,   // M-6 · 22507
        MissingDataCollection  = 7,   // M-7 · 15246
        BarcodeNotCompliant    = 8,   // M-8 · 17150
        RequiredValueMissing   = 9,   // M-9 · 10015
        FieldMustBeNumber      = 11,  // M-11 · 12205
        ValueAboveUpperLimit   = 12,  // M-12 · 15233
        ValueBelowLowerLimit   = 13,  // M-13 · 15232
        SplitQtyMissing        = 14,  // M-14 · 13875
        SplitQtyAboveAvailable = 15,  // M-15 · 13885
        SplitQtyNotPositive    = 16,  // M-16 · 13213
        NoNcCodesLogged        = 17   // M-17 · 18117
    }

    // Dónde lo pinta el MES (spec 017, "Medidas de las superficies"). Nunca es un popup.
    public enum MesSurface
    {
        PodBanner,    // B: banner rojo del POD, entre el header y la franja de búsqueda
        WindowStrip,  // F: franja roja dentro de la ventana abierta
        SplitForm     // S: reemplaza el formulario de SFC Split, con [OK] que cierra la ventana
    }

    // Un mensaje del catálogo ya armado: lo elige la BLL y la página solo lo pinta.
    public class MesError
    {
        public MesError(MesMessageId id, string text, MesSurface surface)
        {
            Id      = id;
            Text    = text;
            Surface = surface;
        }

        public MesMessageId Id { get; }
        public string Text { get; }
        public MesSurface Surface { get; }
    }

    // Plantilla de un mensaje del MES: en inglés, como en producción, con marcadores {nombre}
    // y "(Message n)" al final cuando el MES lo numera. Los textos son literales de la spec.
    public sealed class MesMessage
    {
        private static readonly Regex Marker = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        private static readonly IDictionary<MesMessageId, MesMessage> Catalog = new[]
        {
            new MesMessage(MesMessageId.ResourceSetupMissing, null,
                "Resource Setup has not been done, please perform Resource Setup", MesSurface.PodBanner),
            new MesMessage(MesMessageId.LineClearanceNotDone, 22501,
                "Line Clearance has not been done since the last Resource Setup, please perform Line Clearance",
                MesSurface.PodBanner),
            new MesMessage(MesMessageId.LineClearanceStatusNo, 22501,
                "Line Clearance status is NO, please perform Line Clearance", MesSurface.PodBanner),
            new MesMessage(MesMessageId.SfcMissingStart, 22506, "SFC is missing", MesSurface.PodBanner),
            new MesMessage(MesMessageId.SfcMissing, 12906, "SFC is missing", MesSurface.PodBanner),
            new MesMessage(MesMessageId.InvalidSfc, 22507, "Invalid SFC", MesSurface.PodBanner),
            new MesMessage(MesMessageId.MissingDataCollection, 15246,
                "There are missing required values for data collection parameter(s) {list} for SFC \"{sfc}\"",
                MesSurface.PodBanner),
            new MesMessage(MesMessageId.BarcodeNotCompliant, 17150,
                "2D barcode is not compliant with ISO 15434 format", MesSurface.WindowStrip),
            new MesMessage(MesMessageId.RequiredValueMissing, 10015,
                "Required value \"{param}\" missing; enter a value in the {param} field", MesSurface.WindowStrip),
            new MesMessage(MesMessageId.FieldMustBeNumber, 12205,
                "Field {param} must contain a number", MesSurface.WindowStrip),
            new MesMessage(MesMessageId.ValueAboveUpperLimit, 15233,
                "Value {value} is above the upper limit", MesSurface.WindowStrip),
            new MesMessage(MesMessageId.ValueBelowLowerLimit, 15232,
                "Value {value} is below the lower limit", MesSurface.WindowStrip),
            new MesMessage(MesMessageId.SplitQtyMissing, 13875, "Define Qty to Split", MesSurface.SplitForm),
            new MesMessage(MesMessageId.SplitQtyAboveAvailable, 13885,
                "Requested qty to split {n} is greater than the available SFC qty {m}", MesSurface.SplitForm),
            new MesMessage(MesMessageId.SplitQtyNotPositive, 13213,
                "Quantity must be greater than 0", MesSurface.SplitForm),
            new MesMessage(MesMessageId.NoNcCodesLogged, 18117, "No NC codes logged", MesSurface.WindowStrip)
        }.ToDictionary(m => m.Id);

        private MesMessage(MesMessageId id, int? number, string template, MesSurface surface)
        {
            Id       = id;
            Number   = number;
            Template = template;
            Surface  = surface;
        }

        public MesMessageId Id { get; }

        // null: el MES no lo numera (M-1, provisional).
        public int? Number { get; }

        public string Template { get; }
        public MesSurface Surface { get; }

        public static IEnumerable<MesMessage> All => Catalog.Values;

        public static MesMessage Get(MesMessageId id)
        {
            if (!Catalog.TryGetValue(id, out var message))
                throw new ArgumentOutOfRangeException(nameof(id), id, "Mensaje del MES fuera del catálogo.");
            return message;
        }

        // Atajo para la BLL: el mensaje armado con sus valores.
        public static MesError Error(MesMessageId id, params (string Name, string Value)[] args)
            => Get(id).ToError(args);

        public MesError ToError(params (string Name, string Value)[] args)
            => new MesError(Id, Format(args), Surface);

        // Texto final: cada marcador reemplazado por su valor (tal cual, sin escapar: lo codifica
        // quien lo pinta) y "(Message n)" al final. Un marcador sin valor es un error de código.
        public string Format(params (string Name, string Value)[] args)
        {
            var values = (args ?? new (string, string)[0])
                .ToDictionary(a => a.Name, a => a.Value ?? string.Empty);

            string text = Marker.Replace(Template, m =>
            {
                if (!values.TryGetValue(m.Groups[1].Value, out var value))
                    throw new ArgumentException(
                        "Falta el valor de {" + m.Groups[1].Value + "} para el mensaje " + Id + ".", nameof(args));
                return value;
            });

            return Number.HasValue ? text + " (Message " + Number.Value + ")" : text;
        }
    }
}
