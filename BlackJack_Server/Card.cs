using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackJack_Server
{
    /// <summary>
    /// Carta generica per il blackjack
    /// </summary>
    public class Card
    {
        private char _seme;
        private int _numero;
        private int _valore;

        public char Seme { get => _seme; set => _seme = value; }
        public int Valore { get => _valore; set => _valore = value; }
        public int Numero { get => _numero; set => _numero = value; }

        /// <summary>
        /// Istanza della classe carta
        /// </summary>
        /// <param name="seme">Seme della carta</param>
        /// <param name="numero">Numero della carta</param>
        /// <param name="valore">Valore della carta</param>
        public Card(char seme, int numero, int valore)
        {
            this._seme = seme;
            this._numero = numero;
            this._valore = valore;
        }
    }
}
