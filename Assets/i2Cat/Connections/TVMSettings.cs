using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TVMSettings : NetworkBehaviour {

    [SyncVar]
    public string uri;

    ShowTVMs tvm;

	// Use this for initialization
	void Awake () {
		tvm = gameObject.GetComponent<ShowTVMs>();

        tvm.connectionURI = uri;
    }
	
}
