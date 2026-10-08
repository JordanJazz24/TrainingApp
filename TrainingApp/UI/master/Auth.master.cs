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
    }
}
