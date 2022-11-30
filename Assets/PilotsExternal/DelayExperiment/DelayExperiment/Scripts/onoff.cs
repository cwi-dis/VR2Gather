using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interactive360.Utils;
using UnityEngine.Video;
public class onoff : MonoBehaviour {
    public bool isdisplay = false;
	private Shader shader1;
	private Shader shader2;
	private Renderer rend;
	bool flag;
	// Use this for initialization
	void Start () {
		flag = false;
		rend = GetComponent<Renderer>();
		if (rend == null) rend = GetComponentInChildren<Renderer>();
		shader1 = Shader.Find("Standard");

		shader2 = (!isdisplay) ? Shader.Find("Unlit/Color"):Shader.Find("Outlined/Diffuse");
        // CameraEditorControlVoting.onreturnflag += HandleOnFlagReturn;
    }
	void HandleOnFlagReturn (List<bool> flags_rec){
		flag = flag; // all of the display highlight flags are received, only the number "videonumber" is stored
	}
	public void Highlight(bool highlight){
		flag =highlight;

        
    }
	// Update is called once per frame
	void Update () {
        if (flag)
        {
            rend.material.shader = Shader.Find("Unlit/Color");
            rend.material.SetColor("_Color", Color.red);
        }
        else
        {
            rend.material.shader = Shader.Find("Unlit/Color");
            rend.material.SetColor("_Color", Color.gray);
        }
		/*rend.material.shader = (flag== true) ? shader2:shader1; // if the flag is set the display highlights
        rend.material.SetColor(Shader.PropertyToID("Unlit/Color"));*/
	}
}

