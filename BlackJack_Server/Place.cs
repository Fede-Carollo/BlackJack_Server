using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackJack_Server
{
    public class Place
    {
        private Player _player;
        private List<Card> _carte;

        public List<Card> Carte { get => _carte; set => _carte = value; }
        internal Player Player { get => _player; set => _player = value; }

        //ritorna il valore e se è blackjack
        public (int,bool) GetMano()
        {
            int tot = 0;
            bool isBlackJack = false;
            if(this.Carte.Count == 2)
            {
                if(Carte[0].Numero == 1 && (Carte[0].Seme == 'f' || Carte[0].Seme == 'p'))
                {
                    if(Carte[1].Valore == 10 && (Carte[1].Seme == 'f' || Carte[1].Seme == 'p'))
                    {
                        return (21, true);
                    } 
                }
                else if (Carte[1].Numero == 1 && (Carte[1].Seme == 'f' || Carte[1].Seme == 'p'))
                {
                    if (Carte[1].Valore == 10 && (Carte[1].Seme == 'f' || Carte[1].Seme == 'p'))
                    {
                        return (21, true);
                    }
                }
            }
            List<Card> assi = new List<Card>(); //asso vale 11 se non sballa
            foreach (Card carta in _carte)
            {
                if (carta.Numero == 1)
                    assi.Add(carta);
                else
                    tot += carta.Valore;
            }

            for (int i = 0; i < assi.Count; i++)
            {
                if (tot + 11 <= 21 && assi.Count == 1 || tot + 11 <= 20 && assi.Count == 2 || tot + 11 <= 19 && assi.Count == 3)
                {
                    tot += 11;
                }
                else
                    tot += 1;
            }
            return (tot, isBlackJack);
        }
    }

    public class Card
    {
        private char _seme;
        private int _numero;
        private int _valore;

        public char Seme { get => _seme; set => _seme = value; }
        public int Valore { get => _valore; set => _valore = value; }
        public int Numero { get => _numero; set => _numero = value; }

        public Card(char seme, int numero, int valore)
        {
            this._seme = seme;
            this._numero = numero;
            this._valore = valore;
        }
    }
}
