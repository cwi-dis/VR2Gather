using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Prototype.NetworkLobby;
using System.Net;
using System.Net.Sockets;

public class TVMSettings_Hook : LobbyHook {

    public LobbyPlayer myLobby;

    public static string LocalIPAddress()
    {
        IPHostEntry host;
        string localIP = "0.0.0.0";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                if (localIP.StartsWith("192.168")) break;
            }
        }
        return localIP;
    }

    void Start()
    {       
        myLobby.OnMyURI(LocalIPAddress());
        myLobby.OnMyExchange("");

        #region Debugs
        Debug.Log("IP: " + LocalIPAddress());
        Debug.Log("Name: " + myLobby.playerName);
        string uri = "amqp://tofis:tofis@";
        string IP = myLobby.playerName;
        uri += IP;
        uri += ":5672";
        Debug.Log("URL: " + uri);
        #endregion
    }

    public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager manager, GameObject lobbyPlayer, GameObject gamePlayer)
    {
        LobbyPlayer lobby = lobbyPlayer.GetComponent<LobbyPlayer>();
        ShowTVMs tvm = gamePlayer.GetComponentInChildren<ShowTVMs>();

        string uri = "amqp://tofis:tofis@";
        string IP = lobby.playerURI;
        uri += IP;
        uri += ":5672";

        tvm.connectionURI = uri;
        tvm.exchangeName = lobby.playerExchange;

        //base.OnLobbyServerSceneLoadedForPlayer(manager, lobbyPlayer, gamePlayer);
    }

}
