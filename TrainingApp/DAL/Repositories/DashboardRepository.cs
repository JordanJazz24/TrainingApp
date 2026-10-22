using System;
using System.Collections.Generic;
using TrainingApp.DAL.Infrastructure;
using TrainingApp.Domain;

namespace TrainingApp.DAL.Repositories
{
    // SPs de 10_spDashboard.sql (spec 018), de solo lectura.
    public class DashboardRepository : IDashboardRepository
    {
        public IList<DashboardGuide> GetGuides()
        {
            var guides = new List<DashboardGuide>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spDashboardGuides"))
            {
                while (reader.Read())
                    guides.Add(new DashboardGuide
                    {
                        Id          = Convert.ToInt32(reader["Id"]),
                        Title       = reader["Title"].ToString(),
                        AreaName    = reader["AreaName"].ToString(),
                        ProductName = reader["ProductName"].ToString()
                    });
            }
            return guides;
        }

        public IList<DashboardOperator> GetOperators(string text)
        {
            var operators = new List<DashboardOperator>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spDashboardOperators",
                cmd => cmd.Parameters.AddWithValue("@Text", (object)text ?? DBNull.Value)))
            {
                while (reader.Read())
                    operators.Add(new DashboardOperator
                    {
                        UserId    = Convert.ToInt32(reader["Id"]),
                        Code1     = reader["Code1"].ToString(),
                        Name      = reader["Name"].ToString(),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    });
            }
            return operators;
        }

        public IList<DashboardSession> GetSessions(int? userId, bool includeFailedAttempts)
        {
            var sessions = new List<DashboardSession>();
            using (var db = DatabaseFactory.Create())
            using (var reader = db.ExecuteReader("spDashboardSessions", cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId",                (object)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IncludeFailedAttempts", includeFailedAttempts);
            }))
            {
                while (reader.Read())
                    sessions.Add(new DashboardSession
                    {
                        SessionId       = Convert.ToInt32(reader["SessionId"]),
                        UserId          = Convert.ToInt32(reader["UserId"]),
                        TrainingGuideId = Convert.ToInt32(reader["TrainingGuideId"]),
                        Mode            = (SessionMode)Convert.ToByte(reader["Mode"]),
                        Status          = (SessionStatus)Convert.ToByte(reader["Status"]),
                        FinalScore      = Convert.ToDecimal(reader["FinalScore"]),
                        StartedAt       = Convert.ToDateTime(reader["StartedAt"]),
                        CompletedAt     = reader["CompletedAt"] == DBNull.Value
                            ? (DateTime?)null : Convert.ToDateTime(reader["CompletedAt"]),
                        FailedAttempts  = reader["FailedAttempts"] == DBNull.Value
                            ? (int?)null : Convert.ToInt32(reader["FailedAttempts"])
                    });
            }
            return sessions;
        }
    }
}
