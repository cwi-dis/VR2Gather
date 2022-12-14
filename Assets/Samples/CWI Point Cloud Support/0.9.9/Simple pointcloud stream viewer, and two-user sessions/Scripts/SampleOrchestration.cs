using System;
using System.Collections;
using System.Collections.Generic;
using Cwipc;
using UnityEngine;

public class SampleOrchestration : MonoBehaviour
{
    protected SimpleSocketReceiver controlReceiver;
    protected SimpleSocketSender controlSender;
    protected Dictionary<string, Action<string>> callbacks = new Dictionary<string, Action<string>>();
    [Serializable]
    protected struct OrchestratorMessage<T>
    {
        public string command;
        public T argument;
    };
    [Serializable]
    protected struct BareOrchestratorMessage
    {
        public string command;
    };

    private void OnDestroy()
    {
        controlSender?.Stop();
        controlSender = null;
        controlReceiver?.Stop();
        controlReceiver = null;
    }

    private void Update()
    {
        if (controlReceiver != null)
        {
            string msg = controlReceiver.Receive();
            if (msg != null)
            {
                MessageReceived(msg);
            }
        }
    }

    /// <summary>
    /// Initialize the base class (host names), then create the server to and client to
    /// communicate control information with the other side.
    /// </summary>
    public void Initialize(string senderUrl, string receiverUrl)
    {
        controlSender = new SimpleSocketSender(senderUrl);
        controlReceiver = new SimpleSocketReceiver(receiverUrl);
    }

    public void Send<T>(string command,T argument)
    {
        //
        // We wrap the command and argument in a single JSON structure and forward it.
        //
        OrchestratorMessage<T> message = new OrchestratorMessage<T>()
        {
            command = command,
            argument = argument
        };
        string jsonEncoded = JsonUtility.ToJson(message);
        controlSender.Send(jsonEncoded);
    }

    public void RegisterCallback<T>(string command, Action<T> _callback)
    {
        //
        // The callback wrapper will JSON-parse the wrapper structure
        // and call out to the real callback.
        //
        void _callbackWrapper(string s)
        {
            OrchestratorMessage<T> message = JsonUtility.FromJson<OrchestratorMessage<T>>(s);
            _callback(message.argument);
        }
        Debug.Log($"SampleOrchestration: registered callback for \"{command}\"");
        callbacks[command] = _callbackWrapper;
    }

    protected void MessageReceived(string msg)
    {
        Debug.Log($"SampleOrchestration: Received message \"{msg}\"");
        //
        // We first JSON-parse the message and ignore everything but the command string.
        //
        BareOrchestratorMessage commandStruct = JsonUtility.FromJson<BareOrchestratorMessage>(msg);
        string command = commandStruct.command;
        //
        // Now we can call the callback wrapper for this command (if any) which will parse
        // the whole message and call the registered callback with the right argument.
        //
        if (callbacks.ContainsKey(command))
        {
            var cb = callbacks[command];
            cb(msg);
        }
        else
        {
            Debug.LogError($"SampleOrchestration: no callback registered for command \"{command}\"");
        }
    }

    protected class SimpleSocketReceiver : AsyncTCPReader
    {
        QueueThreadSafe myReceiveQueue = new QueueThreadSafe("SimpleSocketReceiver", 1, false);

        public SimpleSocketReceiver(string _url) : base(_url)
        {
            receivers = new ReceiverInfo[]
            {
                new ReceiverInfo()
                {
                    outQueue=myReceiveQueue,
                    host=url.Host,
                    port=url.Port,
                    fourcc=0x60606060
                }
            };
            Start();
        }

        public string Receive()
        {
            BaseMemoryChunk packet = myReceiveQueue.TryDequeue(0);
            if (packet == null) return null;
            string packetString = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(packet.pointer, packet.length);
            return packetString;
        }
    }

    protected class SimpleSocketSender : AsyncTCPWriter
    {
        QueueThreadSafe mySendQueue = new QueueThreadSafe("SimpleSocketSender", 1, false);

        public SimpleSocketSender(string _url) : base()
        {
            Uri url = new Uri(_url);
            descriptions = new TCPStreamDescription[]
            {
                new TCPStreamDescription()
                {
                    host=url.Host,
                    port=url.Port,
                    fourcc=0x60606060,
                    inQueue=mySendQueue
                }
            };
            Start();
        }

        public override void Stop()
        {
            if (!mySendQueue.IsClosed())
            {
                mySendQueue.Close();
            }
        }

        public void Send(string message)
        {
            byte[] messageBytes = System.Text.UTF8Encoding.UTF8.GetBytes(message);
            NativeMemoryChunk packet = new NativeMemoryChunk(messageBytes.Length);
            System.Runtime.InteropServices.Marshal.Copy(messageBytes, 0, packet.pointer, packet.length);
            mySendQueue.Enqueue(packet);
        }
    }
}
