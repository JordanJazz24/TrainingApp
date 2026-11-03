using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class TrainingSessionRepository : ITrainingSessionRepository
    {
        public int Insert(TrainingSession session)
        {
            using (var db = DatabaseFactory.Create())
            {
                var id = db.ExecuteScalar("spTrainingSessionInsert", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId",           session.UserId);
                    cmd.Parameters.AddWithValue("@TrainingGuideId",  session.TrainingGuideId);
                    cmd.Parameters.AddWithValue("@Mode",             (int)session.Mode);
                    cmd.Parameters.AddWithValue("@Status",           (int)session.Status);
                    cmd.Parameters.AddWithValue("@CurrentStepOrder", session.CurrentStepOrder);
                    cmd.Parameters.AddWithValue("@StartedAt",        session.StartedAt);
                    cmd.Parameters.AddWithValue("@ResourceSetupType", (int)session.ResourceSetupType);
                });
                return Convert.ToInt32(id);
            }
        }

        public TrainingSession GetById(int id)
        {
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spTrainingSessionGetById",
                cmd => cmd.Parameters.AddWithValue("@Id", id)))
            {
                if (reader.Read()) return Map(reader);
                return null;
            }
        }

        // Las últimas sesiones completadas del operador, de la más reciente (spec 025, P-4).
        public IList<CompletedSessionItem> GetRecentCompleted(int userId, int top)
        {
            var list = new List<CompletedSessionItem>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spTrainingSessionGetRecentCompleted", cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Top",    top);
            }))
            {
                while (reader.Read())
                    list.Add(new CompletedSessionItem
                    {
                        SessionId       = Convert.ToInt32(reader["SessionId"]),
                        TrainingGuideId = Convert.ToInt32(reader["TrainingGuideId"]),
                        Mode            = (SessionMode)Convert.ToInt32(reader["Mode"]),
                        CompletedAt     = Convert.ToDateTime(reader["CompletedAt"]),
                        AreaId          = Convert.ToInt32(reader["AreaId"]),
                        AreaName        = reader["AreaName"].ToString(),
                        ProductName     = reader["ProductName"].ToString()
                    });
            }
            return list;
        }

        public void Abandon(int sessionId)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spTrainingSessionAbandon",
                    cmd => cmd.Parameters.AddWithValue("@SessionId", sessionId));
        }

        public void AbandonActiveByUser(int userId)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spTrainingSessionAbandonActiveByUser",
                    cmd => cmd.Parameters.AddWithValue("@UserId", userId));
        }

        private static TrainingSession Map(SqlDataReader r) => new TrainingSession
        {
            Id              = Convert.ToInt32(r["Id"]),
            UserId          = Convert.ToInt32(r["UserId"]),
            TrainingGuideId = Convert.ToInt32(r["TrainingGuideId"]),
            Mode            = (SessionMode)Convert.ToInt32(r["Mode"]),
            Status          = (SessionStatus)Convert.ToInt32(r["Status"]),
            CurrentStepOrder= Convert.ToInt32(r["CurrentStepOrder"]),
            FinalScore      = Convert.ToDecimal(r["FinalScore"]),
            StartedAt       = Convert.ToDateTime(r["StartedAt"]),
            CompletedAt     = r["CompletedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CompletedAt"]),
            ResourceSetupType = (ResourceSetupType)Convert.ToInt32(r["ResourceSetupType"])
        };
    }
}
