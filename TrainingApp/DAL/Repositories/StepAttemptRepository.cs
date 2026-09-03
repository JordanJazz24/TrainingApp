using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class StepAttemptRepository
    {
        public void Insert(TrainingSessionStepAttempt attempt)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spStepAttemptInsert", cmd =>
                {
                    cmd.Parameters.AddWithValue("@SessionId",     attempt.SessionId);
                    cmd.Parameters.AddWithValue("@StepId",        attempt.StepId);
                    cmd.Parameters.AddWithValue("@AttemptNumber", attempt.AttemptNumber);
                    cmd.Parameters.AddWithValue("@IsCorrect",     attempt.IsCorrect);
                    cmd.Parameters.AddWithValue("@Timestamp",     attempt.Timestamp);
                });
        }

        public IList<TrainingSessionStepAttempt> GetBySessionId(int sessionId)
        {
            var list = new List<TrainingSessionStepAttempt>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spStepAttemptGetBySession",
                cmd => cmd.Parameters.AddWithValue("@SessionId", sessionId)))
            {
                while (reader.Read())
                    list.Add(Map(reader));
            }
            return list;
        }

        public IList<TrainingSessionStepAttempt> GetBySessionAndStep(int sessionId, int stepId)
        {
            var list = new List<TrainingSessionStepAttempt>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spStepAttemptGetBySessionAndStep", cmd =>
            {
                cmd.Parameters.AddWithValue("@SessionId", sessionId);
                cmd.Parameters.AddWithValue("@StepId",    stepId);
            }))
            {
                while (reader.Read())
                    list.Add(Map(reader));
            }
            return list;
        }

        private static TrainingSessionStepAttempt Map(SqlDataReader r) =>
            new TrainingSessionStepAttempt
            {
                Id            = Convert.ToInt32(r["Id"]),
                SessionId     = Convert.ToInt32(r["SessionId"]),
                StepId        = Convert.ToInt32(r["StepId"]),
                AttemptNumber = Convert.ToInt32(r["AttemptNumber"]),
                IsCorrect     = Convert.ToBoolean(r["IsCorrect"]),
                Timestamp     = Convert.ToDateTime(r["Timestamp"])
            };
    }
}
