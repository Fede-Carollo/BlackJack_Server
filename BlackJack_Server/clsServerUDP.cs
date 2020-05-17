using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SOCKET_UDP
{

    public delegate void datiRicevutiEventHandler(ClsMessaggio message);
    class clsServerUDP
    {
        const int MAX_BYTE = 1024;
        private Socket socketServer;
        private EndPoint endPointServer;
        private EndPoint endPointClient;
        private Thread thAscolto;

        private volatile bool canListen = true;

        public event datiRicevutiEventHandler datiRicevutiEvent;

        /// <summary>
        /// Nuova istanza clsServer
        /// </summary>
        /// <param name="ip">Ip del server su cui ascolta</param>
        /// <param name="port">Porta delserver su cui ascoltaz</param>
        public clsServerUDP(IPAddress ip, int port)
        {
            socketServer = new Socket(AddressFamily.InterNetwork,
                                      SocketType.Dgram,
                                      ProtocolType.Unspecified);
            endPointServer = new IPEndPoint(ip, port);
            socketServer.Bind(endPointServer);
            //bind apre il socket
        }

        public void avvia()
        {
            if(thAscolto == null)
            {
                thAscolto = new Thread(ricevi);
                thAscolto.Start();
                //attendo finchè non parte
                while (!thAscolto.IsAlive) ;
            }    
        }

        private void ricevi()
        {
            //corpo del thread
            int nByteRicevuti;
            string message;
            byte[] bufferRx;

            bufferRx = new byte[MAX_BYTE];

            //inizializzare endpointclient
            endPointClient = new IPEndPoint(IPAddress.Parse("127.0.0.3"), 5785);
            //carica auto le info del mittente
            //resize bufferRx
            
            while(canListen)
            {
                try
                {
                    nByteRicevuti = socketServer.ReceiveFrom(bufferRx, MAX_BYTE, SocketFlags.None, ref endPointClient);
                    message = Encoding.ASCII.GetString(bufferRx);
                    ClsMessaggio mex = new ClsMessaggio(message,';');
                    mex.Ip = (endPointClient as IPEndPoint).Address.ToString();
                    mex.Port = (endPointClient as IPEndPoint).Port.ToString();

                    datiRicevutiEvent(mex);
                }
                catch (SocketException ex)
                {
                    //mancanza di dati è 10004
                    if(ex.ErrorCode != 10004)
                    {
                        System.Windows.Forms.MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        public void chiudi()
        {
            canListen = false;
            thAscolto.Join();
            socketServer.Close();
        }
    }
}
