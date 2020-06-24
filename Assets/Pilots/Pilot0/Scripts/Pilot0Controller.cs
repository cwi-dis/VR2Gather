using UnityEngine;

public class Pilot0Controller : PilotController {

    public PlayerManager[] players;

    // Start is called before the first frame update
    public override void Start() { 
        base.Start();
        LoadPlayers(players);
    }

    public override void MessageActivation(string message) { }
}
