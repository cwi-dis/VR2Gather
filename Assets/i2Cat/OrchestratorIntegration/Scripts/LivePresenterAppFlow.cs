using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LivePresenterAppFlow : MonoBehaviour {

    [SerializeField] GameObject panelIP;
    [SerializeField] GameObject panelHoward;
    [SerializeField] GameObject panel180;
    [SerializeField] InputField ipInput;
    [SerializeField] OscJack.OscPropertySender sender;

    // Start is called before the first frame update
    void Start() {
        panelIP.SetActive(true);
        panelHoward.SetActive(false);
        panel180.SetActive(false);
        sender.gameObject.SetActive(false);
    }

    public void AssignIP() {
        sender._ipAddress = ipInput.text;

        panelIP.SetActive(false);
        panelHoward.SetActive(true);
        panel180.SetActive(true);
        sender.gameObject.SetActive(true);
    }

}
