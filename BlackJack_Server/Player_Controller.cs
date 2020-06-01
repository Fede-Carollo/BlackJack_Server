using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using Progetto_Alternanza;
using System.Data;

namespace BlackJack_Server
{
    public class Player_Controller
    {
        Player p;
        SqlConnection conn;
        SqlCommand myCommand;

        internal Player ReadPlayer_ByEmailAndPass(string email, string password)
        {
            conn = DbUtilities.InstanceSqlConn();
            conn.Open();
            myCommand = conn.CreateCommand();
            try
            {
                myCommand.Parameters.AddWithValue("@email", email);
                myCommand.Parameters.AddWithValue("@password", password);
                myCommand.CommandType = CommandType.StoredProcedure;
                myCommand.CommandText = "Player_ReadByEmailAndPass";
                p = new Player();
                using (SqlDataReader dr = myCommand.ExecuteReader())
                {
                    dr.Read();
                    if (!dr.HasRows)
                        return null;
                    p.Email = dr["email"].ToString();
                    p.Username = dr["username"].ToString();
                    p.Password = dr["pass"].ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                myCommand.Dispose();
                conn.Close();
                conn = null;
            }
            return p;
        }
        internal Player ReadPlayer_ByUsernameAndPass(string username, string password)
        {
            conn = DbUtilities.InstanceSqlConn();
            conn.Open();
            myCommand = conn.CreateCommand();
            try
            {
                myCommand.Parameters.AddWithValue("@username", username);
                myCommand.Parameters.AddWithValue("@password", password);
                myCommand.CommandType = CommandType.StoredProcedure;
                myCommand.CommandText = "Player_ReadByUsernameAndPass";
                p = new Player();
                using (SqlDataReader dr = myCommand.ExecuteReader())
                {
                    dr.Read();
                    if (!dr.HasRows)
                        return null;
                    p.Email = dr["email"].ToString();
                    p.Username = dr["username"].ToString();
                    p.Password = dr["pass"].ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                myCommand.Dispose();
                conn.Close();
                conn = null;
            }
            return p;
        }

        internal void CreatePlayer(string username, string email, string password)
        {
            conn = DbUtilities.InstanceSqlConn();
            conn.Open();
            myCommand = conn.CreateCommand();
            try
            {
                myCommand.Parameters.AddWithValue("@username", username);
                myCommand.Parameters.AddWithValue("@email", email);
                myCommand.Parameters.AddWithValue("@password", password);
                myCommand.CommandType = CommandType.StoredProcedure;
                myCommand.CommandText = "Player_Create";
                myCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                myCommand.Dispose();
                conn.Close();
                conn = null;
            }
        }

        internal bool UsernameExisting(string username)
        {
            conn = DbUtilities.InstanceSqlConn();
            conn.Open();
            myCommand = conn.CreateCommand();
            try
            {
                myCommand.Parameters.AddWithValue("@username", username);
                myCommand.CommandType = CommandType.StoredProcedure;
                myCommand.CommandText = "Player_ReadByUsername";
                using (SqlDataReader dr = myCommand.ExecuteReader())
                {
                    return dr.HasRows;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                myCommand.Dispose();
                conn.Close();
                conn = null;
            }
            return false;
        }

        internal bool EmailExisting(string email)
        {
            conn = DbUtilities.InstanceSqlConn();
            conn.Open();
            myCommand = conn.CreateCommand();
            try
            {
                myCommand.Parameters.AddWithValue("@email", email);
                myCommand.CommandType = CommandType.StoredProcedure;
                myCommand.CommandText = "Player_ReadByEmail";
                p = new Player();
                using (SqlDataReader dr = myCommand.ExecuteReader())
                {
                    return dr.HasRows;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                myCommand.Dispose();
                conn.Close();
                conn = null;
            }
            return false;
        }
    }
}
