using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackJack_Server
{
    internal class Player
    {
        string _username;
        string _password;

        public string Username { get => _username; set => _username = value; }
        public string Password { get => _password; set => _password = value; }

        public Player(string usern, string pass)
        {
            this._username = usern;
            this._password = pass;
        }
    }
}
