using System.Configuration;

namespace TrainingApp.DAL.Infrastructure
{
    public static class DatabaseFactory
    {
        public static Database Create()
        {
            string cs = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
            return new Database(cs);
        }
    }
}
