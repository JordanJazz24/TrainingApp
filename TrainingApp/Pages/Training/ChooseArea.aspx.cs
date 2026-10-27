using System;
using System.Linq;
using System.Web.UI;
using TrainingApp.BLL.Training;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Training
{
    // Spec 021: el operador sin área la elige una vez (D-169, D-177). La BLL valida todo
    // (OperatorAreaService); la página solo arma el selector y redirige.
    public partial class ChooseArea : Page
    {
        private readonly TrainingGuideService _guideService = new TrainingGuideService();

        private int CurrentUserId => (int)Session[Constants.SessionKeys.UserId];

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!SessionHelper.EnsureUserSession())
            {
                Response.Redirect(SessionHelper.LoginUrl);
                return;
            }

            // Un admin no tiene área (P-2): a su módulo.
            if (!SessionHelper.CanTrain())
            {
                SessionHelper.RedirectHome();
                return;
            }

            if (SessionHelper.MustChangePassword())
            {
                Response.Redirect(SessionHelper.ChangePasswordUrl);
                return;
            }

            // Con área en la BD no se vuelve a elegir (AC-5). Se refresca la Session por si la
            // asignó un admin: si no, el inicio lo mandaría otra vez aquí.
            if (_guideService.GetStartOptions(CurrentUserId).HasArea)
            {
                if (SessionHelper.RefreshUserSession())
                    Response.Redirect(SessionHelper.TrainingHomeUrl);
                else
                    SessionHelper.Logout();
                return;
            }

            if (!IsPostBack)
                BindAreas();
        }

        protected void BtnContinue_Click(object sender, EventArgs e)
        {
            // Sin selección solo llega un envío manipulado (el cliente lo impide): se vuelve a pintar.
            if (!int.TryParse(Request.Form["area"], out int areaId))
            {
                BindAreas();
                return;
            }

            new OperatorAreaService().ChooseOwnArea(CurrentUserId, areaId);
            SessionHelper.RefreshUserSession();
            Response.Redirect(SessionHelper.TrainingHomeUrl);
        }

        // Las áreas elegibles agrupadas por producto, ya ordenadas por el SP (D-176).
        private void BindAreas()
        {
            var groups = _guideService.GetEligibleAreas()
                .GroupBy(a => a.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    Areas = g.Select(a => new { a.AreaId, a.AreaName, Label = AreaLabel.Of(a.ProductName, a.AreaName) })
                })
                .ToList();

            PhChoose.Visible   = groups.Count > 0;
            LblNoAreas.Visible = groups.Count == 0;
            RptProducts.DataSource = groups;
            RptProducts.DataBind();
        }
    }
}
