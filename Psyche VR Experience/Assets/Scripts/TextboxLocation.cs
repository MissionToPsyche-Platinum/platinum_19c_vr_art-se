using UnityEngine;

public class TextboxLocation : MonoBehaviour
{
    public GameObject frameBottom;
    public GameObject textbox;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textbox.transform.position = new Vector3(frameBottom.transform.position.x, frameBottom.transform.position.y - (float)0.25, frameBottom.transform.position.z + (float)0.01);
        textbox.transform.eulerAngles = new Vector3(0, frameBottom.transform.eulerAngles.y + (float)180.0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        textbox.transform.position = new Vector3(frameBottom.transform.position.x, frameBottom.transform.position.y - (float)0.25, frameBottom.transform.position.z + (float)0.01);
        textbox.transform.eulerAngles = new Vector3(0, frameBottom.transform.eulerAngles.y + (float)180.0, 0);
    }
}
