using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
namespace SOCKET_UDP
{
    class clsClientUDP
    {
        const int MAX_BYTE = 1024;

        //coppia ip porta
        private Socket _socketClient;   //in c# solo ip (no porta)
        private EndPoint _endPointServer;   //contiene riferimento a ip e porta

        public clsClientUDP(IPAddress ipServer, int portServer)
        {
            _socketClient = new Socket(AddressFamily.InterNetwork,
                                        SocketType.Dgram,
                                        ProtocolType.Unspecified);
            _endPointServer = new IPEndPoint(ipServer, portServer);
        }

        public void Invia(ClsMessaggio mex)
        {
            string messaggio = mex.toCSV();
            byte[] bufferTransmission;
            bufferTransmission = Encoding.ASCII.GetBytes(messaggio);
            //Apre un socket, invia al server e richiude
            _socketClient.SendTo(bufferTransmission, 
                                bufferTransmission.Length, 
                                SocketFlags.None, 
                                _endPointServer);
        }
    }
}
