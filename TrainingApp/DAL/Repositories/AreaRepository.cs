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

        // Áreas con al menos una guía publicada, por producto y área (spec 021, D-176).
        public IList<EligibleArea> GetEligible()
        {
            var list = new List<EligibleArea>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spAreaGetEligible"))
            {
                while (reader.Read())
                    list.Add(new EligibleArea
                    {
                        ProductId   = Convert.ToInt32(reader["ProductId"]),
                        ProductName = reader["ProductName"].ToString(),
                        AreaId      = Convert.ToInt32(reader["AreaId"]),
                        AreaName    = reader["AreaName"].ToString()
                    });
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
