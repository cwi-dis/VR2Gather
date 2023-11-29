using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cwipc;

public class VideoQualityRating : MonoBehaviour
{
    public Button nextButton;      // The next button
    public int currentRating = -1; // Invalid default value to ensure selection
    private static string fileName;
    public TMP_Text canvasText;
    // Mainly for debug messages:
    static int instanceCounter = 0;
    int instanceNumber = instanceCounter++;

    public string Name()
    {
        return $"{GetType().Name}#{instanceNumber}";
    }

    void Awake()
    {
        Debug.Log($"{Name()}: Awake()");
        // Set the file name only once when the first instance of the script is loaded
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "Rating_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        }
    }

    void Start()
    {
        
        if (canvasText != null)
        {
            string currentText = canvasText.text;
            Debug.Log($"{Name()}: Current Canvas Text: {currentText}");
        }
        else
        {
            Debug.LogError($"{Name()}: CanvasText is not assigned!");
        }
        InitializeRating();
        nextButton.onClick.AddListener(SaveRatingAndProceed);
    }

    void InitializeRating()
    {
        // I dont want to have a seperate file for each rating. 
        //fileName = "Rating_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        nextButton.interactable = false; // Disable next button initially
    }

    public void SetRating(int rating) // setrating int 
    {
        currentRating = rating; // int.Parse(clickedButton.name); // Assuming button names are set to their respective rating values
        Debug.Log($"{Name()}: rating={rating}");
        nextButton.interactable = true; // Enable next button when a rating is selected
    }
        
    public void SaveRatingAndProceed()
    {
        if (currentRating == -1)
        {
            Debug.LogError($"{Name()}: Rating = -1"); // need to fix  this.
        }
        else
        {
            string ratingText = $"canvasText.text: {currentRating}\n";
            Statistics.Output(Name(), $"question={ratingText}, rating={currentRating}");

            File.AppendAllText(fileName, ratingText);
            // Load the next question or handle the end of the questionnaire
            currentRating = -1; // Reset rating for the next question
            nextButton.interactable = false; // Disable next button until new rating is chosen
        }
    }
}
