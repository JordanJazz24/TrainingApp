using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Admin
{
    // Los filtros del dashboard en la URL (spec 018 → Filtros), leídos en un solo lugar: cada
    // página y su descarga (Export.ashx) los leen igual, así el Excel trae lo mismo que la
    // pantalla (DA-5). Un valor inválido vale el de inicio; el servicio vuelve a validar.
    public static class ReportFilters
    {
        // Valores de la URL; sin el parámetro, el de inicio (últimos 30 días, Assessment).
        public const string PeriodAll    = "all";
        public const string ModePractice = "practice";

        // Las descargas (Export.ashx?view=…).
        public const string ViewOperators = "operators";
        public const string ViewSteps     = "steps";
        public const string ViewFollowUp  = "followup";
        public const string ViewSessions  = "sessions";

        // Valor del filtro de estado en la URL ↔ estado de la política.
        public static readonly IReadOnlyDictionary<string, ReadinessState> StateValues =
            new Dictionary<string, ReadinessState>(StringComparer.OrdinalIgnoreCase)
            {
                { "passed",     ReadinessState.Passed },
                { "failed",     ReadinessState.Failed },
                { "training",   ReadinessState.InTraining },
                { "notstarted", ReadinessState.NotStarted }
            };

        // ?q=…&state=…&guide=…&page=…
        public static OperatorFilter Operators(NameValueCollection query)
        {
            var filter = new OperatorFilter { Text = query["q"] };
            if (StateValues.TryGetValue(query["state"] ?? string.Empty, out var state))
                filter.State = state;
            filter.GuideId = Guide(query);
            if (int.TryParse(query["page"], out int page))
                filter.Page = page;
            return filter;
        }

        // ?period=all&guide=…&mode=practice
        public static StepFilter Steps(NameValueCollection query)
            => new StepFilter
            {
                Period  = Period(query),
                GuideId = Guide(query),
                Mode    = query["mode"] == ModePractice ? SessionMode.FreePractice : SessionMode.Assessment
            };

        // ?period=all&guide=…
        public static SummaryFilter Summary(NameValueCollection query)
            => new SummaryFilter { Period = Period(query), GuideId = Guide(query) };

        // ?guide=…
        public static FollowUpFilter FollowUp(NameValueCollection query)
            => new FollowUpFilter { GuideId = Guide(query) };

        // La descarga de una vista con los filtros de la URL de su página (sin la página de la
        // lista ni el aviso de Operadores).
        public static string ExportUrl(string view, NameValueCollection query)
        {
            var export = HttpUtility.ParseQueryString(string.Empty);
            export["view"] = view;
            foreach (string key in query.AllKeys)
                if (key != null && key != "view" && key != "page" && key != "missing")
                    export[key] = query[key];
            return VirtualPathUtility.ToAbsolute("~/Pages/Admin/Export.ashx") + "?" + export;
        }

        private static ReportPeriod Period(NameValueCollection query)
            => query["period"] == PeriodAll ? ReportPeriod.All : ReportPeriod.Last30Days;

        private static int? Guide(NameValueCollection query)
            => int.TryParse(query["guide"], out int guideId) ? guideId : (int?)null;
    }
}
