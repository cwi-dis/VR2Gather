using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class QuestionnaireManager : MonoBehaviour
{
    [Tooltip("Array of per-question canvases")]
    public Canvas[] canvases;
    [Tooltip("Array of per-question Next buttons")]
    public Button[] nextButtons;
    [Tooltip("Event called when all questions have been answered")]
    public UnityEvent OnEndOfQuestionnaireTrigger;
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
            if (OnEndOfQuestionnaireTrigger != null)
            {
                OnEndOfQuestionnaireTrigger.Invoke();
            } else
            {
                Debug.LogWarning("Questionnaire finished, but no end scene transition specified");
            }
        }
        else
        {
            canvases[currentCanvasIndex].gameObject.SetActive(true);
        }
    }

    
}
