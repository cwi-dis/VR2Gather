using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public Canvas canvas1;
    public Canvas canvas2;

    public Button[] canvas1Buttons;
    public Button canvas2Button;

    private void Start()
    {
        // Set up button click listeners
        foreach (Button button in canvas1Buttons)
        {
            button.onClick.AddListener(() => OnCanvas1ButtonClick(button));
        }

        canvas2Button.onClick.AddListener(OnCanvas2ButtonClick);

        // Show initial state
        ShowCanvas1();
    }

    private void OnCanvas1ButtonClick(Button clickedButton)
    {
        // Hide canvas1 and show canvas2
        canvas1.gameObject.SetActive(false);
        canvas2.gameObject.SetActive(true);
    }

    private void OnCanvas2ButtonClick()
    {
        // Hide canvas2 and show canvas1
        canvas2.gameObject.SetActive(false);
        canvas1.gameObject.SetActive(true);
    }

    private void ShowCanvas1()
    {
        canvas1.gameObject.SetActive(true);
        canvas2.gameObject.SetActive(false);
    }
}
