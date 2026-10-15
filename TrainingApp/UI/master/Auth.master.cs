using System.Web;

namespace TrainingApp.UI.Master
{
    public partial class AuthMaster : AntiXsrfMasterPage
    {
        // Errores del servidor (código no registrado, contraseña incorrecta…) en un
        // SweetAlert, como el login del legacy. Los de formato quedan bajo cada campo.
        public void ShowError(string message)
        {
            string script = string.Format(
                "Swal.fire({{ icon: 'error', title: 'Error', text: '{0}' }});",
                HttpUtility.JavaScriptStringEncode(message));
            Page.ClientScript.RegisterStartupScript(GetType(), "authError", script, true);
        }

        // Error con un enlace al camino correcto (spec 013: un admin en el login de SAP).
        public void ShowError(string message, string linkText, string linkUrl)
        {
            string html = HttpUtility.HtmlEncode(message) + "<br /><a href=\"" +
                HttpUtility.HtmlAttributeEncode(linkUrl) + "\">" + HttpUtility.HtmlEncode(linkText) + "</a>";
            string script = string.Format(
                "Swal.fire({{ icon: 'error', title: 'Error', html: '{0}' }});",
                HttpUtility.JavaScriptStringEncode(html));
            Page.ClientScript.RegisterStartupScript(GetType(), "authError", script, true);
        }
    }
}
