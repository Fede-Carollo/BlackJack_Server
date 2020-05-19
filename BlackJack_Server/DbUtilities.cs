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
