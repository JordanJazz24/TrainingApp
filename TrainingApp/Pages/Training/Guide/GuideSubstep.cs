using System.Collections.Generic;
using System.Linq;

namespace TrainingApp.Pages.Training.Guide
{
    // Cómo avanza un subpaso (spec 010, "Cómo avanza").
    public static class GuideMode
    {
        public const string Click   = "click";   // clic en el elemento
        public const string Change  = "change";  // cambio en un desplegable
        public const string Capture = "capture"; // escribe o escanea y presiona "Continuar"
        public const string Verify  = "verify";  // revisa un valor en pantalla y "Continuar"
        public const string Info    = "info";    // explica lo que se ve y "Continuar"
    }

    // Formato que "Continuar" exige en un subpaso de captura: el mismo que ya pide el control.
    // Nunca revisa decisiones (lectura A de la spec).
    public static class GuideFormat
    {
        public const string Required = "required"; // no vacío y sin < ni > (AUD-11)
        public const string Digits   = "digits";   // solo dígitos (Shop Order)
    }

    // Un subpaso del guion. El texto ya viene armado y codificado por GuideScriptBuilder.
    public sealed class GuideSubstep
    {
        public string Key { get; set; }

        // Selector CSS del elemento a resaltar (atributos data-guide de los controles).
        public string Anchor { get; set; }

        // Varios elementos con el ancla (ej. filas de Resource Setup): se resalta el último.
        public bool PickLast { get; set; }

        public string Mode { get; set; }

        // El clic envía la página: la guía espera la respuesta del servidor.
        public bool Postback { get; set; }

        // Solo en Capture.
        public string Format { get; set; }

        // Info sobre un mensaje del MES: la guía lo oculta al presionar "Continuar" (R9).
        public bool HideOnContinue { get; set; }

        // Subpaso al que vuelve la guía si el clic termina en un aviso de formato del control.
        public string RetryKey { get; set; }

        public string StepLabel { get; set; }
        public int Index { get; set; }
        public int Total { get; set; }

        // HTML de la instrucción: texto codificado y valores en <strong>.
        public string Html { get; set; }
    }

    // Lo que recibe el motor en cada respuesta: los subpasos y la clave de entrada.
    public sealed class GuideScript
    {
        public List<GuideSubstep> Substeps { get; } = new List<GuideSubstep>();
        public string EntryKey { get; set; }

        public bool IsEmpty => Substeps.Count == 0;

        // Nombres cortos para el JS (JavaScriptSerializer usa los de las propiedades).
        public object ToClient()
        {
            return new
            {
                entry = EntryKey,
                substeps = Substeps.Select(s => new
                {
                    key      = s.Key,
                    anchor   = s.Anchor,
                    last     = s.PickLast,
                    mode     = s.Mode,
                    postback = s.Postback,
                    format   = s.Format,
                    hide     = s.HideOnContinue,
                    retry    = s.RetryKey,
                    step     = s.StepLabel,
                    index    = s.Index,
                    total    = s.Total,
                    html     = s.Html
                }).ToList()
            };
        }
    }
}
