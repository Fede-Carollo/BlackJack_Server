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
        Player player;
        Gioco gioco;
        internal static List<Player> playersConnected;


        Player_Controller p_controller;

        public Form1()
        {
            InitializeComponent();
            server = new clsServerUDP(IPAddress.Parse(NetUtilities.GetLocalIPAddress()), 7777);
            p_controller = new Player_Controller();
            playersConnected = new List<Player>();
            this.Visible = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            server.avvia();
            server.datiRicevutiEvent += Server_datiRicevutiEvent;
            gioco = new Gioco(server);
        }

        private void Server_datiRicevutiEvent(ClsMessaggio message)
        {
            string[] ricevuti = message.toArray();
            ObjMex received = new ObjMex(null, null);
            ClsMessaggio toSend = new ClsMessaggio();
            ObjMex objToSend = new ObjMex();
            received = JsonConvert.DeserializeObject<ObjMex>(ricevuti[2]);
            switch(received.Action)
            {
                case "new-conn":
                    NewConn(received.Data[0]);
                    break;
                case "login-ask":
                    LoginAsk(received.Data);
                    break;
                case "player-ready":
                    if(!gioco.gameStarted)
                    {
                        gioco.NuovoTurno();
                    }
                    break;
            }
        }

        private void NewConn(object data)
        {
            int id = GeneraId();
            clsClientUDP client = new clsClientUDP(IPAddress.Parse(NetUtilities.GetLocalIPAddress()),
                                                    Convert.ToInt32(data));
            gioco.ClientsConnected.Add(id, client);
            List<object> lst = new List<object>();
            lst.Add(id);
            client.Invia(GeneraMessaggio("conn-established", lst));
        }

        private void LoginAsk(List<object> data)
        {
            List<object> lst = new List<object>();
            int id_player = Convert.ToInt32(data[0]);
            player = JsonConvert.DeserializeObject<Player>(data[1].ToString());
            if (player.Email != null)
                player = p_controller.ReadPlayer_ByEmailAndPass(player.Email, player.Password);
            else
                player = p_controller.ReadPlayer_ByUsernameAndPass(player.Username, player.Password);
            if (player == null || playersConnected.Any(p => p.Username == player.Username))
            {
                lst = new List<object>();
                lst.Add(player == null);
                gioco.ClientsConnected[id_player].Invia(GeneraMessaggio("login-failed", lst));
            }
            else
            {
                if (gioco.Lobby.Count < 4)
                {
                    lst = new List<object>();
                    lst.Add(player);
                    lst.Add(gioco.DeterminaPosto());
                    gioco.ClientsConnected[id_player].Invia(GeneraMessaggio("login-success", lst));
                    gioco.Lobby.Add(id_player, player);
                    gioco.Posti.Add(new Place(player, gioco.DeterminaPosto())); //TODO: probabilmente va assegnato dinamicamente a inizio turno per i nuovi player
                    playersConnected.Add(player);
                    if (gioco.NowPlaying.Count > 0 && gioco.playersBet == gioco.NowPlaying.Count)
                        gioco.UpdateGraphicsPlayer(player);
                    else if (gioco.NowPlaying.Count > 0)
                        gioco.UpdateGraphicsPlayer_dealer(player);
                    gioco.UpdatePlayerNames();
                }
                else
                {
                    gioco.ClientsConnected[id_player].Invia(GeneraMessaggio("lobby-full", null));
                }
            }
        }

        public ClsMessaggio GeneraMessaggio(string action, List<object> data)
        {
            ClsMessaggio toSend = new ClsMessaggio();
            ObjMex objMex = new ObjMex(action, data);
            toSend.Messaggio = JsonConvert.SerializeObject(objMex);
            return toSend;
        }

        private int GeneraId()
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            int id;
            bool alr_existing = false;
            do
            {
                alr_existing = false;
                id = rnd.Next(10000, 100000);
                foreach (var key in gioco.ClientsConnected.Keys)
                {
                    if(key == id)
                    {
                        alr_existing = true;
                        break;
                    }
                }
            }
            while (alr_existing);
            return id;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (clsClientUDP client in gioco.ClientsConnected.Values)
            {
                client.Invia(GeneraMessaggio("server-shutdown",null));
            }
        }
    }
}
