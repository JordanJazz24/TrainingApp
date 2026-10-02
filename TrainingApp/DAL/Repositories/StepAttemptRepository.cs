using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    public class StepAttemptRepository
    {
        // Guarda el intento y, en la misma transacción, avanza la sesión a nextStepOrder o la
        // cierra con finalScore (auditoría AUD-06). Ambos null = solo el intento. stepOrder es
        // el paso en el que la BLL tomó la decisión: el SP falla si la sesión ya no está ahí.
        public void Record(TrainingSessionStepAttempt attempt, int stepOrder, int? nextStepOrder,
            decimal? finalScore)
        {
            using (var db = DatabaseFactory.Create())
                db.ExecuteNonQuery("spStepAttemptRecord", cmd =>
                {
                    cmd.Parameters.AddWithValue("@SessionId",     attempt.SessionId);
                    cmd.Parameters.AddWithValue("@StepId",        attempt.StepId);
                    cmd.Parameters.AddWithValue("@StepOrder",     stepOrder);
                    cmd.Parameters.AddWithValue("@AttemptNumber", attempt.AttemptNumber);
                    cmd.Parameters.AddWithValue("@IsCorrect",     attempt.IsCorrect);
                    cmd.Parameters.AddWithValue("@Timestamp",     attempt.Timestamp);
                    cmd.Parameters.AddWithValue("@NextStepOrder", (object)nextStepOrder ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FinalScore",    (object)finalScore ?? DBNull.Value);
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
