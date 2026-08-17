using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace PruebaUsuario.DAL
{
    class DatabaseFactory
    {
        // nombre = nombre de la cadena de conexion almacenada en el web.config
        public static Database CreateDatabase(string nombre)
        {

            String con = "";
            try
            {
                Database db = new Database();

                con = System.Configuration.ConfigurationManager.ConnectionStrings[nombre].ToString();
                //con = "Data Source=localhost;Initial Catalog=Ventas;User ID=sa;Password=123456";

                SqlConnection conexion = new SqlConnection(con);

                conexion.Open();

                db.Conexion = conexion;

                if (conexion.State != ConnectionState.Open)
                {
                    throw new Exception("No se pudo abrir la Base de Datos, revise los parámetros de conexión! ");
                }
               

                    return db;
            }
            catch (Exception ex)
            {
                ex.Source += " Conexion " + con + "Parámetro :" + nombre;

                throw ex;

            }
            finally
            {

            }

        }

        public static Database CreateDatabase(string nombre, String usuario, String contrasena)
        {
            String con = "";
            try
            {

                Database db = new Database();
                con = ConfigurationManager.ConnectionStrings[nombre].ToString();
                con = con + String.Format("User Id={0};Password={1};", usuario, contrasena);

                SqlConnection conexion = new SqlConnection(con);
                conexion.Open();

                db.Conexion = conexion;

                if (conexion.State != ConnectionState.Open)
                {

                    throw new Exception("No se pudo abrir la Base de Datos, revise los parámetros de conexión! ");
                }

                return db;
            }
            catch (Exception ex)
            {
                ex.Source += " Conexion " + con + "Parámetro :" + nombre;

                throw ex;

            }

        }
    }

}