using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class AssemblyComponentRepository
    {
        public IList<AssemblyComponent> GetByArea(int areaId)
        {
            var result = new List<AssemblyComponent>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spAssemblyComponentGetByArea",
                cmd => cmd.Parameters.AddWithValue("@AreaId", areaId)))
            {
                while (reader.Read())
                    result.Add(MapComponent(reader));
            }
            return result;
        }

        private static AssemblyComponent MapComponent(SqlDataReader r) => new AssemblyComponent
        {
            Id           = Convert.ToInt32(r["Id"]),
            AreaId       = Convert.ToInt32(r["AreaId"]),
            OperationId  = r["OperationId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["OperationId"]),
            PartNumber   = r["PartNumber"].ToString(),
            Revision     = r["Revision"].ToString(),
            Description  = r["Description"].ToString(),
            Qty          = Convert.ToDecimal(r["Qty"]),
            DisplayOrder = Convert.ToInt32(r["DisplayOrder"])
        };
    }
}
