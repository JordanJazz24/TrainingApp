using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class DataCollectionGroupRepository
    {
        public IList<DataCollectionGroup> GetByArea(int areaId)
        {
            var result = new List<DataCollectionGroup>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spDataCollectionGroupGetByArea",
                cmd => cmd.Parameters.AddWithValue("@AreaId", areaId)))
            {
                while (reader.Read())
                    result.Add(MapGroup(reader));
            }
            return result;
        }

        private static DataCollectionGroup MapGroup(SqlDataReader r) => new DataCollectionGroup
        {
            Id            = Convert.ToInt32(r["Id"]),
            AreaId        = Convert.ToInt32(r["AreaId"]),
            OperationId   = r["OperationId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["OperationId"]),
            Code          = r["Code"].ToString(),
            Description   = r["Description"].ToString(),
            CollectDataAt = r["CollectDataAt"].ToString(),
            IsRequired    = Convert.ToBoolean(r["IsRequired"]),
            DisplayOrder  = Convert.ToInt32(r["DisplayOrder"])
        };
    }
}
