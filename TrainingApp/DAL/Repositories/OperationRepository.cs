using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class OperationRepository
    {
        // De la actual (mayor Sequence) a las anteriores (spec 014, D-58).
        public IList<Operation> GetByArea(int areaId)
        {
            var result = new List<Operation>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spOperationGetByArea",
                cmd => cmd.Parameters.AddWithValue("@AreaId", areaId)))
            {
                while (reader.Read())
                    result.Add(MapOperation(reader));
            }
            return result;
        }

        private static Operation MapOperation(SqlDataReader r) => new Operation
        {
            Id          = Convert.ToInt32(r["Id"]),
            AreaId      = Convert.ToInt32(r["AreaId"]),
            Name        = r["Name"].ToString(),
            Sequence    = r["Sequence"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Sequence"]),
            Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString()
        };
    }
}
