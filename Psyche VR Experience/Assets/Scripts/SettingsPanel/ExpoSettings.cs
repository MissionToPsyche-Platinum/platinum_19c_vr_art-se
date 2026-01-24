using Unity.VisualScripting;
using UnityEngine;

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

    public static float MUSEUM_TOUR_DURATION
    {
        get {return expoDuration; }
        set {expoDuration = value; }
    }

    public void ReadTourDurationInput (string inputString)
    {
          if(float.TryParse(inputString, out expoDuration))
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

    static int numArtPieces = 30;
    static bool regenerateMuseum = true;
    static float expoDuration = 300.0f;
    
}