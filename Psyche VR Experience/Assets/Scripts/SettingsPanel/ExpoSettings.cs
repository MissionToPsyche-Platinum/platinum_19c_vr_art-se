using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ExpoSettings : MonoBehaviour
{
    public static int ART_PIECE_COUNT
    {
        get { return numArtPieces; }
        set {numArtPieces = value; }
    }

    public static bool REGENERATE_MUSEUM
    {
        get { return regenerateMuseum; }
        set {regenerateMuseum = value; }        
    }

    public static int MUSEUM_TOUR_DURATION
    {
        get {return expoDuration; }
        set {expoDuration = value; }
    }

    public void ReadTourDurationInput (string inputString)
    {
          if(int.TryParse(inputString, out expoDuration))
        {
            Debug.Log("Successful Conversion: " + expoDuration);
        } 
        else
        {
            Debug.LogError("Invalid Input: not a valid float! " + inputString);    
        }    
    }

    public void ReadArtworkCountInput (string inputString)
    {
        if(int.TryParse(inputString, out numArtPieces))
        {
            Debug.Log("Successful Conversion: " + numArtPieces);
        } 
        else
        {
            Debug.LogError("Invalid Input: not a valid integer! " + inputString);    
        }   
    }

    //There is definetly a better way to do this, but in the temporary intrest of time...
    public void IncrementArtByOne()
    {
        if (numArtPieces < 150)
        {
            numArtPieces += 1;
        }
        else
        {
            numArtPieces = 150;
        }
    }

    public void DecrementArtByOne()
    {
        if (numArtPieces > 0)
        {
            numArtPieces -= 1;
        }
        else
        {
            numArtPieces = 0;
        }
    }

    public void IncremementArtByTen()
    {
        if (numArtPieces < 140)
        {
            numArtPieces += 10;
        }
        else
        {
            numArtPieces = 150;
        }
    }

    public void DecrementArtByTen()
    {
        if (numArtPieces > 10)
        {
            numArtPieces -= 10;
        }
        else
        {
            numArtPieces = 0;
        }
    }

    public void IncrementTimeByOne()
    {
        if (expoDuration < 3600)
        {
            expoDuration += 1;
        }
        else
        {
            expoDuration = 3600;
        }
    }

    public void DecrementTimeByOne()
    {
        if (expoDuration > 0)
        {
            expoDuration -= 1;
        }
        else
        {
            expoDuration = 0;
        }
    }

    public void IncremementTimeByTen()
    {
        if (expoDuration < 3600)
        {
            expoDuration += 10;
        }
        else
        {
            expoDuration = 150;
        }
    }

    public void DecrementTimeByTen()
    {
        if (expoDuration > 10)
        {
            expoDuration -= 10;
        }
        else
        {
            expoDuration = 0;
        }
    }

    void Update()
    {
        artworkInputField.text = numArtPieces.ToString();
        expoDurationInputField.text = expoDuration.ToString();
        freeArtworkInputField.text = numArtPieces.ToString();
        freeDurationInputField.text = expoDuration.ToString();
    }

    public TMP_InputField artworkInputField;
    public TMP_InputField expoDurationInputField;
    public TMP_InputField freeArtworkInputField;
    public TMP_InputField freeDurationInputField;
    static int numArtPieces = 30;
    static bool regenerateMuseum = true;
    static int expoDuration = 300;
    
}