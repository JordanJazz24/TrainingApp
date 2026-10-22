using System;
using System.IO;
using System.Text;

namespace TrainingApp.Shared
{
    /// <summary>
    /// Registro en archivo de la app (AUD-28, Fase 9 E2): errores no controlados
    /// (Global.Application_Error) y logins fallidos y bloqueos (AuthService). Un archivo por
    /// día en App_Data/Logs (la identidad del App Pool necesita permiso de escritura:
    /// docs/deploy.md); los de más de RetentionDays días se borran. Nunca contraseñas ni datos
    /// de formularios. Si no puede escribir, no hace nada: el registro no puede tumbar un
    /// request.
    /// </summary>
    public static class AppLog
    {
        private const int RetentionDays = 60;

        private static readonly object FileLock = new object();

        public static void Write(string category, string message)
        {
            try
            {
                // App_Data de la aplicación web; fuera de ASP.NET (tests) no hay y no se registra.
                var dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
                if (string.IsNullOrEmpty(dataDirectory)) return;

                var now = DateTime.UtcNow;
                var folder = Path.Combine(dataDirectory, "Logs");
                var file = Path.Combine(folder, now.ToString("yyyy-MM-dd") + ".log");
                var line = now.ToString("yyyy-MM-dd HH:mm:ss") + "Z [" + category + "] " + message + Environment.NewLine;

                lock (FileLock)
                {
                    Directory.CreateDirectory(folder);
                    if (!File.Exists(file))
                        DeleteOldFiles(folder, now);
                    File.AppendAllText(file, line, Encoding.UTF8);
                }
            }
            catch
            {
                // Sin permiso o disco lleno: se pierde la línea, no el request.
            }
        }

        // Una vez por día (al crear el archivo del día): borra los de más de RetentionDays días.
        private static void DeleteOldFiles(string folder, DateTime nowUtc)
        {
            foreach (var old in Directory.GetFiles(folder, "*.log"))
                if (File.GetLastWriteTimeUtc(old) < nowUtc.AddDays(-RetentionDays))
                    File.Delete(old);
        }

        /// <summary>
        /// Un texto que puede traer lo que escribió el cliente (un Code1, una URL, el mensaje de
        /// una excepción) en una sola línea y corto: sin saltos ni caracteres de control, no
        /// puede inventar líneas en el log.
        /// </summary>
        public static string Clean(string value, int maxLength = 50)
        {
            if (string.IsNullOrEmpty(value)) return "(vacío)";
            var text = new StringBuilder(value.Length);
            foreach (var c in value)
                text.Append(char.IsControl(c) || c == (char)0x2028 || c == (char)0x2029 ? ' ' : c);
            var oneLine = text.ToString().Trim();
            return oneLine.Length > maxLength ? oneLine.Substring(0, maxLength) + "…" : oneLine;
        }

        /// <summary>
        /// Un valor que podría ser una contraseña escrita en el campo equivocado (ej. un Code1
        /// que no existe): solo sus 2 primeros caracteres y su largo.
        /// </summary>
        public static string Mask(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "(vacío)";
            var clean = Clean(value, int.MaxValue);
            return clean.Length <= 2 ? "***" : clean.Substring(0, 2) + "*** (" + clean.Length + " caracteres)";
        }
    }
}
