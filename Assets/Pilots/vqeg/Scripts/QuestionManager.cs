using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cwipc;
using VRT.Pilots.Common;

public class QuestionManager : MonoBehaviour
{
    public Button nextButton;      // The next button
    public int currentRating = -1; // Invalid default value to ensure selection
    private static string fileName;
    public TMP_Text canvasText;
    private string currentText;
    // Mainly for debug messages:
    static int instanceCounter = 0;
    int instanceNumber = instanceCounter++;

    private static int saveRatingAndProceedCounter = 0; // I want to check how many times my SaveRatingAndProceed function is being called. 

    

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
            currentText = canvasText.text;
            Debug.Log($"{Name()}: Current Canvas Text: {currentText}");
        }
        else
        {
            Debug.LogError($"{Name()}: CanvasText is not assigned!");
        }
        InitializeRating();
    }

    void InitializeRating()
    {
        nextButton.interactable = false; // Disable next button initially
    }

    public void SetRating(int rating) 
    {
        currentRating = rating; // int.Parse(clickedButton.name); // Assuming button names are set to their respective rating values
        Debug.Log($"{Name()}: SetRating: rating={rating}");
        nextButton.interactable = true; // Enable next button when a rating is selected
    }
        
    public void SaveRatingAndProceed()
    {
        saveRatingAndProceedCounter++;
        Debug.Log($"{Name()}: SaveRatingAndProceed: rating={currentRating}, counter={saveRatingAndProceedCounter}");  // want to know how many times this function is called. 

      //  var stackTrace = new System.Diagnostics.StackTrace();
      //  Debug.Log($"{Name()}: SaveRatingAndProceed called, invocation count: {saveRatingAndProceedCounter}, called from: {stackTrace}");


        if (currentRating == -1)
        {
            Debug.LogError($"{Name()}: SaveRatingAndProceed: Rating = {currentRating}"); 
        }
        else
        {
            //string ratingText = $"canvasText: {currentText} {currentRating}\n";
            //Statistics.Output(Name(), $"question={ratingText}, rating={currentRating}");

            string ratingText = $"{currentText} {currentRating}\n";
            //string ratingText_File = $"{currentText} {currentRating}\n";

            Statistics.Output(Name(), $"question={currentText}, rating={currentRating}");
            File.AppendAllText(fileName, ratingText);
            // Load the next question or handle the end of the questionnaire
            currentRating = -1; // Reset rating for the next question
            nextButton.interactable = false; // Disable next button until new rating is chosen


            

        }

    }
}
