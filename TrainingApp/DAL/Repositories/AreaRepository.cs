using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class AreaRepository
    {
        public Area GetById(int id)
        {
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spAreaGetById",
                cmd => cmd.Parameters.AddWithValue("@Id", id)))
            {
                if (!reader.Read()) return null;
                return MapArea(reader);
            }
        }

        public IList<Area> GetByProduct(int productId)
        {
            var list = new List<Area>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spAreaGetByProduct",
                cmd => cmd.Parameters.AddWithValue("@ProductId", productId)))
            {
                while (reader.Read())
                    list.Add(MapArea(reader));
            }
            return list;
        }

        private static Area MapArea(SqlDataReader r) => new Area
        {
            Id                     = Convert.ToInt32(r["Id"]),
            ProductId              = Convert.ToInt32(r["ProductId"]),
            Name                   = r["Name"].ToString(),
            SerializationType      = (SerializationType)Convert.ToInt32(r["SerializationType"]),
            RequiresAssembly       = Convert.ToBoolean(r["RequiresAssembly"]),
            RequiresDataCollection = Convert.ToBoolean(r["RequiresDataCollection"]),
            RequiresStartChecklist = Convert.ToBoolean(r["RequiresStartChecklist"])
        };
    }
}
