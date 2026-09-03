using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class TrainingGuideRepository
    {
        // Returns the guide AND populates Steps (see CLAUDE.md convention:
        // TrainingGuide.Steps must always be populated by the repository).
        public TrainingGuide GetById(int id)
        {
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spTrainingGuideGetById",
                cmd => cmd.Parameters.AddWithValue("@Id", id)))
            {
                if (!reader.Read()) return null;
                var guide = MapGuide(reader);

                // Second result set: steps for this guide
                if (reader.NextResult())
                {
                    guide.Steps = new List<TrainingStep>();
                    while (reader.Read())
                        guide.Steps.Add(MapStep(reader));
                }
                return guide;
            }
        }

        // Returns guides without steps (list view — steps loaded on demand)
        public IList<TrainingGuide> GetByArea(int areaId)
        {
            var guides = new List<TrainingGuide>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spTrainingGuideGetByArea",
                cmd => cmd.Parameters.AddWithValue("@AreaId", areaId)))
            {
                while (reader.Read())
                    guides.Add(MapGuide(reader));
            }
            return guides;
        }

        private static TrainingGuide MapGuide(SqlDataReader r) => new TrainingGuide
        {
            Id          = Convert.ToInt32(r["Id"]),
            AreaId      = Convert.ToInt32(r["AreaId"]),
            OperationId = r["OperationId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["OperationId"]),
            Title       = r["Title"].ToString(),
            IsPublished = Convert.ToBoolean(r["IsPublished"]),
            CreatedAt   = Convert.ToDateTime(r["CreatedAt"])
        };

        private static TrainingStep MapStep(SqlDataReader r) => new TrainingStep
        {
            Id              = Convert.ToInt32(r["Id"]),
            TrainingGuideId = Convert.ToInt32(r["TrainingGuideId"]),
            StepType        = (StepType)Convert.ToInt32(r["StepType"]),
            Order           = Convert.ToInt32(r["Order"]),
            Weight          = Convert.ToDecimal(r["Weight"])
        };
    }
}
