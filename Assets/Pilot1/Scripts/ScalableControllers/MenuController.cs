using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour {

    public Dropdown expMode;
    public Dropdown playerSeat;
    public InputField playerIp;
    public InputField voIPPort;
    public Toggle debugMode;

    // Use this for initialization
    void Start() {
        ConfigManager.ReadConfig();

        expMode.value = ConfigManager.config.playerMode;
        playerSeat.value = ConfigManager.config.is_player_1 ? 0 : 1;
        playerIp.text = ConfigManager.config.player2_ip;
        voIPPort.text = ConfigManager.config.port.ToString();
        debugMode.isOn = ConfigManager.config.single_player;
    }

    // Only for Debugg PC Controls
    void Update() {
        if (Input.GetKeyUp(KeyCode.Space)) StartExperience();

        if (Input.GetKeyDown(KeyCode.C)) Debug.Log(JsonUtility.ToJson(ConfigManager.config, true));
    }

    public void StartExperience() {
        ConfigManager.config.playerMode = expMode.value;
        ConfigManager.config.is_player_1 = playerSeat.value == 0;
        ConfigManager.config.player2_ip = playerIp.text;
        int.TryParse(voIPPort.text, out ConfigManager.config.port);
        ConfigManager.config.single_player = debugMode.isOn;

        //ConfigManager.WriteConfig();
        SceneManager.LoadScene("02.TVMCalibration");
    }
}
