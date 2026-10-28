using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;
using TrainingApp.BLL.Reports;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Admin
{
    // Los filtros del dashboard en la URL (spec 018 → Filtros; el período, spec 023 P-7), leídos
    // en un solo lugar: cada página y su descarga (Export.ashx) los leen igual, así el Excel trae
    // lo mismo que la pantalla (DA-5). Un valor inválido vale el de inicio; el servicio vuelve a
    // validar.
    public static class ReportFilters
    {
        // Valores de la URL; sin el parámetro, el de inicio (Este mes, Assessment).
        public const string PeriodAll    = "all";
        public const string ModePractice = "practice";

        // Las descargas (Export.ashx?view=…).
        public const string ViewOperators = "operators";
        public const string ViewSteps     = "steps";
        public const string ViewFollowUp  = "followup";
        public const string ViewSessions  = "sessions";

        // Valor del período en la URL ↔ período (P-7). Este mes no va en la URL: es el de inicio.
        public static readonly IReadOnlyDictionary<string, ReportPeriod> PeriodValues =
            new Dictionary<string, ReportPeriod>(StringComparer.OrdinalIgnoreCase)
            {
                { "today",     ReportPeriod.Today },
                { "week",      ReportPeriod.ThisWeek },
                { "month",     ReportPeriod.ThisMonth },
                { "prevmonth", ReportPeriod.PrevMonth },
                { "30d",       ReportPeriod.Last30Days },
                { PeriodAll,   ReportPeriod.All },
                { "custom",    ReportPeriod.Custom }
            };

        // Fechas de Personalizado en la URL (y en <input type="date">).
        public const string DateFormat = "yyyy-MM-dd";

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

        // ?period=…&from=…&to=…&guide=…&mode=practice
        public static StepFilter Steps(NameValueCollection query)
            => new StepFilter
            {
                Period  = Period(query),
                From    = Date(query["from"]),
                To      = Date(query["to"]),
                GuideId = Guide(query),
                Mode    = query["mode"] == ModePractice ? SessionMode.FreePractice : SessionMode.Assessment
            };

        // ?period=…&from=…&to=…&guide=…
        public static SummaryFilter Summary(NameValueCollection query)
            => new SummaryFilter { Period = Period(query), From = Date(query["from"]), To = Date(query["to"]), GuideId = Guide(query) };

        // ?guide=…
        public static FollowUpFilter FollowUp(NameValueCollection query)
            => new FollowUpFilter { GuideId = Guide(query) };

        // Escribe el período en una URL: nada con Este mes; Inicio y Fin solo con Personalizado
        // (tal como llegaron: si faltan, la BLL los rechaza y la página muestra el aviso).
        public static void WritePeriod(NameValueCollection query, ReportPeriod period, DateTime? from, DateTime? to)
        {
            var key = ValueOf(period);
            if (period == ReportPeriods.Default || key == null) return;
            query["period"] = key;
            if (period != ReportPeriod.Custom) return;
            if (from.HasValue) query["from"] = from.Value.ToString(DateFormat, CultureInfo.InvariantCulture);
            if (to.HasValue)   query["to"]   = to.Value.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        // El valor de un período en la URL ("week"); null fuera de las opciones.
        public static string ValueOf(ReportPeriod period)
            => PeriodValues.FirstOrDefault(p => p.Value == period).Key;

        // Una fecha AAAA-MM-DD; null si falta o no es válida.
        public static DateTime? Date(string value)
            => DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date : (DateTime?)null;

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

        // Sin el parámetro, Este mes. Un valor desconocido pasa como período fuera del enum: la
        // BLL lo resuelve como Este mes y marca el aviso (P-6).
        private static ReportPeriod Period(NameValueCollection query)
        {
            var value = query["period"];
            if (string.IsNullOrEmpty(value)) return ReportPeriods.Default;
            return PeriodValues.TryGetValue(value, out var period) ? period : (ReportPeriod)(-1);
        }

        private static int? Guide(NameValueCollection query)
            => int.TryParse(query["guide"], out int guideId) ? guideId : (int?)null;
    }
}
