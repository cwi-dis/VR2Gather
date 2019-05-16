using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreparationTest : MonoBehaviour {

    [SerializeField]
    private PlayerManager[] players;

    private OrchestrationTest test;
    private OrchestratorGui orchestrator;

    private GameObject mainPanel;
    private GameObject background;

    #region Utils
    private Color playerCol = new Color(0.15f, 0.78f, 0.15f); // Green
    private Color otherCol = new Color(1.0f, 1.0f, 1.0f); // White
    private Color offlineCol = new Color(1.0f, 0.5f, 0.5f); // Red
    #endregion

    // Start is called before the first frame update
    void Start() {
        mainPanel = GameObject.Find("MainPanel");
        background = GameObject.Find("Background");
        test = GameObject.Find("ManagerTest").GetComponent<OrchestrationTest>();
        orchestrator = GameObject.Find("MainWindow").GetComponent<OrchestratorGui>();
        mainPanel.SetActive(false);
        background.SetActive(false);

        // Put the players on the correct seat
        for (int i = 0; i < orchestrator.activeSession.sessionUsers.Length; i++) {
            if (orchestrator.activeSession.sessionUsers[i] == orchestrator.TestGetUserID()) players[i].cam.SetActive(true);
            //TODO put exchange name and connection uri in players[i].tvm
        }
    }
    // Update is called once per frame
    void Update() {
        
    }
}
