using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace SOCKET_UDP
{
    public class clsAddress
    {
        public static List<IPAddress> ipList;
        
        static clsAddress()
        {
            ipList = new List<IPAddress>();
        }

        /// <summary>
        /// Metodo per la ricerca degli ip nel pc
        /// </summary>
        public static void cercaIP()
        {
            //Aggiungere il localhost
            ipList.Add(IPAddress.Parse("127.0.0.1"));
            //recuperare info su ip
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if(ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipList.Add(ip);
                }
            }
        }

    }
}
