using System;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class TrainingSessionRepository
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
