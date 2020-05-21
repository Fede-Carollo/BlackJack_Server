using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackJack_Server
{
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
