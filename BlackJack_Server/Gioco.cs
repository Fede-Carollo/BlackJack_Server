using Newtonsoft.Json;
using SOCKET_UDP;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public int HavePlayed { get => _havePlayed; set => _havePlayed = value; }
        internal Dictionary<int, Player> Lobby { get => _lobby; set => _lobby = value; }
        internal Dictionary<int, Player> NowPlaying { get => _nowPlaying; set => _nowPlaying = value; }
        internal Dictionary<int, clsClientUDP> ClientsConnected { get => _clientsConnected; set => _clientsConnected = value; }
        public List<Place> Posti { get => _posti; set => _posti = value; }
        internal clsServerUDP Server { get => _server; set => _server = value; }
        public List<Card> Mazzo { get => _mazzo; set => _mazzo = value; }

        internal Gioco(clsServerUDP server)
        {
            this._havePlayed = 0;
            this._lobby = new Dictionary<int, Player>();
            this._nowPlaying = new Dictionary<int, Player>();
            this.ClientsConnected = new Dictionary<int, clsClientUDP>();
            this._server = server;
            this._posti = new List<Place>(4);
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
                default:
                    break;
            }
        }

        public void NuovoTurno()
        {
            //aggiunta player entrati con il turno in corso
            foreach (var player in _lobby)
                _nowPlaying.Add(player.Key,player.Value);
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
            }

        }

        public void FineTurno()
        {

        }

        public int DeterminaPosto()
        {
            for (int i = 1; i <= 4; i++)
            {
                if (!this._posti.Any(p => p.Posizione == i))
                {
                    return i+1;
                }
            }
            return 0;
        }

        public ClsMessaggio GeneraMessaggio(string action, List<object> data)
        {
            ClsMessaggio toSend = new ClsMessaggio();
            ObjMex objMex = new ObjMex(action, data);
            toSend.Messaggio = JsonConvert.SerializeObject(objMex);
            return toSend;
        }

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
