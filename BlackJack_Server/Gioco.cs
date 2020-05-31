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

        #region variabili e prop

        private List<Card> _mazzo;
        private clsServerUDP _server;
        private int _havePlayed;
        private Dictionary<int, clsClientUDP> _clientsConnected;
        private Dictionary<int, Player> _lobby;
        private Dictionary<int, Player> _nowPlaying;
        private (Dictionary<int, clsClientUDP>,Dictionary<int, clsClientUDP>) _clientsPingResponse;
        private List<Place> _posti;
        private Place _banco;
        public bool gameStarted;
        private int id_playing;
        private int numPinged;
        public int playersBet;
        private bool betPhase;

        public int HavePlayed { get => _havePlayed; set => _havePlayed = value; }
        internal Dictionary<int, Player> Lobby { get => _lobby; set => _lobby = value; }
        internal Dictionary<int, Player> NowPlaying { get => _nowPlaying; set => _nowPlaying = value; }
        internal Dictionary<int, clsClientUDP> ClientsConnected { get => _clientsConnected; set => _clientsConnected = value; }
        public List<Place> Posti { get => _posti; set => _posti = value; }
        internal clsServerUDP Server { get => _server; set => _server = value; }
        public List<Card> Mazzo { get => _mazzo; set => _mazzo = value; }
        public Place Banco { get => _banco; set => _banco = value; }

        #endregion

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
            playersBet = 0;
            betPhase = false;
            
            numPinged = 0;
            PingConn();
        }

        private void Server_datiRicevutiEvent(ClsMessaggio message)
        {
            string[] ricevuti = message.toArray();
            ObjMex received = new ObjMex(null, null);
            received = JsonConvert.DeserializeObject<ObjMex>(ricevuti[2]);
            switch(received.Action)
            {
                case "player-bet":
                    PlayerBet(received.Data);
                    break;

                case "player-hit":
                    PlayerHit(received.Data);
                    break;

                case "player-stand":
                    PlayerStand(received.Data[0]);
                    break;
                case "double-bet":
                    DoubleBet(received.Data);
                    break;
            }
        }

        #region gestione carte giocatori prima mano

        private void GeneraCartePlayers()
        {
            
            foreach (Place posto in _posti)
            {
                posto.Carte = new List<Card>();

                //#region forza bj
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
                //#endregion

                for (int i = 0; i < 2; i++)
                {
                    posto.Carte.Add(_mazzo[0]);
                    _mazzo.RemoveAt(0);
                }
                #endif
                
            }
        }

        private void GiveCards()
        {
            foreach (Place posto in _posti)
            {
                if (posto.Carte.Count > 0)
                {
                    List<object> lst = new List<object>();
                    lst.Add(posto);
                    ClsMessaggio mex = GeneraMessaggio("new-cards", lst);
                    foreach (clsClientUDP client in _clientsConnected.Values)
                        client.Invia(mex);
                }
            }
        }

        #endregion

        #region updates

        internal void UpdateGraphicsPlayer_dealer(Player player)
        {
            List<object> lst = new List<object>();
            lst.Add(_banco);
            lst.Add(_banco.Carte.Count == 2);
            foreach (var keyValue in _lobby)
            {
                if (keyValue.Value.Username == player.Username)
                    _clientsConnected[keyValue.Key].Invia(GeneraMessaggio("new-cards-dealer", lst));
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

        #endregion

        #region gestione turno

        public void NuovoTurno()
        {
            gameStarted = true;
            playersBet = 0;
            betPhase = true;
            //aggiunta player entrati con il turno in corso
            foreach (var player in _lobby)
            {
                _nowPlaying.Add(player.Key, player.Value);
            }
            foreach (int key in _nowPlaying.Keys)
            {
                _clientsConnected[key].Invia(GeneraMessaggio("new-turn"));
            }
            //generazione carte banco
            CaricaMazzo();
            ShuffleMazzo();
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
            _lobby = new Dictionary<int, Player>();
            _havePlayed = 0;
            GeneraCartePlayers();
        }

        private void StartPlayerTurn(int pos)
        {
            Place p;
            bool hasBj;
            bool rightPlayer = false;
            (int, bool) mano = (0, false);
            do
            {
                hasBj = false;
                p = _posti.Find(pl => pl.Posizione == pos);
                //TODO: controllare che il posto trovato esista in nowplaying
                if(p!= null)
                {
                    foreach (var keyValue in _nowPlaying)
                    {
                        if (keyValue.Value.Username == p.Player.Username)
                        {
                            rightPlayer = true;
                            break;
                        }
                    }
                }
                
                if (p == null || !rightPlayer)
                    pos++;
                else
                {
                    mano = p.GetMano();
                    if (mano.Item1 == 21)
                    {
                        foreach (int chiave in _nowPlaying.Keys)
                        {
                            if (_nowPlaying[chiave].Username == p.Player.Username)
                                _clientsConnected[chiave].Invia(GeneraMessaggio("blackjack"));
                        }
                        hasBj = true;
                        pos++;
                    }
                }

            }
            while ((p == null && pos <= 4) || (hasBj && pos <= 4) || !rightPlayer);
            if (pos > 4)
                FineTurno();
            else
            {
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

        public async void FineTurno()
        {
            Console.WriteLine("eseguo fine turno");
            List<object> lst;
            id_playing = 0;
            //Controlli banco che deve fare almeno 17
            if (_banco.GetMano().Item1 >= 17)
            {
                foreach (var client in _clientsConnected.Values)
                {
                    client.Invia(GeneraMessaggio("unveil-card"));
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
                    await Task.Delay(1500);
                }
            }
            #region vittoria sconfitta player
            foreach (Place p in _posti)
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
                if (toSend != null)
                {
                    lst = new List<object>();
                    Console.WriteLine($"{p.Player.Username}: {p.Fiches}");
                    if (mano_player.Item2 && mano_banco.Item2)  //entrambi blackjack
                    {
                        p.Fiches += p.Puntata;
                        lst.Add(p.Fiches);
                        toSend.Invia(GeneraMessaggio("draw", lst));
                    }
                    else if (mano_player.Item2)  //blackjack player
                    {
                        p.Fiches += p.Puntata + p.Puntata * (5 / 2);
                        lst.Add(p.Fiches);
                        toSend.Invia(GeneraMessaggio("player-wins", lst));
                    }
                    else if (mano_banco.Item2)   //blackjack server
                    {
                        lst.Add(p.Fiches);
                        toSend.Invia(GeneraMessaggio("dealer-wins", lst));
                    }
                    else if (mano_player.Item1 > 21)  //giocatore sballa
                    {
                        lst.Add(p.Fiches);
                        toSend.Invia(GeneraMessaggio("dealer-wins", lst));
                    }
                    else if (mano_banco.Item1 > 21)
                    {
                        p.Fiches += p.Puntata * 2;
                        lst.Add(p.Fiches);
                        toSend.Invia(GeneraMessaggio("player-wins", lst));
                    }
                    else if (mano_banco.Item1 == mano_player.Item1)  //stessa mano
                    {
                        p.Fiches += p.Puntata;
                        lst.Add(p.Fiches);
                        toSend.Invia(GeneraMessaggio("draw", lst));
                    }
                    else if (mano_player.Item1 > mano_banco.Item1) //player > server
                    {
                        p.Fiches += p.Puntata * 2;
                        lst.Add(p.Fiches);
                        toSend.Invia(GeneraMessaggio("player-wins", lst));
                    }
                    else    //server > player
                    {
                        lst.Add(p.Fiches);
                        toSend.Invia(GeneraMessaggio("dealer-wins", lst));
                    }
                    p.Puntata = 0;
                    Console.WriteLine($"{p.Player.Username}: {p.Fiches}");

                    if (p.Fiches == 0)
                    {
                        int id_pl = 0;
                        foreach (int id_player in _nowPlaying.Keys)
                        {
                            if (_nowPlaying[id_player].Username == p.Player.Username)
                            {
                                id_pl = id_player;
                                break;
                            }
                        }
                        lst = new List<object>();
                        _clientsConnected[id_pl].Invia(GeneraMessaggio("no-fiches"));
                        EliminaPlayer(id_pl, p.Player, "playing");
                    }
                }
                #endregion
            }
            await Task.Delay(5000);
            _banco.Carte.Clear();
            foreach (Place p in _posti)
                p.Carte.Clear();
            if (_nowPlaying.Count != 0 || _lobby.Count != 0)
                NuovoTurno();
            else
                gameStarted = false;

        }

        #endregion

        #region metodi switch

        private void PlayerBet(List<object> data)
        {
            int posizione_tavolo = Convert.ToInt32(data[0]);
            int puntata = Convert.ToInt32(data[1]);
            Place current = _posti.Find(p => p.Posizione == posizione_tavolo);
            current.Fiches -= puntata;
            current.Puntata += puntata;
            playersBet++;
            List<object> lst = new List<object>();
            if (playersBet >= _nowPlaying.Count)
            {
                GiveCards();
                betPhase = false;
                StartPlayerTurn(_havePlayed + 1);
            }
        }

        private void PlayerHit(List<object> data)
        {
            int posizione_tavolo = Convert.ToInt32(data[0]);
            int id_player = Convert.ToInt32(data[1]);
            _posti.Find(pl => pl.Posizione == posizione_tavolo).Carte.Add(_mazzo[0]);
            _mazzo.RemoveAt(0);
            List<object> lst = new List<object>();
            foreach (Place posto in _posti)
            {
                if (posto.Posizione == posizione_tavolo)
                {
                    lst.Add(posto);
                    ClsMessaggio mex = GeneraMessaggio("new-cards", lst);
                    foreach (clsClientUDP client in _clientsConnected.Values)
                        client.Invia(mex);
                    break;
                }
            }
            (int, bool) hand = _posti.Find(pl => pl.Posizione == posizione_tavolo).GetMano();

            if (hand.Item1 == 21)
            {
                lst = new List<object>();
                lst.Add(hand.Item2);
                _clientsConnected[id_player].Invia(GeneraMessaggio("hand-twentyone", lst));
                _havePlayed++;

                if (_havePlayed >= _posti.Count)
                    FineTurno();
                else
                {
                    StartPlayerTurn(_havePlayed + 1);
                }
            }
            else if (hand.Item1 > 21)
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
        }

        private void PlayerStand(object data)
        {
            int id_player = Convert.ToInt32(data);
            _havePlayed++;

            if (_havePlayed >= _nowPlaying.Count)
                FineTurno();
            else
                StartPlayerTurn(_havePlayed + 1);
        }

        private void DoubleBet(List<object> lst)
        {
            int pos_tavolo = Convert.ToInt32(lst[0]);
            Place current = _posti.Find(p => p.Posizione == pos_tavolo);
            current.Fiches -= current.Puntata;
            current.Puntata *= 2;
            
            PlayerHit(lst);
        }

        #endregion

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

        #region metodi mazzo

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

        #endregion

        #region ping connessi
        System.Windows.Forms.Timer pingResponse;

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
            pingResponse.Interval = 2000;
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
                        if(id_playing == clientSentKey)
                        {
                            SwitchPlayer();
                        }
                        else
                        {
                            if (_nowPlaying.Keys.Any(c => c == clientSentKey))
                                EliminaPlayer(clientSentKey,_nowPlaying[clientSentKey], "playing");
                            else if(_lobby.Keys.Any(c => c == clientSentKey))
                                EliminaPlayer(clientSentKey,_lobby[clientSentKey], "lobby");
                        }
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
                    bool trovato = false;
                    foreach (var chiave in _clientsPingResponse.Item2.Keys)
                    {
                        if(chiave == id_player)
                        {
                            trovato = true;
                            break;
                        }    
                    }
                    if(!trovato)
                    {
                        foreach (int keys in _clientsPingResponse.Item1.Keys)
                        {
                            if(keys == id_player)
                            {
                                trovato = true;
                                break;
                            }
                        }
                        if(id_player != 0 && trovato)  //dà problemi alcune volte
                            _clientsPingResponse.Item2.Add(id_player, _clientsPingResponse.Item1[id_player]);
                        numPinged--;
                    }
                        
                    break;
            }
        }
        #endregion


        #region gestione player leave

        private async void SwitchPlayer()
        {
            await Task.Delay(1);
            if(_posti.Any(p => p.Player.Username == _nowPlaying[id_playing].Username))
            {
                 int pos = _posti.Find(p => p.Player.Username == _nowPlaying[id_playing].Username).Posizione;
                _posti.Remove(_posti.Find(p => p.Player == _nowPlaying[id_playing]));
                try
                {
                    Form1.playersConnected.Remove(Form1.playersConnected.Find(player => player.Username == _nowPlaying[id_playing].Username));
                }
                catch(Exception)
                { }
                _nowPlaying.Remove(id_playing);
                _havePlayed++;
                List<object> lst = new List<object>();
                lst.Add(pos);
                foreach (clsClientUDP client in _clientsConnected.Values)
                {
                    client.Invia(GeneraMessaggio("player-leave", lst));
                }
            }
            if (_havePlayed > _nowPlaying.Count)
                FineTurno();
            else
                StartPlayerTurn(_havePlayed + 1);
        }

        private async void EliminaPlayer(int id, Player toDelete, string status)
        {
            await Task.Delay(1);
            if (playersBet > 0)
                playersBet--;
            int pos = _posti.Find(p => p.Player.Username == toDelete.Username).Posizione;
            _posti.Remove(_posti.Find(p => p.Player.Username == toDelete.Username));
            Form1.playersConnected.Remove(Form1.playersConnected.Find(player => player.Username == toDelete.Username));
            if (status == "playing")
            {
                _nowPlaying.Remove(id);
            }
            else
            {
                _lobby.Remove(id);
            }
            List<object> lst = new List<object>();
            lst.Add(pos);
            foreach (clsClientUDP client in _clientsConnected.Values)
            {
                client.Invia(GeneraMessaggio("player-leave", lst));
            }
            if(betPhase)
            {
                if (_havePlayed >= _nowPlaying.Count)
                    FineTurno();
            }
        }

        #endregion
    }

    
}
