using System;
using TMPro;
using UnityEngine;

public class TextBoxController : MonoBehaviour
{
    [Tooltip("The text will appear to the right of this frame part.")]
    [SerializeField] private Transform frameTransform;
    [Tooltip("This transform is used to find the height of componenets.")]
    [SerializeField] private Transform textAlign;

    [Header("Image Components")]
    [Tooltip("The text field that shows the art description.")]
    [SerializeField] private TextMeshProUGUI artDescription;
    [Tooltip("The text field that shows the art description.")]
    [SerializeField] private TextMeshProUGUI scrollingTextbox;
    [Tooltip("The transform of the button .")]
    [SerializeField] private Transform descriptionButtonTransform;
    [Tooltip("The transform of the plaque container.")]
    [SerializeField] private Transform plaqueContainerTransform;
    [Tooltip("The scrolling textbox container.")]
    [SerializeField] private GameObject scrollingContainer;

    private float defaultFontSize;

    /* Credit to @zbarlow FrameController for some of the base setup functionality*/
    public void Awake()
    {
        defaultFontSize = artDescription.fontSize;
        SettingsManager.m_TextSizeChanged.AddListener(ChangeTextSize);
        ChangeTextSize();
        UpdateTextLocation();
    }

    public void ChangeTextSize()
    {
        artDescription.fontSize = defaultFontSize * GlobalSettings.TEXT_SIZE_MULTIPLIER;
    }

    public void UpdateTextLocation()
    {
        // rotate properly
        transform.eulerAngles = new Vector3(0, frameTransform.transform.eulerAngles.y + 180f, 0);

        // align plaque
        plaqueContainerTransform.position = textAlign.position + textAlign.right * 0.05f;

        // align button
        float width = artDescription.rectTransform.rect.width;
        float margin = 0.15f;
        descriptionButtonTransform.position = textAlign.position + textAlign.right * (width + margin);

        // align scrolling textbox
        scrollingContainer.transform.position = descriptionButtonTransform.position + descriptionButtonTransform.right * -0.5f;
        scrollingContainer.transform.position = scrollingContainer.transform.position + descriptionButtonTransform.forward * 0.05f;
    }

    // TODO: This is probably a performance sink but otherwise the button wasn't lining up
    public void Update()
    {
        UpdateTextLocation();
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

        artDescription.text = organizedText;

        string scrollingText = "<b>Artist Major: </b>" + capitalizeFirstLetterOfEachWord(data.artistMajor) + 
                               "\n\n<b>About the Work: </b>" + data.artworkDescription;
        scrollingTextbox.text = scrollingText;

        UpdateTextLocation();
    }

    public void ToggleScrollingDescription()
    {
        scrollingContainer.SetActive(!scrollingContainer.activeSelf);
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
