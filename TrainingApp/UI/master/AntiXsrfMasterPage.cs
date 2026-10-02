using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;

namespace TrainingApp.UI.Master
{
    /// <summary>
    /// Protección anti-CSRF del template de Web Forms (auditoría AUD-17): ata el ViewState de
    /// cada página a un token por navegador (cookie HttpOnly) y al usuario autenticado, así
    /// un postback armado desde otro sitio no pasa la validación.
    /// </summary>
    public abstract class AntiXsrfMasterPage : MasterPage
    {
        private const string AntiXsrfTokenKey    = "__AntiXsrfToken";
        private const string AntiXsrfUserNameKey = "__AntiXsrfUserName";

        private string _antiXsrfTokenValue;

        // El Init del master corre antes que el de la página: ViewStateUserKey queda fijado
        // antes de que se cargue el ViewState del postback.
        protected override void OnInit(EventArgs e)
        {
            var requestCookie = Request.Cookies[AntiXsrfTokenKey];
            Guid requestToken;
            if (requestCookie != null && Guid.TryParse(requestCookie.Value, out requestToken))
            {
                _antiXsrfTokenValue = requestCookie.Value;
            }
            else
            {
                _antiXsrfTokenValue = Guid.NewGuid().ToString("N");
                Response.Cookies.Set(new HttpCookie(AntiXsrfTokenKey)
                {
                    HttpOnly = true,
                    Value    = _antiXsrfTokenValue,
                    Secure   = FormsAuthentication.RequireSSL && Request.IsSecureConnection
                });
            }

            Page.ViewStateUserKey = _antiXsrfTokenValue;
            Page.PreLoad += Page_PreLoad;
            base.OnInit(e);
        }

        private void Page_PreLoad(object sender, EventArgs e)
        {
            string userName = Context.User?.Identity?.Name ?? string.Empty;

            if (!IsPostBack)
            {
                ViewState[AntiXsrfTokenKey]    = Page.ViewStateUserKey;
                ViewState[AntiXsrfUserNameKey] = userName;
            }
            else if ((string)ViewState[AntiXsrfTokenKey] != _antiXsrfTokenValue
                     || (string)ViewState[AntiXsrfUserNameKey] != userName)
            {
                throw new InvalidOperationException("Validation of Anti-XSRF token failed.");
            }
        }
    }
}
