using UnityEngine;
//XXXShishir switched back to original pilot0controller, ToDo: Cherry pick the rating scale scene transitions, ToDo: Reimplement scene controller to use modified entity pipeline later
//Note: Use scenemanager.loadsceneasync for the rating scale scene once remote user prerecorded view is reimplemented
public class QualityAssesmentController : PilotController {
    public static QualityAssesmentController Instance { get; private set; }

    public void Awake() {
        if (Instance == null) {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    public override void Start() { 
        base.Start();
    }

    public override void MessageActivation(string message) { }
}
