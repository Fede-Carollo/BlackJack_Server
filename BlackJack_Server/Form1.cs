using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using SOCKET_UDP;
using Newtonsoft.Json;

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
                case "register":
                    VerificaEsistenza(received.Data);
                    break;
            }
        }

        /// <summary>
        /// Per registrare un nuovo profilo
        /// </summary>
        /// <param name="data"></param>
        private void VerificaEsistenza(List<object> data)
        {
            int log_id = Convert.ToInt32(data[0]);
            string username = data[2].ToString();
            string email = data[1].ToString();
            string password = data[3].ToString();
            List<object> lst = new List<object>();
            if (p_controller.EmailExisting(email))
            {
                lst.Add(false);
                lst.Add("email");
            }
            else if (p_controller.UsernameExisting(username))
            {
                lst.Add(false);
                lst.Add("username");
            }
            else
            {
                lst.Add(true);
                p_controller.CreatePlayer(username, email, password);
            }
            //primo oggetto: credenziali valide; secondo oggetto: quale delle due è sbagliata
            gioco.ClientsConnected[log_id].Invia(GeneraMessaggio("response-register", lst));
        }
        /// <summary>
        /// Registrazione di un nuovo client collegato
        /// </summary>
        /// <param name="data"></param>
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

        /// <summary>
        /// Richiesta di accesso in lobby utente
        /// </summary>
        /// <param name="data">Datti passati dal client</param>
        private void LoginAsk(List<object> data)
        {
            List<object> lst = new List<object>();
            int id_player = Convert.ToInt32(data[0]);
            player = JsonConvert.DeserializeObject<Player>(data[1].ToString());
            if (player.Email != null)   //se login tramite username
                player = p_controller.ReadPlayer_ByEmailAndPass(player.Email, player.Password);
            else    //se login tramite password
                player = p_controller.ReadPlayer_ByUsernameAndPass(player.Username, player.Password);
            if (player == null || playersConnected.Any(p => p.Username == player.Username))
            {
                lst = new List<object>();
                lst.Add(player == null);
                gioco.ClientsConnected[id_player].Invia(GeneraMessaggio("login-failed", lst));
            }
            else
            {
                if ((gioco.Lobby.Count+gioco.NowPlaying.Count) < 4)  //posto disponibile
                {
                    int posto = gioco.DeterminaPosto();
                    lst = new List<object>();
                    lst.Add(player);
                    lst.Add(posto);
                    gioco.ClientsConnected[id_player].Invia(GeneraMessaggio("login-success", lst));
                    gioco.Lobby.Add(id_player, player);
                    gioco.Posti.Add(new Place(player, posto));
                    playersConnected.Add(player);
                    if (gioco.NowPlaying.Count > 0 && !gioco.betPhase)   //giocatori in fase di gioco
                    {
                        gioco.UpdateGraphicsPlayer(player);
                    }
                    else if (gioco.NowPlaying.Count > 0)    //giocatori in fase di bet
                    {
                        gioco.UpdateGraphicsPlayer_dealer(player);
                    }
                    gioco.UpdatePlayerNames();
                }
                else    //lobby piena
                {
                    gioco.ClientsConnected[id_player].Invia(GeneraMessaggio("lobby-full"));
                }
            }
        }

        /// <summary>
        /// Generazione del messaggio standard per invio messaggi
        /// </summary>
        /// <param name="action">azione da eseguire</param>
        /// <param name="data">dati aggiuntivi (opzionali)</param>
        /// <returns></returns>
        public ClsMessaggio GeneraMessaggio(string action, List<object> data = null)
        {
            ClsMessaggio toSend = new ClsMessaggio();
            ObjMex objMex = new ObjMex(action, data);
            toSend.Messaggio = JsonConvert.SerializeObject(objMex);
            return toSend;
        }

        /// <summary>
        /// Generazione id_unico per la partita
        /// </summary>
        /// <returns>id univoco per la sessione</returns>
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

        //Fa sapere ai giocatori collegati che il server si stacca
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (clsClientUDP client in gioco.ClientsConnected.Values)
            {
                client.Invia(GeneraMessaggio("server-shutdown"));
            }
        }
    }
}
