using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Auth;
using TrainingApp.BLL.Reports;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Admin
{
    // Operadores (spec 018, AC-7 y AC-13). Solo UI: el estado lo decide ReadinessPolicy y la
    // búsqueda, los filtros y la paginación DashboardService. Los filtros viven en la URL
    // (?q=…&state=…&guide=…&page=…): nada se guarda en Session.
    public partial class Operators : Page
    {
        private const string PageUrl = "~/Pages/Admin/Operators.aspx";

        // Valor del filtro de estado en la URL ↔ estado de la política.
        private static readonly Dictionary<string, ReadinessState> StateValues =
            new Dictionary<string, ReadinessState>(StringComparer.OrdinalIgnoreCase)
            {
                { "passed",     ReadinessState.Passed },
                { "failed",     ReadinessState.Failed },
                { "training",   ReadinessState.InTraining },
                { "notstarted", ReadinessState.NotStarted }
            };

        private readonly DashboardService _dashboard = new DashboardService();

        // Usuario autenticado (Session escrita solo por el servidor tras el login).
        private int UserId => (int)Session[Constants.SessionKeys.UserId];

        // Quien perdió la capacidad con el RoleCache vigente llega a la BLL, que lanza
        // UnauthorizedAccessException: vuelve a su inicio en vez de un error, nunca al login
        // (AC-2). Con la caché invalidada, el request siguiente ya lleva su rol nuevo.
        protected override void OnError(EventArgs e)
        {
            if (Server.GetLastError()?.GetBaseException() is UnauthorizedAccessException)
            {
                Server.ClearError();
                RoleCache.Invalidate(User.Identity.Name);
                SessionHelper.RedirectHome();
                return;
            }
            base.OnError(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!SessionHelper.EnsureUserSession())
            {
                Response.Redirect(SessionHelper.LoginUrl);
                return;
            }

            // Web.config deja pasar solo al rol Administrator; la página revisa la capacidad y
            // quien no la tiene vuelve a su inicio, nunca al login (AC-2).
            if (!SessionHelper.CanViewReports())
            {
                SessionHelper.RedirectHome();
                return;
            }

            if (SessionHelper.MustChangePassword())
            {
                Response.Redirect(SessionHelper.ChangePasswordUrl);
                return;
            }

            if (!IsPostBack)
                Bind(ReadFilter());
        }

        // Filtrar vuelve a la página 1 con los filtros en la URL.
        protected void BtnFilter_Click(object sender, EventArgs e)
        {
            int.TryParse(DdlGuide.SelectedValue, out int guideId);
            Response.Redirect(BuildUrl(TxtSearch.Text.Trim(), DdlState.SelectedValue,
                guideId > 0 ? guideId : (int?)null, 1));
        }

        // Los filtros de la URL; un valor inválido vale el de inicio (el servicio también valida
        // la guía y el estado).
        private OperatorFilter ReadFilter()
        {
            var filter = new OperatorFilter { Text = Request.QueryString["q"] };
            if (StateValues.TryGetValue(Request.QueryString["state"] ?? string.Empty, out var state))
                filter.State = state;
            if (int.TryParse(Request.QueryString["guide"], out int guideId))
                filter.GuideId = guideId;
            if (int.TryParse(Request.QueryString["page"], out int page))
                filter.Page = page;
            return filter;
        }

        private void Bind(OperatorFilter filter)
        {
            var result = _dashboard.SearchOperators(UserId, filter);

            // Los controles y los enlaces muestran los filtros que de verdad se aplicaron.
            var applied = result.Applied;
            string stateValue = applied.State.HasValue
                ? StateValues.First(kv => kv.Value == applied.State.Value).Key : string.Empty;
            int? guideId = applied.GuideId;
            TxtSearch.Text = applied.Text;
            DdlState.SelectedValue = stateValue;
            DdlGuide.Items.Clear();
            DdlGuide.Items.Add(new ListItem("Todas", string.Empty));
            foreach (var g in result.Guides)
                DdlGuide.Items.Add(new ListItem(g.AreaName, g.Id.ToString()) { Selected = g.Id == guideId });

            RptGuideHeaders.DataSource = result.Guides;
            RptGuideHeaders.DataBind();
            RptOperators.DataSource = result.Rows.Select(ToRow).ToList();
            RptOperators.DataBind();

            LblEmpty.Visible = result.Total == 0;
            LitPageInfo.Text = result.Total == 0 ? string.Empty :
                string.Format("Página {0} de {1} · {2} operadores", result.Page, result.PageCount, result.Total);
            LnkPrev.Visible = result.Page > 1;
            LnkNext.Visible = result.Page < result.PageCount;
            LnkPrev.NavigateUrl = BuildUrl(TxtSearch.Text, stateValue, guideId, result.Page - 1);
            LnkNext.NavigateUrl = BuildUrl(TxtSearch.Text, stateValue, guideId, result.Page + 1);
        }

        private static OperatorListRow ToRow(OperatorRow row)
        {
            return new OperatorListRow
            {
                UserId           = row.UserId,
                Code1            = row.Code1,
                Name             = row.Name,
                NotStarted       = row.NotStarted,
                LastActivityText = ReportText.Date(row.LastActivity),
                DetailUrl        = VirtualPathUtility.ToAbsolute("~/Pages/Admin/Operator.aspx") + "?id=" + row.UserId,
                Guides           = row.Guides.Select(g => new OperatorGuideCell
                {
                    HasSessions = g.State != ReadinessState.NotStarted,
                    StateText   = ReportText.State(g.State),
                    StateCss    = ReportText.StateCss(g.State),
                    ScoresText  = g.BestScore.HasValue
                        ? "Mejor " + ReportText.Score(g.BestScore) + " · Último " + ReportText.Score(g.LastScore)
                        : string.Empty
                }).ToList()
            };
        }

        private string BuildUrl(string text, string state, int? guideId, int page)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrWhiteSpace(text)) query["q"] = text.Trim();
            if (!string.IsNullOrEmpty(state)) query["state"] = state;
            if (guideId.HasValue) query["guide"] = guideId.Value.ToString();
            if (page > 1) query["page"] = page.ToString();
            var url = ResolveUrl(PageUrl);
            return query.Count == 0 ? url : url + "?" + query;
        }
    }
}
