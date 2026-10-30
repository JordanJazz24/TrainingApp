using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainingApp.BLL.Reports;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Admin
{
    // Textos del dashboard (spec 018), iguales en todas sus vistas. Solo presentación: el
    // estado lo decide ReadinessPolicy.
    public static class ReportText
    {
        // Sin dato (sin denominador, sin Assessment, sesión en curso): nunca 0 (AC-11).
        public const string None = "—";

        public static string State(ReadinessState state)
        {
            switch (state)
            {
                case ReadinessState.Passed:     return "Aprobado";
                case ReadinessState.Failed:     return "Reprobado";
                case ReadinessState.InTraining: return "En entrenamiento";
                default:                        return "Sin iniciar";
            }
        }

        // Insignia de estado (brief 019, P-2): el color va siempre con el texto y su ícono.
        public static string StateCss(ReadinessState state)
        {
            switch (state)
            {
                case ReadinessState.Passed:     return "mt-badge mt-badge-ok";
                case ReadinessState.Failed:     return "mt-badge mt-badge-bad";
                case ReadinessState.InTraining: return "mt-badge mt-badge-warn";
                default:                        return "mt-badge mt-badge-idle";
            }
        }

        public static string StateIcon(ReadinessState state)
        {
            switch (state)
            {
                case ReadinessState.Passed:     return "bi bi-check-circle";
                case ReadinessState.Failed:     return "bi bi-x-circle";
                case ReadinessState.InTraining: return "bi bi-hourglass-split";
                default:                        return "bi bi-dash-circle";
            }
        }

        // El estado de un operador que no tiene guías que entrenar en su área (spec 024, D-267,
        // P-5); null si las tiene (entonces van sus insignias).
        public static string AreaNote(OperatorAreaStatus status)
        {
            switch (status)
            {
                case OperatorAreaStatus.NoArea:           return AreaLabel.NoArea;
                case OperatorAreaStatus.NoPublishedGuide: return "Sin guía publicada";
                default:                                  return null;
            }
        }

        // "Mejor 85.0% · Último 60.0%"; vacío sin Assessment completados.
        public static string Scores(GuideReadiness readiness)
            => readiness.BestScore.HasValue
                ? "Mejor " + Score(readiness.BestScore) + " · Último " + Score(readiness.LastScore)
                : string.Empty;

        // Puntaje de 0 a 1 como en el resumen final del operador ("85.0%").
        public static string Score(decimal? score)
            => Percent(score);

        // Una tasa de 0 a 1 ("45.0%"); "—" sin denominador (AC-11).
        public static string Percent(decimal? rate)
            => rate.HasValue ? (rate.Value * 100m).ToString("F1") + "%" : None;

        // El ancho de una barra de % para el atributo style ("45.5"), con punto decimal en
        // cualquier cultura.
        public static string BarWidth(decimal percent)
            => Math.Round(percent, 1).ToString("0.#", CultureInfo.InvariantCulture);

        // "3 de 8 sesiones": las sesiones con fallo de un paso entre las que llegaron a él.
        public static string SessionsWithFailure(int withFailure, int sessions)
            => withFailure + " de " + sessions + (sessions == 1 ? " sesión" : " sesiones");

        // El período de una vista, para decir a qué corresponde cada bloque.
        public static string Period(ReportPeriod period)
        {
            switch (period)
            {
                case ReportPeriod.Today:      return "Hoy";
                case ReportPeriod.ThisWeek:   return "Esta semana";
                case ReportPeriod.PrevMonth:  return "Mes anterior";
                case ReportPeriod.Last30Days: return "Últimos 30 días";
                case ReportPeriod.All:        return "Todo el historial";
                case ReportPeriod.Custom:     return "Personalizado";
                default:                      return "Este mes";
            }
        }

        // Meses abreviados, sin depender de la cultura del servidor.
        private static readonly string[] Months =
            { "ene.", "feb.", "mar.", "abr.", "may.", "jun.", "jul.", "ago.", "sep.", "oct.", "nov.", "dic." };

        private static string Month(DateTime day) => Months[day.Month - 1];

        // El rango aplicado en palabras (P-14): "Del 1 al 28 de oct. de 2026", "Del 29 de sep. al
        // 28 de oct. de 2026", "El 28 de oct. de 2026"; con Todo, "Todo el historial".
        public static string Range(ReportRange range)
        {
            if (!range.FromLocal.HasValue || !range.ToLocal.HasValue) return Period(ReportPeriod.All);
            DateTime from = range.FromLocal.Value, to = range.ToLocal.Value;
            if (from == to) return "El " + from.Day + " de " + Month(from) + " de " + from.Year;
            string start = from.Year != to.Year  ? from.Day + " de " + Month(from) + " de " + from.Year
                         : from.Month != to.Month ? from.Day + " de " + Month(from)
                         : from.Day.ToString(CultureInfo.InvariantCulture);
            return "Del " + start + " al " + to.Day + " de " + Month(to) + " de " + to.Year;
        }

        // El período anterior, corto ("1–28 sep.", "30 ago.–28 sep.", "27 oct."; con el año si no
        // es el del período): la referencia de cada cambio (P-5).
        public static string PreviousRange(ReportRange range)
        {
            if (!range.PrevFromLocal.HasValue || !range.PrevToLocal.HasValue) return string.Empty;
            DateTime from = range.PrevFromLocal.Value, to = range.PrevToLocal.Value;
            string year = range.ToLocal.HasValue && to.Year != range.ToLocal.Value.Year ? " " + to.Year : string.Empty;
            if (from == to) return from.Day + " " + Month(from) + year;
            string start = from.Year != to.Year  ? from.Day + " " + Month(from) + " " + from.Year
                         : from.Month != to.Month ? from.Day + " " + Month(from)
                         : from.Day.ToString(CultureInfo.InvariantCulture);
            return start + "–" + to.Day + " " + Month(to) + year;
        }

        // A qué período corresponde un bloque ("este mes", "del 5 al 14 de oct. de 2026"): el
        // nombre del atajo o, con Personalizado, el rango.
        public static string PeriodOf(ReportRange range)
        {
            string text = range.Period == ReportPeriod.Custom ? Range(range) : Period(range.Period);
            return char.ToLowerInvariant(text[0]) + text.Substring(1);
        }

        // Cómo va la gráfica de actividad ("por día").
        public static string Grain(ActivityGrain grain)
        {
            switch (grain)
            {
                case ActivityGrain.Week:  return "por semana (de lunes a domingo)";
                case ActivityGrain.Month: return "por mes";
                default:                  return "por día";
            }
        }

        // Fecha UTC de la BD en la hora de la planta (DA-8).
        public static string Date(DateTime? utc)
            => utc.HasValue ? SessionHelper.ToLocal(utc.Value).ToString("dd/MM/yyyy HH:mm") : None;

        // Los nombres de modo de la pantalla de inicio del operador.
        public static string Mode(SessionMode mode)
        {
            switch (mode)
            {
                case SessionMode.Assessment:   return "Assessment";
                case SessionMode.FreePractice: return "Práctica libre";
                default:                       return "Guided";
            }
        }

        public static string Status(SessionStatus status)
        {
            switch (status)
            {
                case SessionStatus.Completed: return "Completada";
                case SessionStatus.Abandoned: return "Abandonada";
                default:                      return "En curso";
            }
        }

        // Motivo de un intento fallido (AttemptReason, spec 013).
        public static string Reason(AttemptReason? reason)
        {
            switch (reason)
            {
                case AttemptReason.OutOfSequence:      return "Fuera de secuencia";
                case AttemptReason.WrongDecision:      return "Decisión incorrecta";
                case AttemptReason.InvalidAction:      return "Acción inválida";
                case AttemptReason.MissingSelection:   return "Falta selección";
                case AttemptReason.EmptyRequiredField: return "Campo vacío";
                case AttemptReason.Skipped:            return "Saltado";
                default:                               return None;
            }
        }

        // Un motivo con su acción y cuántas veces se registró (Pasos y su Excel). La acción va
        // con su código, como se registra (AttemptActions).
        public static string ReasonCount(ReasonCount reason)
            => Reason(reason.Reason) + " · " + (reason.Action ?? None) + " (" + reason.Count + ")";

        // El dato de la curva (AC-15): "4 sesiones de práctica antes del primer aprobado".
        public static string PracticeBeforeFirstPass(int? sessions)
        {
            if (!sessions.HasValue) return "Todavía no aprueba esta guía.";
            return sessions.Value == 1
                ? "1 sesión de práctica antes del primer aprobado"
                : sessions.Value + " sesiones de práctica antes del primer aprobado";
        }

        // Cómo terminó un paso de una sesión (hoja Intentos por paso).
        public static string Outcome(StepOutcome outcome)
        {
            switch (outcome)
            {
                case StepOutcome.Correct:        return "Acierto";
                case StepOutcome.WithoutSuccess: return "Sin acierto";
                case StepOutcome.Skipped:        return "Saltado";
                default:                         return "Sin resolver";
            }
        }

        // Una línea por regla de Seguimiento; S-3, una por paso (Excel). Las mismas frases que la
        // página, unidas a su S-n.
        public static IEnumerable<string> FollowUpLines(FollowUpReason reason)
            => FollowUpReasonLines(reason).Select(l => l.Code + " · " + l.Text);

        // Una regla de Seguimiento como frase con su etiqueta S-n (spec 022, brief 5); S-3, una por
        // paso. La de reprobados va en rojo con el ícono de alerta (P-2): nunca solo por color.
        public static IEnumerable<FollowUpLine> FollowUpReasonLines(FollowUpReason reason)
        {
            switch (reason.Rule)
            {
                case FollowUpRule.RepeatedFailures:
                    return new[] { Line("S-1", true, "bi bi-exclamation-triangle",
                        string.Format("Reprobó {0} Assessment sin aprobar ninguno", reason.FailedAssessments)) };
                case FollowUpRule.Inactive:
                    return new[] { Line("S-2", false, "bi bi-clock-history",
                        string.Format("{0} días sin sesiones de esta guía", reason.DaysInactive)) };
                case FollowUpRule.StuckStep:
                    return reason.Steps.Select(s => Line("S-3", false, "bi bi-arrow-repeat",
                        string.Format("{0}: sin acierto en {1} Assessment", s.StepName, s.Times)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason.Rule, "Regla de seguimiento sin texto.");
            }
        }

        private static FollowUpLine Line(string code, bool alert, string icon, string text) => new FollowUpLine
        {
            Code   = code,
            Text   = text,
            Icon   = icon,
            Css    = alert ? "is-alert" : string.Empty,
            TagCss = alert ? "followup-tag is-alert" : "followup-tag"
        };

        // "3 de 3 reglas", "1 de 3 reglas": cuántas reglas cumple un caso (spec 022, brief 5).
        public static string FollowUpRuleCount(int rules)
            => string.Format("{0} de {1} reglas", rules, Enum.GetValues(typeof(FollowUpRule)).Length);

        // "1 caso", "8 casos" (Seguimiento).
        public static string Cases(int n) => n == 1 ? "1 caso" : n + " casos";

        // "45 s", "25 min 13 s", "1 h 05 min"; "—" sin duración (sesión en curso).
        public static string Duration(TimeSpan? duration)
        {
            if (!duration.HasValue) return None;
            var d = duration.Value;
            if (d.TotalHours >= 1) return string.Format("{0} h {1:00} min", (int)d.TotalHours, d.Minutes);
            if (d.TotalMinutes >= 1) return string.Format("{0} min {1:00} s", d.Minutes, d.Seconds);
            return d.Seconds + " s";
        }
    }
}
