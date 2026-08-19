using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MES_TRAINING.UI.DashboardAdmin
{
    public partial class main : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        [WebMethod]
        public static string All()
        {
            DataSet ds = DAL.User.All();
            return ds.GetXml();

        }

        [WebMethod]
        public static void Update(int code1, string password, string area)
        {
            DAL.User.Update(code1, password, area);



        }
        [WebMethod]
        public static void Delete(int code1)
        {
            DAL.User.Delete(code1);



        }
    }
}