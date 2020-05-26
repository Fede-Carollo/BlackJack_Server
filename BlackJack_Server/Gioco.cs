using Newtonsoft.Json;
using SOCKET_UDP;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlackJack_Server
{
    public class Gioco
    {
        private List<Card> _mazzo;
        private clsServerUDP _server;
        private int _havePlayed;
        private volatile Dictionary<int, clsClientUDP> _clientsConnected;
        private volatile Dictionary<int, Player> _lobby;
        private volatile Dictionary<int, Player> _nowPlaying;
        private (Dictionary<int, clsClientUDP>,Dictionary<int, clsClientUDP>) _clientsPingResponse;
        private List<Place> _posti;
        private Place _banco;
        public bool gameStarted;
        private int id_playing;
        private int numPinged;

        public int HavePlayed { get => _havePlayed; set => _havePlayed = value; }
        internal Dictionary<int, Player> Lobby { get => _lobby; set => _lobby = value; }
        internal Dictionary<int, Player> NowPlaying { get => _nowPlaying; set => _nowPlaying = value; }
        internal Dictionary<int, clsClientUDP> ClientsConnected { get => _clientsConnected; set => _clientsConnected = value; }
        public List<Place> Posti { get => _posti; set => _posti = value; }
        internal clsServerUDP Server { get => _server; set => _server = value; }
        public List<Card> Mazzo { get => _mazzo; set => _mazzo = value; }
        public Place Banco { get => _banco; set => _banco = value; }

        internal Gioco(clsServerUDP server)
        {
            this._havePlayed = 0;
            this._lobby = new Dictionary<int, Player>();
            this._nowPlaying = new Dictionary<int, Player>();
            this.ClientsConnected = new Dictionary<int, clsClientUDP>();
            this._server = server;
            this._posti = new List<Place>(4);
            this._banco = new Place();
            server.datiRicevutiEvent += Server_datiRicevutiEvent;
            server.datiRicevutiEvent += Server_pingEvent;
            gameStarted = false;
            
            numPinged = 0;
            PingConn();
        }

        private void Server_datiRicevutiEvent(ClsMessaggio message)
        {
            string[] ricevuti = message.toArray();
            ObjMex received = new ObjMex(null, null);
            ClsMessaggio toSend = new ClsMessaggio();
            ObjMex objToSend = new ObjMex();
            received = JsonConvert.DeserializeObject<ObjMex>(ricevuti[2]);
            int id_player;
            Place p;
            switch(received.Action)
            {
                default:
                    break;

                case "player-hit": 
                    int posizione_tavolo = Convert.ToInt32(received.Data[0]);
                    id_player = Convert.ToInt32(received.Data[1]);
                    _posti.Find(pl => pl.Posizione == posizione_tavolo).Carte.Add(_mazzo[0]);
                    _mazzo.RemoveAt(0);
                    List<object> lst = new List<object>();
                    foreach (Place posto in _posti)
                    {
                        if(posto.Posizione == posizione_tavolo)
                        {
                            lst.Add(posto);
                            ClsMessaggio mex = GeneraMessaggio("new-cards", lst);
                            foreach (clsClientUDP client in _clientsConnected.Values)
                                client.Invia(mex);
                            break;
                        }
                    }
                    (int, bool) hand = _posti.Find(pl => pl.Posizione == posizione_tavolo).GetMano();

                    if(hand.Item1 == 21)
                    {
                        lst = new List<object>();
                        lst.Add(hand.Item2);
                        _clientsConnected[id_player].Invia(GeneraMessaggio("hand-twentyone", lst));
                        _havePlayed++;

                        if(_havePlayed == _posti.Count)
                            FineTurno();
                        else
                        {
                            p = _posti.Find(pl => pl.Posizione == _havePlayed + 1);
                            foreach (var keyValue in _nowPlaying)
                            {
                                if (keyValue.Value.Username == p.Player.Username)
                                {
                                    _clientsConnected[keyValue.Key].Invia(GeneraMessaggio("your-turn", null));
                                    id_playing = keyValue.Key;
                                }
                                    
                            }
                        }
                    }
                    else if(hand.Item1>21)
                    {
                        _clientsConnected[id_player].Invia(GeneraMessaggio("hand-bust", null));
                        _havePlayed++;

                        if (_havePlayed == _nowPlaying.Count)
                            FineTurno();
                        else
                        {
                            StartPlayerTurn(_havePlayed + 1);
                        }
                    }
                    break;

                case "player-stand":
                    //id_player = Convert.ToInt32(received.Data[0]);
                    _havePlayed++;

                    if (_havePlayed == _nowPlaying.Count)
                        FineTurno();
                    else
                        StartPlayerTurn(_havePlayed + 1);
                    break;
            }
        }

        internal void UpdatePlayerNames()
        {
            List<object> lst = new List<object>();
            foreach (Place posto in _posti)
            {
                lst.Add(posto.Player.Username);
                lst.Add(posto.Posizione);
            }
            foreach (clsClientUDP client in _clientsConnected.Values)
                client.Invia(GeneraMessaggio("update-names", lst));
        }

        internal void UpdateGraphicsPlayer(Player player)
        {
            List<object> lst = new List<object>();
            lst.Add(_banco);
            foreach (Place posto in _posti)
                lst.Add(posto);
            Place p = _posti.Find(pl => pl.Player.Username == player.Username);
            foreach (var keyValue in _lobby)
            {
                if (keyValue.Value.Username == player.Username)
                    _clientsConnected[keyValue.Key].Invia(GeneraMessaggio("update-graphics", lst));
            }
        }

        public void NuovoTurno()
        {
            gameStarted = true;
            //aggiunta player entrati con il turno in corso
            foreach (var player in _lobby)
            {
                _nowPlaying.Add(player.Key, player.Value);
            }
            foreach (int key in _nowPlaying.Keys)
            {
                _clientsConnected[key].Invia(GeneraMessaggio("new-turn", null));
            }
            _lobby = new Dictionary<int, Player>();
            _havePlayed = 0;
            CaricaMazzo();
            ShuffleMazzo();
            //generazione carte per ogni giocatore
            foreach (Place posto in _posti)
            {
                posto.Carte = new List<Card>();
                List<object> lst = new List<object>();
                /*
#if DEBUG
                if(_mazzo.Any(c => c.Seme == 'p' && c.Numero == '1'))
                {
                    posto.Carte.Add(_mazzo.Find(c => c.Seme == 'p' && c.Numero == 1));
                    posto.Carte.Add(_mazzo.Find(c => c.Seme == 'p' && c.Numero == 13));
                    _mazzo.Remove(_mazzo.Find(c => c.Seme == 'p' && c.Numero == 1));
                    _mazzo.Remove(_mazzo.Find(c => c.Seme == 'p' && c.Numero == 13));
                }
                else
                {
                    posto.Carte.Add(_mazzo.Find(c => c.Seme == 'f' && c.Numero == 1));
                    posto.Carte.Add(_mazzo.Find(c => c.Seme == 'f' && c.Numero == 13));
                    _mazzo.Remove(_mazzo.Find(c => c.Seme == 'f' && c.Numero == 1));
                    _mazzo.Remove(_mazzo.Find(c => c.Seme == 'f' && c.Numero == 13));
                }
#else
*/
                for (int i = 0; i < 2; i++)
                {
                    posto.Carte.Add(_mazzo[0]);
                    _mazzo.RemoveAt(0);
                }
//#endif
                lst.Add(posto);
                ClsMessaggio mex = GeneraMessaggio("new-cards", lst);
                foreach (clsClientUDP client in _clientsConnected.Values)
                    client.Invia(mex);
                (int, bool) mano = posto.GetMano();
                if(mano.Item1 == 21)
                {
                    lst = new List<object>();
                    lst.Add(mano.Item2);
                    foreach (var client in _clientsConnected.Values)
                    {
                         client.Invia(GeneraMessaggio("hand-twentyone-first", lst));
                        _havePlayed++;
                        if (_havePlayed != _nowPlaying.Count)
                            StartPlayerTurn(_havePlayed + 1);
                        else
                            FineTurno();
                    }
                }
            }
            //generazione carte banco
            List<object> list = new List<object>();
            for (int i = 0; i < 2; i++)
            {
                _banco.Carte.Add(_mazzo[0]);
                _mazzo.RemoveAt(0);
            }
            list.Add(_banco);
            list.Add(true);     //nascondi carta
            ClsMessaggio mess = GeneraMessaggio("new-cards-dealer", list);
            Console.WriteLine((list[0] as Place).Carte.Count);
            foreach (clsClientUDP client in _clientsConnected.Values)
                client.Invia(mess);

            StartPlayerTurn(_havePlayed+1);
        }

        private void StartPlayerTurn(int pos)
        {
            Place p = _posti.Find(pl => pl.Posizione == pos);
            foreach (var keyValue in _nowPlaying)
            {
                if (keyValue.Value.Username == p.Player.Username)
                {
                    _clientsConnected[keyValue.Key].Invia(GeneraMessaggio("your-turn", null));
                    id_playing = keyValue.Key;
                }
            }
        }   //TODO: da modificare: deve mandare il messaggio al primo disponibile

        public void FineTurno() //TODO: controllo pareggi, gestione blackjack
        {
            List<object> lst;

            //Controlli banco che deve fare almeno 17
            if(_banco.GetMano().Item1 >= 17)
            {
                foreach (var client in _clientsConnected.Values)
                {
                    client.Invia(GeneraMessaggio("unveil-card", null));
                }
            }
            else
            {
                while (_banco.GetMano().Item1 < 17)
                {
                    lst = new List<object>();
                    _banco.Carte.Add(_mazzo[0]);
                    lst.Add(_banco);
                    lst.Add(false);  //non nascondere la carta del dealer 
                    Console.WriteLine((lst[0] as Place).Carte.Count);
                    foreach (var client in _clientsConnected.Values)
                        client.Invia(GeneraMessaggio("new-cards-dealer", lst));
                    _mazzo.RemoveAt(0);
                    Thread.Sleep(1500);
                }
            }

            foreach(Place p in _posti)
            {
                clsClientUDP toSend = null;
                (int, bool) mano_player = p.GetMano();
                (int, bool) mano_banco = _banco.GetMano();
                //determino il client corrispondente al posto
                foreach (var keyValue in _nowPlaying)
                {
                    if (keyValue.Value.Username == p.Player.Username)
                    {
                        toSend = _clientsConnected[keyValue.Key];
                    }
                }
                if(toSend!=null)
                {
                    if (mano_player.Item1 > 21)  //giocatore sballa
                    {
                        toSend.Invia(GeneraMessaggio("dealer-wins", null));
                    }
                    else if (mano_banco.Item1 > 21)
                    {
                        toSend.Invia(GeneraMessaggio("player-wins", null));
                    }
                    else if (mano_player.Item2 && mano_banco.Item2)  //entrambi blackjack
                    {
                        toSend.Invia(GeneraMessaggio("draw", null));
                    }
                    else if (mano_player.Item2)  //blackjack player
                    {
                        toSend.Invia(GeneraMessaggio("player-wins", null));
                    }
                    else if (mano_banco.Item2)   //blackjack server
                    {
                        toSend.Invia(GeneraMessaggio("dealer-wins", null));
                    }
                    else if (mano_banco.Item1 == mano_player.Item1)  //stessa mano
                    {
                        toSend.Invia(GeneraMessaggio("draw", null));
                    }
                    else if (mano_player.Item1 > mano_banco.Item1) //player > server
                    {
                        toSend.Invia(GeneraMessaggio("player-wins", null));
                    }
                    else    //server > player
                    {
                        toSend.Invia(GeneraMessaggio("dealer-wins", null));
                    }
                }
            }
            Thread.Sleep(5000);

            _banco.Carte.Clear();
            foreach (Place p in _posti)
                p.Carte.Clear();
            if(_nowPlaying.Count != 0 || _lobby.Count != 0)
                NuovoTurno();
        }

        //Terminato - Funzionante
        public int DeterminaPosto()
        {
            for (int i = 1; i <= 4; i++)
                if (!this._posti.Any(p => p.Posizione == i))
                    return i;
            return 0;
        }

        //Terminato - funzionante
        public ClsMessaggio GeneraMessaggio(string action, List<object> data = null)
        {
            ClsMessaggio toSend = new ClsMessaggio();
            ObjMex objMex = new ObjMex(action, data);
            toSend.Messaggio = JsonConvert.SerializeObject(objMex);
            return toSend;
        }

        //Terminato - funzionante
        private void CaricaMazzo()
        {
            char[] semi = new char[] { 'c', 'q', 'p', 'f' };

            Mazzo = new List<Card>();

            foreach (char seme in semi)
            {
                for (int j = 1; j <= 13; j++)
                {
                    Mazzo.Add(new Card(seme, j, j < 10 ? j : 10));
                }
            }

        }

        //Terminato - funzionante
        private void ShuffleMazzo()
        {
            Random rng = new Random();

            int n = Mazzo.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Card carta = Mazzo[k];
                Mazzo[k] = Mazzo[n];
                Mazzo[n] = carta;
            }
        }


        #region ping connessi
        volatile bool canPing = false;
        System.Windows.Forms.Timer pingResponse;

        private void Ping_Method()
        {
            canPing = true;
            while (true)
            {
                if(canPing)
                {
                    PingConn();
                    
                    canPing = false;
                }

            }
        }

        private void PingConn()
        {
            _clientsPingResponse = (new Dictionary<int, clsClientUDP>(), new Dictionary<int, clsClientUDP>());
            foreach (var client in _clientsConnected)
            {
                client.Value.Invia(GeneraMessaggio("ping"));
                _clientsPingResponse.Item1.Add(client.Key, client.Value);
            }
            #if DEBUG
            Console.WriteLine($"ping inviato a {_clientsConnected.Count} client");
            #endif
            numPinged = _clientsConnected.Count;
            pingResponse = new System.Windows.Forms.Timer();
            pingResponse.Tick += PingResponse_Tick;
            pingResponse.Interval = 5000;
            pingResponse.Start();
        }

        private void PingResponse_Tick(object sender, EventArgs e)
        {
            #if DEBUG
            Console.WriteLine($"Non hanno risposto {numPinged}");
            #endif
            if(numPinged>0)
            {
                //TODO: qualcuno non ha risposto >:(
                foreach (int clientSentKey in _clientsPingResponse.Item1.Keys)
                {
                    if(!_clientsPingResponse.Item2.Keys.Any(key => key == clientSentKey))
                    {
                        _clientsConnected.Remove(clientSentKey);
                    }
                }
                #if DEBUG
                foreach (var item in _clientsConnected.Keys)
                    Console.WriteLine(item);
                #endif

            }
            pingResponse.Stop();
            //canPing = true;
            PingConn();
        }

        private void Server_pingEvent(ClsMessaggio message)
        {
            string[] ricevuti = message.toArray();
            ObjMex received = new ObjMex(null, null);
            received = JsonConvert.DeserializeObject<ObjMex>(ricevuti[2]);
            switch (received.Action)
            {
                case "ping-response":
                    int id_player = Convert.ToInt32(received.Data[0]);
                    _clientsPingResponse.Item2.Add(id_player, _clientsPingResponse.Item1[id_player]);
                    numPinged--;
                    break;
            }
        }
#endregion
    }
}
