using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class VideoQualityRating : MonoBehaviour
{
    public Button[] ratingButtons; // Array of buttons for ratings
    public Button nextButton;      // The next button
    private int currentRating = -1; // Invalid default value to ensure selection
    private string fileName;

    void Start()
    {
        InitializeRating();
        nextButton.onClick.AddListener(SaveRatingAndProceed);
        foreach (var button in ratingButtons)
        {
            button.onClick.AddListener(() => SetRating(button));
        }
    }

    void InitializeRating()
    {
        fileName = "Rating_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        nextButton.interactable = false; // Disable next button initially
    }

    public void SetRating(Button clickedButton) // setrating int 
    {
        currentRating = int.Parse(clickedButton.name); // Assuming button names are set to their respective rating values
        nextButton.interactable = true; // Enable next button when a rating is selected
    }

    public void SaveRatingAndProceed()
    {
        if (currentRating != -1)
        {
            File.AppendAllText(fileName, currentRating.ToString() + "\n");
            // Load the next question or handle the end of the questionnaire
            currentRating = -1; // Reset rating for the next question
            nextButton.interactable = false; // Disable next button until new rating is chosen
        }
    }
}
