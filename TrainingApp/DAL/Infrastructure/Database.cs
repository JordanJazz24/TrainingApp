using System;
using System.Data;
using System.Data.SqlClient;

namespace TrainingApp.DAL.Infrastructure
{
    public class Database : IDisposable
    {
        private readonly SqlConnection _connection;

        public Database(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();
        }

        public SqlDataReader ExecuteReader(string storedProcedure, Action<SqlCommand> parameterize = null)
        {
            var cmd = CreateCommand(storedProcedure, parameterize);
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public int ExecuteNonQuery(string storedProcedure, Action<SqlCommand> parameterize = null)
        {
            using (var cmd = CreateCommand(storedProcedure, parameterize))
                return cmd.ExecuteNonQuery();
        }

        public object ExecuteScalar(string storedProcedure, Action<SqlCommand> parameterize = null)
        {
            using (var cmd = CreateCommand(storedProcedure, parameterize))
                return cmd.ExecuteScalar();
        }

        private SqlCommand CreateCommand(string storedProcedure, Action<SqlCommand> parameterize)
        {
            var cmd = new SqlCommand(storedProcedure, _connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            parameterize?.Invoke(cmd);
            return cmd;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
