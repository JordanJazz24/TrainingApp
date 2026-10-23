using System;
using System.IO;
using System.Web;
using System.Web.SessionState;
using ClosedXML.Excel;
using TrainingApp.BLL.Reports;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Admin
{
    // Descargas del dashboard en .xlsx (spec 018, AC-21 y AC-22): Export.ashx?view=…&filtros, un
    // GET con los mismos parámetros de la URL de la página. Solo lee datos (sin anti-CSRF). Revisa
    // la sesión y CanViewReports como las páginas; sin la capacidad, al inicio del rol (AC-2).
    // Los filtros los lee ReportFilters y los datos salen del mismo método de DashboardService que
    // la página (DA-5); el handler no aplica reglas.
    //
    // Session de solo lectura: una descarga larga ("Todo") no bloquea las otras pestañas del
    // admin. Si la Session se perdió, EnsureUserSession la vuelve a llenar para este request
    // (no se guarda; la próxima página la guarda).
    public class Export : IHttpHandler, IReadOnlySessionState
    {
        private const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            if (!SessionHelper.EnsureUserSession())
            {
                context.Response.Redirect(SessionHelper.LoginUrl);
                return;
            }

            if (!SessionHelper.CanViewReports())
            {
                SessionHelper.RedirectHome();
                return;
            }

            if (SessionHelper.MustChangePassword())
            {
                context.Response.Redirect(SessionHelper.ChangePasswordUrl);
                return;
            }

            int userId = (int)context.Session[Constants.SessionKeys.UserId];
            var query  = context.Request.QueryString;
            var service = new DashboardService();

            XLWorkbook workbook;
            string name;
            try
            {
                switch (query["view"])
                {
                    case ReportFilters.ViewOperators:
                        workbook = ExcelReports.Operators(service.ExportOperators(userId, ReportFilters.Operators(query)));
                        name = "Operadores";
                        break;
                    case ReportFilters.ViewSteps:
                        workbook = ExcelReports.Steps(service.GetSteps(userId, ReportFilters.Steps(query)));
                        name = "Pasos";
                        break;
                    case ReportFilters.ViewFollowUp:
                        workbook = ExcelReports.FollowUp(service.GetFollowUp(userId, ReportFilters.FollowUp(query)));
                        name = "Seguimiento";
                        break;
                    case ReportFilters.ViewSessions:
                        workbook = ExcelReports.Sessions(service.GetSessionExport(userId, ReportFilters.Summary(query)));
                        name = "Sesiones";
                        break;
                    default:
                        // Una vista que no existe: al Resumen, como un filtro inválido.
                        context.Response.Redirect(SessionHelper.AdminHomeUrl);
                        return;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // La BLL negó el acceso: el rol cambió con el RoleCache vigente (B-3).
                SessionHelper.RedirectHomeAfterRoleChange();
                return;
            }

            using (workbook)
            using (var stream = new MemoryStream())
            {
                // Se arma completo antes de mandar encabezados: un error a la mitad da una página
                // de error, no un .xlsx roto.
                workbook.SaveAs(stream);
                var response = context.Response;
                response.Clear();
                response.ContentType = XlsxType;
                response.Cache.SetCacheability(HttpCacheability.NoCache);
                response.AddHeader("Content-Disposition", string.Format("attachment; filename=\"{0}_{1:yyyy-MM-dd}.xlsx\"",
                    name, SessionHelper.ToLocal(DateTime.UtcNow)));
                stream.WriteTo(response.OutputStream);
            }
        }
    }
}
