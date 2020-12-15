using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageBase : Serializable {
    public delegate void OnMessageDelegate(MessageBase m);
    public OnMessageDelegate OnMessage { get; set; } 
    public virtual void Process() { }
    public MessageBase(): base() {
    }

}