using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class ImageBaker : MonoBehaviour
{
    public Texture2D contTex;
    public AnimationCurve continentalness;
    public Texture2D erosionTex;
    public AnimationCurve erosion;
    public Texture2D pnvTex;
    public AnimationCurve peaksAndValleys;
    public Texture2D terrainShapingTex;


    void Start()
    {
        //BakeTexture(contTex, continentalness);
        //BakeTexture(erosionTex, erosion);
        //BakeTexture(pnvTex, peaksAndValleys);

        string fileName = terrainShapingTex.name;

        Color[] colours = new Color[terrainShapingTex.width * terrainShapingTex.height];

        int pixelIndex = 0;
        for (int i = 0; i < terrainShapingTex.width * terrainShapingTex.height; i++)
        {
            colours[i] = new Color(
                continentalness.Evaluate((float)i / (terrainShapingTex.width * terrainShapingTex.height)),
                erosion.Evaluate((float)i / (terrainShapingTex.width * terrainShapingTex.height)),
                peaksAndValleys.Evaluate((float)i / (terrainShapingTex.width * terrainShapingTex.height)), 0);

            pixelIndex++;
        }
        terrainShapingTex.SetPixels(0, 0, terrainShapingTex.width, terrainShapingTex.height, colours);
        terrainShapingTex.Apply();

        SaveImage(terrainShapingTex, fileName);
    }

    private void BakeTexture(Texture2D tex, AnimationCurve aC)
    {
        string fileName = tex.name;

        Color[] colours = new Color[tex.width];

        int pixelIndex = 0;
        for (int i = 0; i < tex.width; i++)
        {
            colours[i] = new Color(aC.Evaluate((float)i / tex.width), 0, 0, 0);

            pixelIndex++;
        }

        tex.SetPixels(0, 0, tex.width, 1, colours);
        tex.Apply();

        SaveImage(tex, fileName);
    }

    private void SaveImage(Texture2D t, string imageName)
    {
        string directoryPath = System.IO.Path.Combine(Application.dataPath, "Materials/Textures");

        // Construct the full path to the image file
        string fullPath = System.IO.Path.Combine(directoryPath, $"{imageName}.png");

        // Ensure the directory exists
        System.IO.Directory.CreateDirectory(directoryPath);

        // Encode the Texture2D to a PNG and save it to the specified path
        byte[] bytes = t.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, bytes);
    }
}
