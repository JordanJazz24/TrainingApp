using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class ProductRepository
    {
        public IList<Product> GetAll()
        {
            var list = new List<Product>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spProductGetAll", cmd => { }))
            {
                while (reader.Read())
                    list.Add(Map(reader));
            }
            return list;
        }

        private static Product Map(SqlDataReader r) => new Product
        {
            Id   = Convert.ToInt32(r["Id"]),
            Name = r["Name"].ToString()
        };
    }
}
