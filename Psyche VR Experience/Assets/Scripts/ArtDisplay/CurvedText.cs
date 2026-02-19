using System;
using System.Xml;
using TMPro;
using UnityEngine;

public class CurvedText : MonoBehaviour
{
    [SerializeField] private int radius = 200;
    [SerializeField] private int angle = 180;

    [SerializeField] TextMeshProUGUI text;

    public void OnValidate()
    {
        if (text != null && !string.IsNullOrEmpty(text.text))
        {
            UpdateArc();
        }
    }

    public void OnEnable()
    {
        UpdateArc();
    }

    private void UpdateArc()
    {
        if (!text) return;

        text.ForceMeshUpdate();
        var textInfo = text.textInfo;

        float charMidBaseline;
        float radiusOffset = radius;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            var charInfo = textInfo.characterInfo[i];

            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            charMidBaseline = (vertices[vertexIndex].x + vertices[vertexIndex + 2].x) / 2;

            float normalized = charMidBaseline / text.bounds.size.x;
            float angleOffset = normalized * angle;

            float radians = Mathf.Deg2Rad * angleOffset;

            Vector3 offset = new Vector3(
                Mathf.Sin(radians) * radiusOffset,
                Mathf.Cos(radians) * radiusOffset - radiusOffset,
                0
            );

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] += offset;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            // look into this occasional error when validating
            try
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            } catch (NullReferenceException e)
            {
                return;
            }
            
        }
    }
}
