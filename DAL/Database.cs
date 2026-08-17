using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace PruebaUsuario.DAL
{
    class Database : IDisposable
    {
        public SqlConnection Conexion { get; set; }

        public DataSet ExecuteReader(SqlCommand sqlCommand, String tabla)
        {

            DataSet dsTabla = new DataSet();
            try
            {
                using (SqlDataAdapter adaptador = new SqlDataAdapter(sqlCommand))
                {
                    sqlCommand.Connection = Conexion;
                    dsTabla = new DataSet();
                    adaptador.Fill(dsTabla, tabla);
                }
                return dsTabla;
            }
            catch (Exception ex)
            {
                ex.Source += " SQL: " + sqlCommand.CommandText.ToString();
                throw ex;
            }
            finally
            {

                if (dsTabla != null || sqlCommand != null)
                    dsTabla.Dispose();
                sqlCommand.Dispose();

            }



        }

        public int ExecuteNonQuery(SqlCommand sqlCommand)
        {

            int registrosafectados = 0;
            try
            {

                sqlCommand.Connection = Conexion;

                registrosafectados = sqlCommand.ExecuteNonQuery();

                return registrosafectados;

            }
            catch (Exception ex)
            {
                ex.Source += " SQL: " + sqlCommand.CommandText.ToString();
                throw ex;
            }
            finally
            {

                if (sqlCommand != null)
                    sqlCommand.Dispose();

            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (Conexion != null)
                Conexion.Close();
        }

        #endregion
    }


}