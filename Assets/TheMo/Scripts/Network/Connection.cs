using System.Collections.Generic;
using UnityEngine;
// using WebSocketSharp;

public class BinaryConnection{
    public delegate void NetEvent(ushort code);

    Queue<byte[]> messagesQueue = new Queue<byte[]>();

//    WebSocket   socket;
    bool        _isConnected=false;

    public BinaryConnection() { }

    public void Connect(string url) {
        /*
        socket = new WebSocket(url);
        socket.WaitTime = System.TimeSpan.FromSeconds(0.25);
        socket.OnMessage += OnMessage;
        socket.OnOpen  += (sender, e) => {
            _isConnected = true;
            Send(GetMessage<NetTime>().Prepare()); OnConnect?.Invoke(0);
        };
        socket.OnClose += (sender, e) => {
            _isConnected = false;
//            Debug.Log($"OnClose [{e.Code}:{e.Reason}]");
            OnDisconnect?.Invoke(e.Code);
        };
        socket.OnError += (sender, e) => { Debug.Log($"OnError {e.Message} {e.Exception}\n >>>>{e.Exception.StackTrace}"); };
        socket.ConnectAsync();
        */
    }

    public NetEvent OnConnect;
    public NetEvent OnDisconnect;


    public bool isConnected { get { return _isConnected; } }
    public void Close() {
//        socket?.CloseAsync();
    }

    void OnMessage(object sender, byte[] data) {
        if (data.Length > 1)  messagesQueue.Enqueue(data);
    }

    byte[] sendBuffer = new byte[4096];
    public void Send(MessageBase message) {
        int len = message.Serialize(null, sendBuffer, 0);
        // socket.Send( new System.IO.MemoryStream(sendBuffer, 0, len ));
    }

    Dictionary<ushort,MessageBase> messages = new Dictionary<ushort, MessageBase>();
    public T RegisterMessage<T>() where T : MessageBase {
        MessageBase ins = (MessageBase)System.Activator.CreateInstance(typeof(T));
        ushort id = Hash(typeof(T).Name);
        typeof(T).GetField("id").SetValue(ins, id);
        if (!messages.ContainsKey(id)) { 
            messages.Add(id, ins);
            return (T)ins;
        } else {
            Debug.Log($"Class name clash {typeof(T).Name}");
        }
        return default(T);
    }

    public T GetMessage<T>() where T : MessageBase {
        return (T)messages[Hash(typeof(T).Name)];
    }

    ushort Hash(string str) {
        ushort hash = (ushort)str.Length;
        foreach (char chr in str) { 
            hash = (ushort)(((hash << 5) - hash) + chr);
            hash = (ushort)(hash & hash);
        }
        return hash;
    }

    public void Update() {
        while (messagesQueue.Count > 0) {
            byte[] data = messagesQueue.Dequeue();
            ushort id = (ushort)(data[0] | data[1] << 8);
            MessageBase msg = messages[id];
            if (msg != null) {
                msg.Deserialize(null, data, 0);
                msg.Process();
                msg.OnMessage?.Invoke(msg);
            }

        }

    }
}