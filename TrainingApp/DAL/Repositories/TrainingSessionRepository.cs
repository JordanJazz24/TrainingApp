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
            var id = DatabaseFactory.Create().ExecuteScalar("spTrainingSessionInsert", cmd =>
            {
                cmd.Parameters.AddWithValue("@OperatorId",      session.OperatorId);
                cmd.Parameters.AddWithValue("@TrainingGuideId", session.TrainingGuideId);
                cmd.Parameters.AddWithValue("@Mode",            (int)session.Mode);
                cmd.Parameters.AddWithValue("@Status",          (int)session.Status);
                cmd.Parameters.AddWithValue("@CurrentStepOrder",session.CurrentStepOrder);
                cmd.Parameters.AddWithValue("@StartedAt",       session.StartedAt);
            });
            return Convert.ToInt32(id);
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

        public void UpdateCurrentStep(int sessionId, int nextStepOrder)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spTrainingSessionUpdateStep", cmd =>
                {
                    cmd.Parameters.AddWithValue("@SessionId",       sessionId);
                    cmd.Parameters.AddWithValue("@CurrentStepOrder",nextStepOrder);
                });
        }

        public void Complete(int sessionId, decimal finalScore)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spTrainingSessionComplete", cmd =>
                {
                    cmd.Parameters.AddWithValue("@SessionId",  sessionId);
                    cmd.Parameters.AddWithValue("@FinalScore", finalScore);
                    cmd.Parameters.AddWithValue("@CompletedAt",DateTime.UtcNow);
                });
        }

        private static TrainingSession Map(SqlDataReader r) => new TrainingSession
        {
            Id              = Convert.ToInt32(r["Id"]),
            OperatorId      = Convert.ToInt32(r["OperatorId"]),
            TrainingGuideId = Convert.ToInt32(r["TrainingGuideId"]),
            Mode            = (SessionMode)Convert.ToInt32(r["Mode"]),
            Status          = (SessionStatus)Convert.ToInt32(r["Status"]),
            CurrentStepOrder= Convert.ToInt32(r["CurrentStepOrder"]),
            FinalScore      = Convert.ToDecimal(r["FinalScore"]),
            StartedAt       = Convert.ToDateTime(r["StartedAt"]),
            CompletedAt     = r["CompletedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CompletedAt"])
        };
    }
}
