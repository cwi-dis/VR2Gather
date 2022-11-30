using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetCloser : MonoBehaviour {
    public string Description;
    public string duration;
    public string URL;
    public string URL360;
    public float points;
    private Vector3 _startPosition;
	private Quaternion _startRotation;
	private Vector3 Distance;
	float angle = 0f;
	float anglefinal = 0f;
	float periodcount = 0f;
	float omega = 1f;
	bool motionflag = false;
	float relation=1f;
	// Use this for initialization
	void Start () {
		_startPosition = transform.position;
		_startRotation = transform.rotation;
	}
	public void Startmotion(Vector3 cameraposition){
		transform.position=_startPosition;
		transform.rotation=_startRotation;
		Distance= new Vector3(_startPosition.x - cameraposition.x,_startPosition.z - cameraposition.z,cameraposition.y);
		relation = _startPosition.y/cameraposition.y;
		anglefinal = _startPosition.y - cameraposition.y;
		if (anglefinal >0) relation = 1/relation;
		angle = 0;
		omega = Mathf.Asin(Mathf.Abs(anglefinal/relation));
        motionflag = true;
        //transform.Translate(0f,-anglefinal,0f);

    }
	public void Stopmotion(){transform.position = _startPosition; transform.rotation=_startRotation ; motionflag = false; }
	
	// Update is called once per frame
	void Update () {
        float aver = 0f;
		if (motionflag){
            
            transform.Translate(-1*Distance.x*Time.deltaTime, (Mathf.Sin((2*Mathf.PI)*angle))*0.1f - anglefinal* Time.deltaTime, -1*Distance.y*Time.deltaTime,Space.World);
			angle += Time.deltaTime;
            if (angle >= 1.0f)
            {
                transform.Translate(0f, -transform.position.y + Distance.z,0f, Space.World);
                motionflag = false;
            }

        }
	}
}
