using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MES_TRAINING
{
    public partial class loginAdmin : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        [WebMethod]
        public static bool UserAuthentication(string Code1, string Password)
        {
            string path = @"LDAP://code1.emi.philips.com";

            string dominio = @"code1";

            string domUsu = dominio + @"\" + Code1;

            DirectoryEntry de = new DirectoryEntry(path, domUsu, Password, System.DirectoryServices.AuthenticationTypes.Secure);
            try
            {
                DirectorySearcher ds = new DirectorySearcher(de);
                ds.FindOne();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}