using UnityEngine;

public class Pilot0Controller : PilotController {
    public static Pilot0Controller Instance { get; private set; }

    public PlayerManager[] players;
    public PlayerManager[] spectators;
    public GameObject voyeur;

    public void Awake() {
        if (Instance == null) {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    public override void Start() { 
        base.Start();
        LoadPlayers(players, spectators);
        if (OrchestratorController.Instance.SelfUser.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__NONE__)
            voyeur.SetActive(true);
    }

    public override void MessageActivation(string message) { }
}
