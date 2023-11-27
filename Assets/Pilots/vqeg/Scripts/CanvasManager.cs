using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public Canvas[] canvases;
    public Button[] nextButtons;
    private int currentCanvasIndex = 0;

    private void Start()
    {
        if (canvases == null || canvases.Length == 0)
        {
            Debug.LogError("Canvases array is not initialized or empty.");
            return;
        }

        if (nextButtons == null || nextButtons.Length == 0)
        {
            Debug.LogError("NextButtons array is not initialized or empty.");
            return;
        }

        InitializeCanvases();

        for (int i = 0; i < nextButtons.Length; i++)
        {
            int index = i;
            nextButtons[i].onClick.AddListener(() => OnNextButtonClick(index));
        }
    }

    private void InitializeCanvases()
    {
        foreach (Canvas canvas in canvases)
        {
            canvas.gameObject.SetActive(false);
        }
        canvases[0].gameObject.SetActive(true);
    }

    private void OnNextButtonClick(int buttonIndex)
    {
        canvases[currentCanvasIndex].gameObject.SetActive(false);

        currentCanvasIndex++;
        if (currentCanvasIndex >= canvases.Length)
        {
            currentCanvasIndex = 0; // Reset to the first canvas or handle end of array differently
        }

        canvases[currentCanvasIndex].gameObject.SetActive(true);
    }
}
