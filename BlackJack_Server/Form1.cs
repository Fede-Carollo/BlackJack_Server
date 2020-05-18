using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using SOCKET_UDP;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace BlackJack_Server
{
    public partial class Form1 : Form
    {
        clsServerUDP server;
        List<clsClientUDP> beforeLogin;
        Dictionary<Player, clsClientUDP> lobby;

        public Form1()
        {
            InitializeComponent();
            server = new clsServerUDP(IPAddress.Parse(GetLocalIPAddress()), 7777);
            beforeLogin = new List<clsClientUDP>();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            server.avvia();
            server.datiRicevutiEvent += Server_datiRicevutiEvent;
        }

        private void Server_datiRicevutiEvent(ClsMessaggio message)
        {
            string[] ricevuti = message.toArray();
            ObjMex msg = JsonConvert.DeserializeObject<ObjMex>(ricevuti[2]);
            switch(msg.Action)
            {
                case "new-conn":
                    clsClientUDP client = new clsClientUDP(IPAddress.Parse(GetLocalIPAddress()), (int)msg.Data);
                    beforeLogin.Add(client);
                    ClsMessaggio mex = new ClsMessaggio(GetLocalIPAddress(), msg.Data.ToString());
                    ObjMex objMex = new ObjMex("conn-established", "");
                    mex.Messaggio = JsonConvert.SerializeObject(objMex);
                    client.Invia(mex);
                    break;
                case "login-ask":
                    break;
            }
        }

        public static string GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Nessuna interfaccia di rete disponibile su questo computer");
        }
    }
}
