using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OrchestratorWrapping;

public class SelfRepresentationPreview : MonoBehaviour
{
    public static SelfRepresentationPreview Instance { get; private set; }

    public PlayerManager player;

    // Start is called before the first frame update
    void Start() {
        if (Instance == null) {
            Instance = this;
        }
    }

    public void Stop() {
        player.avatar.SetActive(false);
        if (player.webcam.TryGetComponent(out WebCamPipeline web))
            Destroy(web);
        player.webcam.SetActive(false);
        if (player.pc.TryGetComponent(out EntityPipeline pointcloud))
            Destroy(pointcloud);
        if (player.pc.TryGetComponent(out Workers.PointBufferRenderer renderer))
            Destroy(renderer);
        player.pc.SetActive(false);
        player.tvm.gameObject.SetActive(false);
    }

    public void ChangeRepresentation(UserData.eUserRepresentationType representation, string webcamName, string microphoneName) {
        if (OrchestratorController.Instance == null || OrchestratorController.Instance.SelfUser==null) return;

        player.userName.text = OrchestratorController.Instance.SelfUser.userName;
        player.gameObject.SetActive(true);
        Stop();
        switch (representation) {
            case UserData.eUserRepresentationType.__NONE__:
                player.gameObject.SetActive(false);
                break;
            case UserData.eUserRepresentationType.__2D__:
                player.webcam.SetActive(true);
                if (webcamName != "None") {
                    WebCamPipeline wcPipeline = player.webcam.AddComponent<WebCamPipeline>();
                    wcPipeline.Init(new User() { userData = new UserData() { webcamName = webcamName, microphoneName = "None" } }, Config.Instance.LocalUser, false, true);
                }

                break;
            case UserData.eUserRepresentationType.__AVATAR__:
                player.avatar.SetActive(true);
                break;
            case UserData.eUserRepresentationType.__TVM__:
                //player.tvm.gameObject.SetActive(true);
                Debug.Log("TVM PREVIEW");
                break;
            case UserData.eUserRepresentationType.__PCC_CWI_:
                player.pc.SetActive(true);
                player.pc.AddComponent<EntityPipeline>().Init(new User() { userData = new UserData() { userRepresentationType = UserData.eUserRepresentationType.__PCC_CWI_ } }, Config.Instance.LocalUser, true);
                break;
            case UserData.eUserRepresentationType.__PCC_SYNTH__:
                player.pc.SetActive(true);
                player.pc.AddComponent<EntityPipeline>().Init(new User() { userData = new UserData() { userRepresentationType = UserData.eUserRepresentationType.__PCC_SYNTH__ } }, Config.Instance.LocalUser, true);
                break;
            case UserData.eUserRepresentationType.__PCC_CERTH__:
                player.pc.SetActive(true);
                player.pc.AddComponent<EntityPipeline>().Init(new User() { userData = new UserData() { userRepresentationType = UserData.eUserRepresentationType.__PCC_CERTH__ } }, Config.Instance.LocalUser, true);
                break;
            case UserData.eUserRepresentationType.__SPECTATOR__:
                player.gameObject.SetActive(false);
                break;
            default:
                break;
        }
    }
}
