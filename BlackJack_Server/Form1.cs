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
        Dictionary<int,clsClientUDP> clients;
        Dictionary<int, Player> lobby;
        Dictionary<int, Player> nowPlaying;

        Player_Controller p_controller;

        public Form1()
        {
            InitializeComponent();
            server = new clsServerUDP(IPAddress.Parse(NetUtilities.GetLocalIPAddress()), 7777);
            lobby = new Dictionary<int, Player>();
            nowPlaying = new Dictionary<int, Player>();
            clients = new Dictionary<int, clsClientUDP>();
            p_controller = new Player_Controller();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            server.avvia();
            server.datiRicevutiEvent += Server_datiRicevutiEvent;
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
                    int id = GeneraId();
                    clsClientUDP client = new clsClientUDP(IPAddress.Parse(NetUtilities.GetLocalIPAddress()), Convert.ToInt32(received.Data[0]));
                    clients.Add(id,client);
                    toSend = new ClsMessaggio(NetUtilities.GetLocalIPAddress(), received.Data[0].ToString());
                    List<object> lst = new List<object>();
                    lst.Add(id);
                    objToSend = new ObjMex("conn-established", lst);
                    toSend.Messaggio = JsonConvert.SerializeObject(objToSend);
                    client.Invia(toSend);
                    break;
                case "login-ask":
                    int id_player = Convert.ToInt32(received.Data[0]);
                    player = JsonConvert.DeserializeObject<Player>(received.Data[1].ToString());
                    if (player.Email != null)
                        player = p_controller.ReadPlayer_ByEmailAndPass(player.Email, player.Password);
                    else
                        player = p_controller.ReadPlayer_ByUsernameAndPass(player.Username, player.Password);
                    if (player == null)
                    {
                        toSend = new ClsMessaggio(NetUtilities.GetLocalIPAddress(),"");
                        lst = new List<object>();
                        lst.Add("Credenziali errate");
                        objToSend = new ObjMex("login-failed", lst);
                        toSend.Messaggio = JsonConvert.SerializeObject(objToSend);
                        clients[id_player].Invia(toSend);
                    }
                    else
                    {
                        toSend = new ClsMessaggio(NetUtilities.GetLocalIPAddress(), "");
                        lst = new List<object>();
                        lst.Add(JsonConvert.SerializeObject(player));
                        objToSend = new ObjMex("login-success",lst);
                        toSend.Messaggio = JsonConvert.SerializeObject(objToSend);
                        clients[id_player].Invia(toSend);
                        lobby.Add(id_player, player);
                    }
                    break;
                case "join-lobby":
                    id_player = Convert.ToInt32(received.Data[0]);
                    nowPlaying.Add(id_player, lobby[id_player]);

                    break;
            }
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
                foreach (var key in clients.Keys)
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
            ClsMessaggio mex;
            ObjMex objMex;
            foreach (clsClientUDP client in clients.Values)
            {
                mex = new ClsMessaggio(NetUtilities.GetLocalIPAddress(), "");
                List<object> lst = new List<object>();
                lst.Add("Connection lost");
                objMex = new ObjMex("server-shutdown",lst);
                mex.Messaggio = JsonConvert.SerializeObject(objMex);
                client.Invia(mex);
            }
        }
    }
}
