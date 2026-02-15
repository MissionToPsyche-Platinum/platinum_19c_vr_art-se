using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Windows;
using Object = UnityEngine.Object;

public class TextBoxController : MonoBehaviour
{
    [Tooltip("The text will appear to the right of this frame part.")]
    [SerializeField] private Transform frameTransform;

    [Tooltip("Transform of this object")]
    [SerializeField] private RectTransform rectTransform;

    [Tooltip("The text field that shows the art description.")]
    [SerializeField] private TextMeshProUGUI artDesc;

    private float defaultFontSize;

    /* Credit to @zbarlow FrameController for some of the base setup functionality*/
    public void Awake()
    {
        defaultFontSize = artDesc.fontSize;
        SettingsManager.m_TextSizeChanged.AddListener(ChangeTextSize);
        ChangeTextSize();
        UpdateTextLocation();
    }

    public void ChangeTextSize()
    {
        artDesc.fontSize = defaultFontSize * GlobalSettings.TEXT_SIZE_MULTIPLIER;
    }

    public void UpdateTextLocation()
    {
        rectTransform.transform.eulerAngles = new Vector3(0, frameTransform.transform.eulerAngles.y + 180f, 0);
        rectTransform.position = frameTransform.position + frameTransform.right * 0.8f - frameTransform.up * 0.5f;
    }

    public void SetDescText(ArtworkData data)
    {
        string artistNameString = data.artistName;
        string titleAndDateString = data.artworkName + " - " + formatArtworkDataDate(data.artworkDate);
        string genreString = capitalizeFirstLetterOfEachWord(data.genre);

        // split title and date again (so we can bold the title)
        string artTitle = titleAndDateString.Split(" - ")[0];
        string artDate = titleAndDateString.Split(" - ")[1];

        // bold and space things out
        string organizedText = "<b>" + artistNameString + "</b>" +
                               "\n\n<b>" + artTitle + "</b> - " + artDate +
                               "\n\n\n<i>" + genreString + "</i>";

        // TODO: Add this to scrolling textbox
        // string descriptionText = "Artist Major: " + data.artistMajor + "\n\n" + data.artworkDescription;

        artDesc.text = organizedText;
        UpdateTextLocation();
    }

    // method to make date from ArtworkData more readable
    private string formatArtworkDataDate(string date)
    {
        if (date == null)
            throw new ArgumentNullException(nameof(date));

        string[] parts = date.Split(
            new[] { " - " },
            3, // maximum number of substrings
            StringSplitOptions.None);

        if (parts.Length != 3)
            throw new FormatException("Input string does not contain exactly two ' - ' delimiters.");

        return parts[0] + " " + parts[1] + ", " + parts[2]; // month day, year
    }

    private string capitalizeFirstLetterOfEachWord(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Split the string into words
        string[] words = input.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]))
            {
                string word = words[i];
                words[i] = char.ToUpper(word[0]) + word.Substring(1).ToLower();
            }
        }

        // Rejoin the words
        return string.Join(" ", words);
    }
}
