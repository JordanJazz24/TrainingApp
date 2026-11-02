using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Admin;
using ResultModel = TrainingApp.BLL.Training.SessionResult;

namespace TrainingApp.Pages.Training
{
    // Una fila de la lista "Pasos" del fin de sesión, con los textos ya armados (spec 025, E3).
    public class SessionResultStepView
    {
        public int Number { get; set; }
        public string Name { get; set; }

        // "Decisión incorrecta", "Acción incorrecta ×2"…: solo el tipo y el conteo, nunca la
        // respuesta correcta (D-331, P-6).
        public IList<string> Chips { get; set; } = new List<string>();

        public string OutcomeText { get; set; }
        public string OutcomeIcon { get; set; }

        // ok, warn, bad o idle (la clase .oc del desenlace; nunca solo color: lleva ícono y texto).
        public string OutcomeTone { get; set; }

        // "is-ref" si el paso va en "A reforzar": la lista la muestra o la oculta en el cliente.
        public string RowCss { get; set; }

        public bool HasPoints { get; set; }
        public string PointsText { get; set; }
        public bool HasLost { get; set; }
        public string LostText { get; set; }
    }

    // La acción principal y la frase de "Qué hacer después" (D-343).
    public class SessionNextView
    {
        public string Why { get; set; }
        public string Label { get; set; }
        public string Icon { get; set; }
    }

    // Textos del fin de sesión (spec 025, P-6, D-343): presentación, como MesErrorNotes. Qué
    // reforzar y qué sugerir lo decide la BLL (SessionResultRule, ModePolicy); aquí solo palabras.
    public static class SessionResultText
    {
        public static string Reason(AttemptReason reason)
        {
            switch (reason)
            {
                case AttemptReason.OutOfSequence:      return "Fuera de secuencia";
                case AttemptReason.WrongDecision:      return "Decisión incorrecta";
                case AttemptReason.InvalidAction:      return "Acción incorrecta";
                case AttemptReason.MissingSelection:   return "Sin selección";
                case AttemptReason.EmptyRequiredField: return "Campo obligatorio vacío";
                default:                               return string.Empty;
            }
        }

        public static string ModeIcon(SessionMode mode)
        {
            switch (mode)
            {
                case SessionMode.Assessment:   return "bi-clipboard-check";
                case SessionMode.FreePractice: return "bi-lightbulb";
                default:                       return "bi-signpost-2";
            }
        }

        // Un número en puntos (fracción × 100), sin ceros de más.
        public static string Points(decimal fraction)
            => (fraction * 100m).ToString("0.#", CultureInfo.CurrentCulture);

        public static SessionResultStepView Step(StepResultRow row)
        {
            var view = new SessionResultStepView
            {
                Number = row.Order,
                Name   = row.StepName,
                Chips  = row.Reasons
                    .Where(r => !string.IsNullOrEmpty(Reason(r.Reason)))
                    .Select(r => Reason(r.Reason) + (r.Count > 1 ? " ×" + r.Count : string.Empty))
                    .ToList(),
                RowCss = row.NeedsReinforcement ? "is-ref" : string.Empty
            };
            switch (row.Outcome)
            {
                case StepResultOutcome.AtFirst:
                    view.OutcomeText = "A la primera"; view.OutcomeIcon = "bi-check-circle"; view.OutcomeTone = "ok"; break;
                case StepResultOutcome.WithFailures:
                    view.OutcomeText = "Con " + Failures(row.Failures); view.OutcomeIcon = "bi-exclamation-triangle"; view.OutcomeTone = "warn"; break;
                case StepResultOutcome.WithoutSuccess:
                    view.OutcomeText = "Sin acierto (avance forzado)"; view.OutcomeIcon = "bi-x-octagon"; view.OutcomeTone = "bad"; break;
                case StepResultOutcome.SkippedWithFailures:
                    view.OutcomeText = "Saltado (" + Failures(row.Failures) + ")"; view.OutcomeIcon = "bi-skip-forward"; view.OutcomeTone = "warn"; break;
                default:
                    view.OutcomeText = "Saltado"; view.OutcomeIcon = "bi-skip-forward"; view.OutcomeTone = "idle"; break;
            }
            if (row.PointsEarned.HasValue && row.PointsPossible.HasValue)
            {
                view.HasPoints  = true;
                view.PointsText = Points(row.PointsEarned.Value) + " / " + Points(row.PointsPossible.Value);
                // Los puntos perdidos, en el paso a reforzar (en Assessment; en Guided no se pierden).
                var lost = row.PointsLost ?? 0m;
                view.HasLost  = row.NeedsReinforcement && lost > 0m;
                view.LostText = "−" + Points(lost) + " pts";
            }
            return view;
        }

        private static string Failures(int n) => n + (n == 1 ? " fallo" : " fallos");

        // La frase y la acción principal de "Qué hacer después" (D-343). La BLL ya eligió la
        // acción (SessionResult.Primary y Repeat); aquí solo sus palabras.
        public static SessionNextView Next(ResultModel result)
        {
            var primary = result.Primary;
            if (primary.IsHome)
            {
                // Sin Repetir, la guía ya no se puede iniciar; con él, un Assessment aprobado.
                return new SessionNextView
                {
                    Label = "Ir al Inicio", Icon = "bi-house",
                    Why = result.Repeat == null ? "Esta guía ya no se puede iniciar." : "Aprobaste este Assessment."
                };
            }
            if (primary.Mode == SessionMode.Assessment)
                return new SessionNextView
                {
                    Label = "Hacer el Assessment", Icon = ModeIcon(SessionMode.Assessment),
                    Why = "Sin fallos ni saltos: ya puedes evaluarte."
                };

            // Práctica libre: de nuevo (si venía de Práctica libre) o después de Guided o de un Assessment.
            switch (result.Detail.Mode)
            {
                case SessionMode.FreePractice:
                    return new SessionNextView
                    {
                        Label = "Practicar de nuevo en Práctica libre", Icon = ModeIcon(SessionMode.FreePractice),
                        Why = "Aún hay pasos por afianzar."
                    };
                case SessionMode.Assessment:
                    return new SessionNextView
                    {
                        Label = "Practicar en Práctica libre", Icon = ModeIcon(SessionMode.FreePractice),
                        Why = "Repasa los pasos marcados sin la presión del puntaje."
                    };
                default:
                    return new SessionNextView
                    {
                        Label = "Practicar en Práctica libre", Icon = ModeIcon(SessionMode.FreePractice),
                        Why = "Ahora sin la guía visual, para afianzar lo aprendido."
                    };
            }
        }
    }
}
