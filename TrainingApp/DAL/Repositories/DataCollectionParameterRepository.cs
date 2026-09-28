using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class DataCollectionParameterRepository
    {
        public IList<DataCollectionParameter> GetByArea(int areaId)
        {
            var result = new List<DataCollectionParameter>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spDataCollectionParameterGetByArea",
                cmd => cmd.Parameters.AddWithValue("@AreaId", areaId)))
            {
                while (reader.Read())
                    result.Add(MapParam(reader));
            }
            return result;
        }

        private static DataCollectionParameter MapParam(SqlDataReader r) => new DataCollectionParameter
        {
            Id           = Convert.ToInt32(r["Id"]),
            AreaId       = Convert.ToInt32(r["AreaId"]),
            OperationId  = r["OperationId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["OperationId"]),
            Name         = r["Name"].ToString(),
            DataType     = r["DataType"].ToString(),
            Unit         = r["Unit"].ToString(),
            MinValue     = r["MinValue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["MinValue"]),
            MaxValue     = r["MaxValue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["MaxValue"]),
            DisplayOrder = Convert.ToInt32(r["DisplayOrder"]),
            DataCollectionGroupId = Convert.ToInt32(r["DataCollectionGroupId"])
        };
    }
}
