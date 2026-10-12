using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.Pages.Training
{
    public partial class StartSession : Page
    {
        private readonly TrainingGuideService _guideService = new TrainingGuideService();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Re-hidratar sesión si se perdió pero FormsAuth sigue válido
            if (!SessionHelper.EnsureUserSession())
            {
                Response.Redirect("~/Pages/Auth/Login.aspx");
                return;
            }

            // Primer acceso sin contraseña personal: no puede entrenar (auditoría AUD-08).
            if (SessionHelper.MustChangePassword())
            {
                Response.Redirect("~/Pages/Auth/ChangePassword.aspx");
                return;
            }

            if (!IsPostBack)
                LoadProducts();
        }

        protected void DdlProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadAreas();
        }

        protected void DdlArea_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadGuides();
        }

        protected void BtnStart_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(DdlGuide.SelectedValue, out int guideId) || guideId == 0)
            {
                ShowError("Selecciona una guía válida.");
                return;
            }

            var mode = RbAssessment.Checked ? SessionMode.Assessment : SessionMode.Guided;
            int userId = (int)Session[Constants.SessionKeys.UserId];

            // D-9 y AUD-41: deja una sola sesión en curso, también en el estado del POD.
            var session = new PodService(new SessionPodStateStore(Session)).StartSession(userId, guideId, mode);
            Response.Redirect("~/Pages/Training/GuidedTest.aspx?sessionId=" + session.Id);
        }

        protected void BtnLogout_Click(object sender, EventArgs e)
        {
            SessionHelper.Logout();
        }

        private void LoadProducts()
        {
            var products = _guideService.GetAllProducts();
            DdlProduct.Items.Clear();
            DdlProduct.Items.Add(new ListItem("-- Selecciona un producto --", "0"));
            foreach (var p in products)
                DdlProduct.Items.Add(new ListItem(p.Name, p.Id.ToString()));
        }

        private void LoadAreas()
        {
            DdlArea.Items.Clear();
            DdlGuide.Items.Clear();
            LblNoGuides.Visible = false;

            if (!int.TryParse(DdlProduct.SelectedValue, out int productId) || productId == 0)
            {
                DdlArea.Items.Add(new ListItem("-- Selecciona un producto primero --", "0"));
                return;
            }

            var areas = _guideService.GetAreasByProduct(productId);
            DdlArea.Items.Add(new ListItem("-- Selecciona un área --", "0"));
            foreach (var a in areas)
                DdlArea.Items.Add(new ListItem(a.Name, a.Id.ToString()));

            LoadGuides();
        }

        private void LoadGuides()
        {
            DdlGuide.Items.Clear();
            LblNoGuides.Visible = false;

            if (!int.TryParse(DdlArea.SelectedValue, out int areaId) || areaId == 0)
            {
                DdlGuide.Items.Add(new ListItem("-- Selecciona un área primero --", "0"));
                return;
            }

            var guides = _guideService.GetByArea(areaId);
            if (guides.Count == 0)
            {
                LblNoGuides.Visible = true;
                return;
            }

            foreach (var g in guides)
                DdlGuide.Items.Add(new ListItem(g.Title, g.Id.ToString()));
        }

        private void ShowError(string message)
        {
            LblError.Text    = message;
            LblError.Visible = true;
        }
    }
}
