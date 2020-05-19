﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using SOCKET_UDP;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace BlackJack_Server
{
    public partial class Form1 : Form
    {
        clsServerUDP server;
        Dictionary<int,clsClientUDP> clients;
        Dictionary<int, Player> lobby;

        Player_Controller p_controller;

        public Form1()
        {
            InitializeComponent();
            server = new clsServerUDP(IPAddress.Parse(GetLocalIPAddress()), 7777);
            clients = new Dictionary<int, clsClientUDP>();
            p_controller = new Player_Controller();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            server.avvia();
            server.datiRicevutiEvent += Server_datiRicevutiEvent;
        }

        private void Server_datiRicevutiEvent(ClsMessaggio message)
        {
            string[] ricevuti = message.toArray();
            ObjMex msg = new ObjMex(null, null, null);
            msg = JsonConvert.DeserializeObject<ObjMex>(ricevuti[2]);
            switch(msg.Action)
            {
                case "new-conn":
                    int id = GeneraId();
                    clsClientUDP client = new clsClientUDP(IPAddress.Parse(GetLocalIPAddress()), (int)msg.SingleData);
                    clients.Add(id,client);
                    ClsMessaggio mex = new ClsMessaggio(GetLocalIPAddress(), msg.SingleData.ToString());
                    ObjMex objMex = new ObjMex("conn-established", id);
                    mex.Messaggio = JsonConvert.SerializeObject(objMex);
                    client.Invia(mex);
                    break;
                case "login-ask":
                    int id_player = (int)msg.MultipleData[0];
                    Player player = msg.MultipleData[1] as Player;
                    if ((player = p_controller.ReadPlayer(player.Email, player.Password)) == null)
                    {
                        ClsMessaggio mes = new ClsMessaggio(GetLocalIPAddress(), msg.MultipleData.ToString());
                        ObjMex objMes = new ObjMex("login-failed", "");
                        mes.Messaggio = JsonConvert.SerializeObject(objMes);
                        clients[id_player].Invia(mes);
                    }
                    else
                    {
                        ClsMessaggio mes = new ClsMessaggio(GetLocalIPAddress(), msg.MultipleData.ToString());
                        ObjMex objMes = new ObjMex("login-success", JsonConvert.SerializeObject(player));
                        mes.Messaggio = JsonConvert.SerializeObject(objMes);
                        clients[id_player].Invia(mes);
                    }
                    
                    break;
            }
        }

        private int GeneraId()
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            int id;
            bool alr_existing = false;
            do
            {
                alr_existing = false;
                id = rnd.Next(10000, 100000);
                foreach (var key in clients.Keys)
                {
                    if(key == id)
                    {
                        alr_existing = true;
                        break;
                    }
                }
            }
            while (alr_existing);
            return id;
        }

        public static string GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Nessuna interfaccia di rete disponibile su questo computer");
        }
    }
}
