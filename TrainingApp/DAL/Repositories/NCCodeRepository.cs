using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class NCCodeRepository
    {
        public IList<NCCode> GetByArea(int areaId)
        {
            var result = new List<NCCode>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spNCCodeGetByArea",
                cmd => cmd.Parameters.AddWithValue("@AreaId", areaId)))
            {
                while (reader.Read())
                    result.Add(MapNCCode(reader));
            }
            return result;
        }

        private static NCCode MapNCCode(SqlDataReader r) => new NCCode
        {
            Id          = Convert.ToInt32(r["Id"]),
            AreaId      = Convert.ToInt32(r["AreaId"]),
            Code        = r["Code"].ToString(),
            Description      = r["Description"].ToString(),
            Method           = (NCMethod)Convert.ToByte(r["Method"]),
            RequiresComments = Convert.ToBoolean(r["RequiresComments"])
        };
    }
}
