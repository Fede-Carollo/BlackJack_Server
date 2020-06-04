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
        public bool betPhase;

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

        /// <summary>
        /// Delegato scatenato in arrivo di nuovi dati
        /// </summary>
        /// <param name="message"></param>
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
                case "player-leaving":
                    int id_player = Convert.ToInt32(received.Data[0]);
                    if (id_player == id_playing)
                        SwitchPlayer();
                    else
                    {
                        if (_nowPlaying.ContainsKey(id_player))
                            EliminaPlayer(id_player, _nowPlaying[id_player], "playing");
                        else
                            EliminaPlayer(id_player, _nowPlaying[id_player], "lobby");
                    }
                    break;
            }
        }

        #region gestione carte giocatori prima mano

        /// <summary>
        /// Vanno solamente generate a inizio turno
        /// </summary>
        private void GeneraCartePlayers()
        {
            foreach (Place posto in _posti)
            {
                posto.Carte = new List<Card>();

                //per test in caso di BJ
                #region forza bj
                /*#if DEBUG
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
                #else*/
                #endregion

                for (int i = 0; i < 2; i++)
                {
                    posto.Carte.Add(_mazzo[0]);
                    _mazzo.RemoveAt(0);
                }
                //#endif
                
            }
        }

        /// <summary>
        /// Distribuzione carte generate in precedenza
        /// </summary>
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

        /// <summary>
        /// Aggiorna le carte solo del dealer per il giocatore entrato in lobby in fase di bet
        /// </summary>
        /// <param name="player"></param>
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

        /// <summary>
        /// Aggiorna i nomi dei giocatori già presenti
        /// </summary>
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
        /// <summary>
        /// Aggiornamento carte di tutti i giocatori e del dealer per giocatori entrati in lobby in fase di game
        /// </summary>
        /// <param name="player">Per identificare l'id del giocatore a cui inviare gli aggiornamenti</param>
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
                {
                    _clientsConnected[keyValue.Key].Invia(GeneraMessaggio("update-graphics", lst));
                    break;
                }
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
            //comunicazione inizio nuovo turno a tutti i giocatori giocanti
            foreach (int key in _nowPlaying.Keys)
            {
                _clientsConnected[key].Invia(GeneraMessaggio("new-turn"));
            }
            _lobby = new Dictionary<int, Player>();
            //Aggiornamento mazzo
            CaricaMazzo();
            ShuffleMazzo();
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
            _havePlayed = 0;
            GeneraCartePlayers();
        }

        /// <summary>
        /// Determina il prossimo giocatore che deve giocare
        /// </summary>
        /// <param name="pos"></param>
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
                //controllo che sia un giocatore in gioco e non in lobby
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
                
                if (p == null || !rightPlayer)  //se non esiste o è in lobby
                    pos++;
                else    //se esiste e gioca
                {
                    mano = p.GetMano();
                    if (mano.Item1 == 21)
                    {
                        foreach (int chiave in _nowPlaying.Keys)
                        {
                            if (_nowPlaying[chiave].Username == p.Player.Username)
                                _clientsConnected[chiave].Invia(GeneraMessaggio("blackjack"));  //blocca i pulsanti lato client
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
                        _clientsConnected[keyValue.Key].Invia(GeneraMessaggio("your-turn"));
                        id_playing = keyValue.Key;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Metodo asincrono per la gestione di fune turno
        /// </summary>
        public async void FineTurno()
        {
            Console.WriteLine("eseguo fine turno");
            List<object> lst;
            id_playing = 0;
            //Controlli banco che deve fare almeno 17
            if (_banco.GetMano().Item1 >= 17) 
            {
                foreach (clsClientUDP client in _clientsConnected.Values)
                {
                    client.Invia(GeneraMessaggio("unveil-card"));   //svela la carta coperta se ha già 17
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
                        break;
                    }
                }
                if (toSend != null) //controlli necessari per l'eventualità di abbandono del giocatore
                {
                    //Gestione vittoria/sconfitta
                    lst = new List<object>();
                    if (mano_player.Item2 && mano_banco.Item2)  //entrambi blackjack
                    {
                        p.Fiches += p.Puntata;
                        lst.Add(p.Fiches);
                        toSend.Invia(GeneraMessaggio("draw", lst));
                    }
                    else if (mano_player.Item2)  //blackjack player
                    {
                        p.Fiches += p.Puntata * (5 / 2);
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

                    //eliminazione giocatore se termina le fiches
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
            await Task.Delay(5000); //tempo di delay per dare il tempo ai player di leggere i risultati
            _banco.Carte.Clear();
            foreach (Place p in _posti)
                p.Carte.Clear();
            if (_nowPlaying.Count != 0 || _lobby.Count != 0)
                NuovoTurno();
            else
                gameStarted = false;    //in modo da far ripartire il gioco al log di un nuovo player

        }

        #endregion

        #region metodi switch

        /// <summary>
        /// Bet da parte di un giocatore
        /// </summary>
        /// <param name="data">informazioni sul giocatore</param>
        private void PlayerBet(List<object> data)
        {
            int posizione_tavolo = Convert.ToInt32(data[0]);
            int puntata = Convert.ToInt32(data[1]);
            Place current = _posti.Find(p => p.Posizione == posizione_tavolo);
            current.Fiches -= puntata;
            current.Puntata += puntata;
            playersBet++;
            List<object> lst = new List<object>();
            if (playersBet >= _nowPlaying.Count)    //quando hanno puntato tutti, >= e non uguale per l'eventualità di abbandono player
            {
                GiveCards();
                betPhase = false;
                StartPlayerTurn(_havePlayed + 1);
            }
        }

        /// <summary>
        /// Richiesta nuova carta giocatore
        /// </summary>
        /// <param name="data">informazioni giocatore</param>
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

                //passaggio prossimo turno
                FineTurnoPlayer();
            }
            else if (hand.Item1 > 21)
            {
                _clientsConnected[id_player].Invia(GeneraMessaggio("hand-bust", null));
                _havePlayed++;
                FineTurnoPlayer();
            }
        }

        private void PlayerStand(object data)
        {
            int id_player = Convert.ToInt32(data);
            _havePlayed++;
            FineTurnoPlayer();
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

        //determina se iniziare un nuovo turno o se passare il turno al prossimo giocatore
        private void FineTurnoPlayer()
        {
            if (_havePlayed == _nowPlaying.Count)
                FineTurno();
            else
            {
                StartPlayerTurn(_havePlayed + 1);
            }
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
            Console.WriteLine($"ping inviato a {_clientsConnected.Count} client");
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
            if(numPinged>0) //se qualcuno non risponde
            {
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
                            if (_nowPlaying.Keys.Any(c => c == clientSentKey))  //elimina il giocatore dalla partita o dalla lobby
                                EliminaPlayer(clientSentKey,_nowPlaying[clientSentKey], "playing");
                            else if(_lobby.Keys.Any(c => c == clientSentKey))
                                EliminaPlayer(clientSentKey,_lobby[clientSentKey], "lobby");
                        }
                    }
                }
            }
            pingResponse.Stop();
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
                        if(id_player != 0 && trovato)  //dà problemi quando non si sincornizza l'esecuzione
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
                    client.Invia(GeneraMessaggio("player-leave", lst)); //Comunica agli altri giocatori di aggiornare il posto
                }
            }
            FineTurnoPlayer();
        }

        private async void EliminaPlayer(int id, Player toDelete, string status)
        {
            await Task.Delay(1);
            if (playersBet > 0) //per evitare problemi
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
