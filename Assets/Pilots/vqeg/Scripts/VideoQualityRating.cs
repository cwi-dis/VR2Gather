
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class VideoQualityRating : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public Button[] ratingButtons;
    private int selectedRating = -1; // Default value to indicate no selection

    private string filePath; // Path to save the user responses

    private void Start()
    {
        // Set the file path for saving user responses
        filePath = Path.Combine(Application.dataPath, "UserResponses.txt");

        // Set up button click listeners
        for (int i = 0; i < ratingButtons.Length; i++)
        {
            int ratingValue = i + 1;
            ratingButtons[i].onClick.AddListener(() => OnRatingButtonClick(ratingValue));
        }
    }

    private void OnRatingButtonClick(int rating)
    {
        selectedRating = rating;
        SaveUserResponseToFile();
    }

    private void SaveUserResponseToFile()
    {
        if (selectedRating == -1)
        {
            Debug.LogWarning("No rating selected.");
            return;
        }

        string ratingText = ratingButtons[selectedRating - 1].GetComponentInChildren<Text>().text;
        string response = $"Video Quality Rating: {ratingText}";

        // Append the response to the file
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine(response);
        }

        Debug.Log("User response saved: " + response);
    }
}
