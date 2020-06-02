using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackJack_Server
{
    /// <summary>
    /// Posto standard BJ
    /// </summary>
    public class Place
    {
        private Player _player;
        private List<Card> _carte;
        private int _posizione;
        private int _fiches;
        private int _puntata;

        public List<Card> Carte { get => _carte; set => _carte = value; }
        internal Player Player { get => _player; set => _player = value; }
        public int Posizione { get => _posizione; set => _posizione = value; }
        public int Fiches { get => _fiches; set => _fiches = value; }
        public int Puntata { get => _puntata; set => _puntata = value; }

        /// <summary>
        /// Istanza con nuovo giocatore
        /// </summary>
        /// <param name="player"></param>
        /// <param name="pos"></param>
        internal Place(Player player, int pos)
        {
            this._player = player;
            this._posizione = pos;
            this._carte = new List<Card>();
            this._fiches = 1000;
        }


        /// <summary>
        /// Istanza del banco
        /// </summary>
        internal Place() 
        {
            this._carte = new List<Card>();
        }

        //ritorna il valore e se è blackjack
        public (int,bool) GetMano()
        {
            int tot = 0;
            bool isBlackJack = false;
            if(this.Carte.Count == 2)
            {
                if(Carte[0].Numero == 1)
                {
                    if(Carte[1].Valore == 10)
                    {
                        return (21, true);
                    } 
                }
                else if (Carte[1].Numero == 1)
                {
                    if (Carte[1].Valore == 10)
                    {
                        return (21, true);
                    }
                }
            }
            List<Card> assi = new List<Card>(); //asso vale 11 se non sballa, quindi va valutato al fondo
            foreach (Card carta in _carte)
            {
                if (carta.Numero == 1)
                    assi.Add(carta);
                else
                    tot += carta.Valore;
            }

            for (int i = 0; i < assi.Count; i++)
            {
                if (tot + 11 <= 21 && assi.Count == 1 || tot + 11 <= 20 && assi.Count == 2 || tot + 11 <= 19 && assi.Count == 3 || tot + 11 <= 18 && assi.Count == 4)
                {
                    tot += 11;
                }
                else
                    tot += 1;
            }
            return (tot, isBlackJack);
        }
    }
}
