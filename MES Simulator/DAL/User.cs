using PruebaUsuario.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace MES_TRAINING.DAL
{
    public class User
    {


        // ONE
        public static DataSet One(int code1,string password)
        {
            Database db = DatabaseFactory.CreateDatabase("Default");

            SqlCommand comando = new SqlCommand("spUserOne");

            comando.CommandType = CommandType.StoredProcedure;
            comando.Parameters.AddWithValue("@code1", code1);
            comando.Parameters.AddWithValue("@password", password);

            DataSet ds = db.ExecuteReader(comando, "Info");
            return ds;
        }
        //All
        public static DataSet All()
        {
            Database db = DatabaseFactory.CreateDatabase("Default");

            SqlCommand comando = new SqlCommand("spUserAll");

            comando.CommandType = CommandType.StoredProcedure;


            DataSet ds = db.ExecuteReader(comando, "Info");
            return ds;
        }

        // Update
        public static void Update(int code1, string password,string area)
        {
            Database db = DatabaseFactory.CreateDatabase("Default");

            SqlCommand comando = new SqlCommand("spUserNewUpdate");

            comando.CommandType = CommandType.StoredProcedure;
            comando.Parameters.AddWithValue("@code1", code1);
            comando.Parameters.AddWithValue("@password", password);
            comando.Parameters.AddWithValue("@area", area);
           

            db.ExecuteNonQuery(comando);
        }
        // Delete
        public static void Delete(int code1)
        {
            Database db = DatabaseFactory.CreateDatabase("Default");

            SqlCommand comando = new SqlCommand("spUserDelete");

            comando.CommandType = CommandType.StoredProcedure;
            comando.Parameters.AddWithValue("@code1", code1);
  
           

            db.ExecuteNonQuery(comando);
        }

    }
}