using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Prototype.NetworkLobby;
using System.Net;
using System.Net.Sockets;

public class TVMSettings_Hook : LobbyHook {

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
                break;
            }
        }
        return localIP;
    }

    void Start()
    {
        Debug.Log(NetworkManager.singleton.networkAddress);
        Debug.Log(LocalIPAddress()); 
    }

    public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager manager, GameObject lobbyPlayer, GameObject gamePlayer)
    {
        LobbyPlayer lobby = lobbyPlayer.GetComponent<LobbyPlayer>();
        ShowTVMs tvm = gamePlayer.GetComponent<ShowTVMs>();

        string uri = "amqp://tofis:tofis@";
        string IP = NetworkManager.singleton.networkAddress;
        uri += IP;
        uri += ":5672";

        tvm.connectionURI = uri;

        //base.OnLobbyServerSceneLoadedForPlayer(manager, lobbyPlayer, gamePlayer);
    }

}
