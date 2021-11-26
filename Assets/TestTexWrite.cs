using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTexWrite : MonoBehaviour
{
    public Texture2D saveTexture;

    Color32[] texturePixels;

    private Color32 reserved;

    private void SaveMetadata()
    {
        texturePixels[0] = new Color32((byte) 'V', (byte) 'O', (byte) 'Y', (byte) 255);
        texturePixels[1] = new Color32((byte) 'A', (byte) 'G', (byte) 'E', (byte) 255);
        texturePixels[2] = new Color32((byte) 'B', (byte) 'U', (byte) 'I', (byte) 255);
        texturePixels[3] = new Color32((byte)1,(byte)0,(byte)0,(byte)255);
        for (int i = 4; i < 2048*256; i++)
        {
            texturePixels[i] = reserved;
        }
        saveTexture.SetPixels32(texturePixels);
        saveTexture.Apply();
        Debug.Log("Metadata saved");
        
    }

    // Start is called before the first frame update
    void Start()
    {
        texturePixels = new Color32[2048*1024];
        reserved = new Color32((byte) 255,(byte) 255,(byte) 255,(byte) 255);
        SaveMetadata();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
