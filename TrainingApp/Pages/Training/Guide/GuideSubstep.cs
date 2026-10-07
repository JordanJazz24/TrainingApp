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
        public const string Scan    = "scan";    // escanea (o escribe + Enter); avanza solo (R6)
        public const string Final   = "final";   // globo final sobre "Session Complete", con OK
    }

    // Regla de un campo de un bloque con decisión (Data Collection, R10): las mismas que
    // califica StepValidator para el tipo del parámetro.
    public sealed class GuideField
    {
        // Selector CSS del campo dentro del bloque.
        public string Selector { get; set; }

        // Valor exacto (Boolean: "true"); null en el resto.
        public string Expect { get; set; }

        // "number" (con Min/Max opcionales) o "date".
        public string Rule { get; set; }

        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
    }

    // Formato que "Continuar" exige en un subpaso de captura: el mismo que ya pide el control.
    // Nunca revisa decisiones (lectura A de la spec).
    public static class GuideFormat
    {
        public const string Required = "required"; // no vacío y sin < ni > (AUD-11)
        public const string Digits   = "digits";   // solo dígitos (Shop Order)
        public const string Number   = "number";   // número (ID del molde, parámetros MSS)
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

        // Respuesta correcta de una decisión (spec 010, R10): valor que deben tener todos los
        // campos del subpaso para avanzar. null en los campos libres y los clics.
        public string Expect { get; set; }

        // Reglas por campo de un bloque (Data Collection); null si no aplica.
        public List<GuideField> Fields { get; set; }

        // Selectores que, si todos tienen un elemento visible, dan el subpaso por hecho: la
        // guía lo salta al llegar (filas ya seleccionadas) o avanza (escaneo con Add visible).
        public List<string> Done { get; set; }

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
                    expect   = s.Expect,
                    fields   = s.Fields?.Select(f => new
                    {
                        sel    = f.Selector,
                        expect = f.Expect,
                        rule   = f.Rule,
                        min    = f.Min,
                        max    = f.Max
                    }).ToList(),
                    done     = s.Done,
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
