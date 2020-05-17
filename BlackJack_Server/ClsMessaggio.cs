using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOCKET_UDP
{
    public class ClsMessaggio
    {
        private string _ip;
        private string _port;
        private string _messaggio;

        public string Ip { get => _ip; set => _ip = value; }
        public string Port { get => _port; set => _port = value; }
        public string Messaggio { get => _messaggio; set => _messaggio = value; }

        //costruttori
        /// <summary>
        /// Nuova istanza standard nella classe
        /// Con parametri standard in caso di omissione
        /// </summary>
        /// <param name="ip">IP</param>
        /// <param name="port">PORT</param>
        /// <param name="messaggio">MESSAGGIO RELATIVO</param>
        public ClsMessaggio(string ip = "", string port= "", string messaggio = "")
        {
            this._ip = ip;
            this._port = port;
            this._messaggio = messaggio;
        }

        public ClsMessaggio(string csv, char separatore = ';') => fromCSV(csv, separatore);


        //stringa concatenata
        /// <summary>
        /// Per concatenerare le informazioni in un'unica stringa
        /// </summary>
        /// <param name="separatore">Di default è ;</param>
        /// <returns>Stringa in formato CSV</returns>

        public string toCSV(char separatore = ';') => _ip + separatore + _port + separatore + _messaggio;

        public void fromCSV(string csv, char separatore = ';')
        {
            string[] param = csv.Split(separatore);
            _ip = param[0];
            _port = param[1];
            _messaggio = param[2];
        }

        public string[] toArray() => new string[] { _ip, _port, _messaggio };
    }
}
