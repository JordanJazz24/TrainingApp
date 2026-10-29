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
    // (?q=…&state=…&area=…&page=…; spec 024): nada se guarda en Session.
    public partial class Operators : Page
    {
        private const string PageUrl = "~/Pages/Admin/Operators.aspx";

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
                SessionHelper.RedirectHomeAfterRoleChange();
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
            {
                Bind(ReportFilters.Operators(Request.QueryString));
                // Vuelta desde un detalle con un id que no existe o no es de un operador.
                if (Request.QueryString["missing"] == "1")
                    ((UI.Master.AdminMaster)Master).ShowMessage("warning", "Operador no encontrado",
                        "El operador no existe o no se puede ver.");
            }
        }

        // Filtrar vuelve a la página 1 con los filtros en la URL. El área llega del <select> nativo
        // (agrupado por producto, sin EventValidation): la BLL vuelve a validarla (P-4).
        protected void BtnFilter_Click(object sender, EventArgs e)
        {
            Response.Redirect(BuildUrl(TxtSearch.Text.Trim(), DdlState.SelectedValue,
                (Request.Form["area"] ?? string.Empty).Trim(), 1));
        }

        private void Bind(OperatorFilter filter)
        {
            var result = _dashboard.SearchOperators(UserId, filter);

            // Los controles y los enlaces muestran los filtros que de verdad se aplicaron.
            var applied = result.Applied;
            string stateValue = applied.State.HasValue
                ? ReportFilters.StateValues.First(kv => kv.Value == applied.State.Value).Key : string.Empty;
            string areaValue = applied.WithoutArea ? ReportFilters.AreaNone
                : applied.AreaId.HasValue ? applied.AreaId.Value.ToString() : string.Empty;
            TxtSearch.Text = applied.Text;
            DdlState.SelectedValue = stateValue;

            // Área (D-262): Todas, las áreas con guía publicada agrupadas por producto y Sin área.
            AreaAllSelected  = areaValue.Length == 0;
            AreaNoneSelected = applied.WithoutArea;
            RptAreaGroups.DataSource = result.Areas
                .GroupBy(a => a.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    Areas = g.Select(a => new { a.AreaId, a.AreaName, Selected = applied.AreaId == a.AreaId }).ToList()
                })
                .ToList();
            RptAreaGroups.DataBind();

            RptOperators.DataSource = result.Rows.Select(ToRow).ToList();
            RptOperators.DataBind();

            PhTable.Visible  = result.Total > 0;
            PnlEmpty.Visible = result.Total == 0;
            LnkClear.NavigateUrl = ResolveUrl(PageUrl);
            PhPager.Visible  = result.Total > 0;
            LitPageInfo.Text = string.Format("Página {0} de {1} · {2} operadores", result.Page, result.PageCount, result.Total);
            LnkPrev.Visible = result.Page > 1;
            LnkNext.Visible = result.Page < result.PageCount;
            LnkPrev.NavigateUrl = BuildUrl(TxtSearch.Text, stateValue, areaValue, result.Page - 1);
            LnkNext.NavigateUrl = BuildUrl(TxtSearch.Text, stateValue, areaValue, result.Page + 1);
            LnkExport.NavigateUrl = ReportFilters.ExportUrl(ReportFilters.ViewOperators, Request.QueryString);
        }

        private static OperatorListRow ToRow(OperatorRow row)
        {
            return new OperatorListRow
            {
                UserId           = row.UserId,
                Code1            = row.Code1,
                Name             = row.Name,
                AreaText         = row.AreaText,
                LastActivityText = ReportText.Date(row.LastActivity),
                DetailUrl        = VirtualPathUtility.ToAbsolute("~/Pages/Admin/Operator.aspx") + "?id=" + row.UserId,
                // "Sin iniciar" también es un estado con su insignia (P-2), no un "—"; sin guías que
                // entrenar en su área, el texto atenuado (spec 024, D-267, P-5).
                AreaNote         = ReportText.AreaNote(row.AreaStatus),
                AreaGuides       = row.AreaGuides.Select(g => new OperatorGuideCell
                {
                    StateText   = ReportText.State(g.State),
                    StateCss    = ReportText.StateCss(g.State),
                    StateIcon   = ReportText.StateIcon(g.State),
                    ScoresText  = ReportText.Scores(g)
                }).ToList()
            };
        }

        // El <option> de Todas o Sin área seleccionado (el de cada área lo marca su fila).
        protected bool AreaAllSelected { get; private set; }
        protected bool AreaNoneSelected { get; private set; }

        private string BuildUrl(string text, string state, string area, int page)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrWhiteSpace(text)) query["q"] = text.Trim();
            if (!string.IsNullOrEmpty(state)) query["state"] = state;
            if (!string.IsNullOrEmpty(area)) query["area"] = area;
            if (page > 1) query["page"] = page.ToString();
            var url = ResolveUrl(PageUrl);
            return query.Count == 0 ? url : url + "?" + query;
        }
    }
}
