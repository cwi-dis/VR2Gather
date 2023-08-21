
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;
using System.Collections.Generic;

public class VideoQualityRating : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public Button[] ratingButtons;
    private int selectedRating = -1; // Default value to indicate no selection

    private List<string> userResponses = new List<string>();

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

    public void OnRatingButtonClick(int rating)
    {
        selectedRating = rating;
        SaveUserResponseToList();
        UpdateButtonHighlighting(rating);
    }

    private void SaveUserResponseToList()
    {
        if (selectedRating == -1)
        {
            Debug.LogWarning("No rating selected.");
            return;
        }

        var button = ratingButtons[selectedRating - 1];
        var comp = button.GetComponentInChildren<TextMeshProUGUI>();
        string ratingText = comp.text;
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string response = $"{timestamp} - Video Quality Rating: {ratingText}";

        userResponses.Add(response);

        Debug.Log("User response added: " + response);
    }

    public void SaveResponsesToFile()
    {
        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            foreach (string response in userResponses)
            {
                writer.WriteLine(response);
            }
        }

        Debug.Log("All user responses saved to file.");
    }



    private void UpdateButtonHighlighting(int selectedRating)
    {
        for (int i = 0; i < ratingButtons.Length; i++)
        {
            bool isSelected = (i + 1) == selectedRating;
            ColorBlock colors = ratingButtons[i].colors;
            colors.normalColor = isSelected ? Color.yellow : Color.white;
            ratingButtons[i].colors = colors;
        }
    }

}
