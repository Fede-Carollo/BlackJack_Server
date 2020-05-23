using Newtonsoft.Json;
using SOCKET_UDP;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
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
        private Dictionary<int, clsClientUDP> _clientsConnected;
        private Dictionary<int, Player> _lobby;
        private Dictionary<int, Player> _nowPlaying;
        private List<Place> _posti;
        private Place _banco;
        public bool gameStarted;
        private int id_playing;

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
            gameStarted = false;
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
                            p = _posti.Find(pl => pl.Posizione == _havePlayed);
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
                    break;

                case "player-stand":
                    //id_player = Convert.ToInt32(received.Data[0]);
                    _havePlayed++;

                    if (_havePlayed == _nowPlaying.Count)
                        FineTurno();
                    else
                    {
                        p = _posti.Find(pl => pl.Posizione == _havePlayed + 1);
                        foreach (var keyValue in _nowPlaying)
                        {
                            if (keyValue.Value.Username == p.Player.Username)
                                _clientsConnected[keyValue.Key].Invia(GeneraMessaggio("your-turn", null));
                        }
                    }
                    break;
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
            _lobby = new Dictionary<int, Player>();
            _havePlayed = 0;
            CaricaMazzo();
            ShuffleMazzo();
            //generazione carte per ogni giocatore
            foreach (Place posto in _posti)
            {
                List<object> lst = new List<object>();
                for (int i = 0; i < 2; i++)
                {
                    posto.Carte.Add(_mazzo[0]);
                    _mazzo.RemoveAt(0);
                }
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
                        client.Invia(GeneraMessaggio("hand-twentyone-first", null));
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
            foreach (clsClientUDP client in _clientsConnected.Values)
                client.Invia(mess);

            StartPlayerTurn(_havePlayed+1);
        }

        private void StartPlayerTurn(int pos)
        {
            Place p = _posti.Find(pl => pl.Posizione == _havePlayed + 1);
            foreach (var keyValue in _nowPlaying)
            {
                if (keyValue.Value.Username == p.Player.Username)
                {
                    _clientsConnected[keyValue.Key].Invia(GeneraMessaggio("your-turn", null));
                    id_playing = keyValue.Key;
                }
            }
        }

        public void FineTurno() //TODO: controllo pareggi, gestione blackjack
        {
            List<object> lst;

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
                    foreach (var client in _clientsConnected.Values)
                        client.Invia(GeneraMessaggio("new-cards-dealer", lst));
                    _mazzo.RemoveAt(0);
                    Thread.Sleep(1000);
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
                if(mano_player.Item1 > 21)  //giocatore sballa
                {
                    toSend.Invia(GeneraMessaggio("dealer-wins", null));
                }
                else if(mano_banco.Item1 > 21)
                {
                    toSend.Invia(GeneraMessaggio("player-wins", null));
                }
                else if (mano_player.Item2 && mano_banco.Item2)  //entrambi blackjack
                {
                    toSend.Invia(GeneraMessaggio("draw", null));
                }
                else if(mano_player.Item2)  //blackjack player
                {
                    toSend.Invia(GeneraMessaggio("player-wins", null));
                }
                else if(mano_banco.Item2)   //blackjack server
                {
                    toSend.Invia(GeneraMessaggio("dealer-wins", null));
                }
                else if(mano_banco.Item1 == mano_player.Item1)  //stessa mano
                {
                    toSend.Invia(GeneraMessaggio("draw", null));
                }
                else if(mano_player.Item1>mano_banco.Item1) //player > server
                {
                    toSend.Invia(GeneraMessaggio("player-wins", null));
                }
                else    //server > player
                {
                    toSend.Invia(GeneraMessaggio("dealer-wins", null));
                }
            }

            Thread.Sleep(5000);

            _banco.Carte.Clear();
            foreach (Place p in _posti)
                p.Carte.Clear();
            if(_nowPlaying.Count != 0 || _lobby.Count != 0)
                NuovoTurno();
        }

        //Terminato
        public int DeterminaPosto()
        {
            for (int i = 1; i <= 4; i++)
                if (!this._posti.Any(p => p.Posizione == i))
                    return i;
            return 0;
        }

        //Terminato - funzionante
        public ClsMessaggio GeneraMessaggio(string action, List<object> data)
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

            for (int i = 0; i < 3; i++)
            {
                foreach (char seme in semi)
                {
                    for (int j = 1; j <= 13; j++)
                    {
                        Mazzo.Add(new Card(seme, j, j < 10 ? j : 10));
                    }
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
    }
}
