using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Progetto_Alternanza
{
    public static class DbUtilities
    {
        /// <summary>
        /// Ritorna l'istanza del DB del BJ
        /// </summary>
        /// <param name="dbName">Nome del Database</param>
        /// <returns></returns>
        public static SqlConnection InstanceSqlConn(string dbName = "BlackJack.mdf")
        {
            string name = Application.StartupPath;
            name = name.Substring(0, name.Length - 9);
            name += @"AppData\"+dbName;
            string connStr = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={name};Integrated Security=True";
            return new SqlConnection(connStr);
        }
    }
}
