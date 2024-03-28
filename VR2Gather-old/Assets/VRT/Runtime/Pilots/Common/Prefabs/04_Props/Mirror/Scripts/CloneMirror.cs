using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloneMirror : MonoBehaviour
{
    public bool trackEyePosition;
    public Camera mirrorCamera;
    public MeshRenderer mirrorMeshRenderer;
    public Material _ClonedMaterial;
    public RenderTexture _ClonedTexture;


    private void Awake()
    {
        _ClonedMaterial = Instantiate(mirrorMeshRenderer.material);
        _ClonedTexture = Instantiate(mirrorCamera.targetTexture);
        _ClonedMaterial.mainTexture = _ClonedTexture;
        mirrorMeshRenderer.material = _ClonedMaterial;
        mirrorCamera.targetTexture = _ClonedTexture;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (trackEyePosition && Camera.main != null)
        {
            float mainCamHeight = Camera.main.transform.position.y;
            Vector3 camPosition = mirrorCamera.transform.position;
            camPosition.y = mainCamHeight;
            mirrorCamera.transform.position = camPosition;
        }
    }
}
