using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MES_TRAINING
{
    public partial class index : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        [WebMethod]
        public static string One(int code1, string password)
        {
            DataSet ds = DAL.User.One(code1, password);

            return ds.GetXml();

        }
        [WebMethod]
        public static void Update(int code1, string password,string area)
        {
            DAL.User.Update(code1,password,area);

       

        }
    }
}