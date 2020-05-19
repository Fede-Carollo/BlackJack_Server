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

        public Player ReadPlayer(string email, string password)
        {
            conn = DbUtilities.InstanceSqlConn();
            myCommand = conn.CreateCommand();
            try
            {
                myCommand.Parameters.AddWithValue("@email", email);
                myCommand.Parameters.AddWithValue("@password", password);
                myCommand.CommandType = CommandType.StoredProcedure;
                myCommand.CommandText = "Player_ReadByCredentials";
                p = new Player();
                using (SqlDataReader dr = myCommand.ExecuteReader())
                {
                    dr.Read();
                    if (!dr.HasRows)
                        return null;
                    p.Email = dr["email"].ToString();
                    p.Username = dr["username"].ToString();
                    p.Password = dr["password"].ToString();
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
    }
}
