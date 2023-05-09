using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Pilots.Common;

public class GeneratePhoto : NetworkInstantiator
{
    [Tooltip("Picture resolution")]
    public int resWidth = 3200;
    [Tooltip("Picture resolution")]
    public int resHeight = 2400;
    [Tooltip("The unity Camera that takes the picture")]
    public Camera cameraMe;
   
    public bool shot = false;


    protected override GameObject InstantiateTemplateObject()
    {
        RenderTexture original = cameraMe.targetTexture;
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        cameraMe.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);

        // Render the camera's view.
        cameraMe.Render();

        // The Render Texture in RenderTexture.active is the one
        // that will be read by ReadPixels.
        RenderTexture.active = rt;

        // Make a new texture and read the active Render Texture into it.
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenShot.Apply();

        cameraMe.targetTexture = original;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(rt);

        //byte[] bytes = screenShot.EncodeToPNG();
        //generate new photos
        GameObject newphoto = Instantiate(templateObject, location.transform.position + new Vector3(0, 0.01f, 0), location.transform.rotation);
        newphoto.GetComponent<Renderer>().material.mainTexture = screenShot;
        return newphoto;

    }

}
