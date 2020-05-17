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

namespace BlackJack_Server
{
    public partial class Form1 : Form
    {
        clsServerUDP server;
        public Form1()
        {
            InitializeComponent();
            server = new clsServerUDP(IPAddress.Parse("127.0.0.1"), 7777);
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
        }
    }
}
