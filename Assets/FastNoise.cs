using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/Noise Generator")]
public class FastNoise : ScriptableObject
{
  [Header("UI Settings")]
  [SerializeField] bool invert = false;

  [SerializeField] bool visualizeWarp = false;

  [Header("General")]
  [SerializeField] int seed = 1337;

  [SerializeField] float frequency = 0.01f;
  [SerializeField] FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
  [SerializeField] FastNoiseLite.RotationType3D rotationType3D = FastNoiseLite.RotationType3D.None;
  [SerializeField] FastNoiseLite.TransformType3D transformType3D = FastNoiseLite.TransformType3D.DefaultOpenSimplex2;

  [Header("Fractals")]
  [SerializeField] FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None;

  [SerializeField] int octaves = 3;
  [SerializeField] float lacunarity = 2.0f;
  [SerializeField] float gain = 0.5f;
  [SerializeField] float weightedStrength = 0.0f;
  [SerializeField] float pingPongStength = 2.0f;
  [SerializeField] float fractalBounding = 1 / 1.75f;

  [Header("Cellular")]
  [SerializeField]
  FastNoiseLite.CellularDistanceFunction cellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.EuclideanSq;

  [SerializeField] FastNoiseLite.CellularReturnType cellularReturnType = FastNoiseLite.CellularReturnType.Distance;
  [SerializeField] float cellularJitterModifier = 1.0f;

  [Header("Domain Warp Visualization")]
  [SerializeField] FastNoiseLite.DomainWarpType domainWarpType = FastNoiseLite.DomainWarpType.None;

  [SerializeField] FastNoiseLite.RotationType3D domainWarpRotationType = FastNoiseLite.RotationType3D.None;

  [SerializeField]
  FastNoiseLite.TransformType3D warpTransformType3D = FastNoiseLite.TransformType3D.DefaultOpenSimplex2;

  [SerializeField] float domainWarpAmp = 1.0f;
  [SerializeField] float domainWarpFrequency = 3;

  [Header("Domain Warp Fractals")]
  [SerializeField] FastNoiseLite.FractalType domainWarpFractalType = FastNoiseLite.FractalType.None;

  [SerializeField] int domainWarpOctaves = 3;
  [SerializeField] float domainWarpLacunarity = 2.0f;
  [SerializeField] float domainWarpGain = 0.5f;

  [Header("Preview Settings")]
  [SerializeField] int previewResolution = 256;

  Color32[] ImageData = null;
  [NonSerialized] Texture2D previewTexture = null;
  [NonSerialized] Stopwatch sw = null;

  public void GeneratePreview()
  {
    // Create noise generators
    var genNoise = new FastNoiseLite();
    var warpNoise = new FastNoiseLite();

    int w = previewResolution;
    int h = previewResolution;

    if (w <= 0 || h <= 0) return;

    genNoise.SetNoiseType(noiseType);
    genNoise.SetRotationType3D(rotationType3D);
    genNoise.SetSeed(seed);
    genNoise.SetFrequency(frequency);
    genNoise.SetFractalType(fractalType);
    genNoise.SetFractalOctaves(octaves);
    genNoise.SetFractalLacunarity(lacunarity);
    genNoise.SetFractalGain(gain);
    genNoise.SetFractalWeightedStrength(weightedStrength);
    genNoise.SetFractalPingPongStrength(pingPongStength);

    genNoise.SetCellularDistanceFunction(cellularDistanceFunction);
    genNoise.SetCellularReturnType(cellularReturnType);
    genNoise.SetCellularJitter(cellularJitterModifier);

    warpNoise.SetSeed(seed);
    warpNoise.SetDomainWarpType(domainWarpType);
    warpNoise.SetRotationType3D(domainWarpRotationType);
    warpNoise.SetDomainWarpAmp(domainWarpAmp);
    warpNoise.SetFrequency(domainWarpFrequency);
    warpNoise.SetFractalType(domainWarpFractalType);
    warpNoise.SetFractalOctaves(domainWarpOctaves);
    warpNoise.SetFractalLacunarity(domainWarpLacunarity);
    warpNoise.SetFractalGain(domainWarpGain);

    if (ImageData == null || ImageData.Length != w * h || previewTexture == null)
    {
      ImageData = new Color32[w * h];
      previewTexture = new Texture2D(w, h, TextureFormat.ARGB32, false);
    }


    float noise;
    float minN = float.MaxValue;
    float maxN = float.MinValue;
    float avg = 0;

    // Timer
    sw = new Stopwatch();

    int index = 0;
    if (!visualizeWarp)
    {
      var noiseValues = new float[w * h];

      sw.Start();
      for (var y = h / -2; y < h / 2; y++)
      {
        for (var x = w / -2; x < w / 2; x++)
        {
          float xf = x;
          float yf = y;
          // float zf = zPos;

          // if (is3D)
          // {
          // if (warp)
          //   warpNoise.DomainWarp(ref xf, ref yf, ref zf);

          //noise = genNoise.GetNoise(xf, yf, zf);
          // }
          // else
          // {
          if (domainWarpType != FastNoiseLite.DomainWarpType.None)
            warpNoise.DomainWarp(ref xf, ref yf);
          noise = genNoise.GetNoise(xf, yf);
          // }

          avg += noise;
          maxN = Math.Max(maxN, noise);
          minN = Math.Min(minN, noise);
          noiseValues[index++] = noise;
        }
      }

      sw.Stop();

      avg /= index - 1;
      float scale = 255 / (maxN - minN);

      for (var i = 0; i < noiseValues.Length; i++)
      {
        var value = (byte) Mathf.Round(Mathf.Clamp((noiseValues[i] - minN) * scale, 0, 255));
        ImageData[i].r = invert ? (byte) (255 - value) : value;
        ImageData[i].g = invert ? (byte) (255 - value) : value;
        ImageData[i].b = invert ? (byte) (255 - value) : value;
      }
    }
    else
    {
      var noiseValues = new float[w * h * 3];

      sw.Start();
      for (var y = -h / 2; y < h / 2; y++)
      {
        for (var x = -w / 2; x < w / 2; x++)
        {
          float xf = x;
          float yf = y;
          // float zf = zPos;

          // if (get3d)
          //   warpNoise.DomainWarp(ref xf, ref yf, ref zf);
          // else
          warpNoise.DomainWarp(ref xf, ref yf);

          xf -= x;
          yf -= y;
          // zf -= zPos;

          avg += (float) (xf + yf);
          maxN = Math.Max(maxN, (float) Math.Max(xf, yf));
          minN = Math.Min(minN, (float) Math.Min(xf, yf));

          noiseValues[index++] = (float) xf;
          noiseValues[index++] = (float) yf;

          // if (get3d)
          // {
          //   avg += (float) zf;
          //   maxN = Math.Max(maxN, (float) zf);
          //   minN = Math.Min(minN, (float) zf);
          //   noiseValues[index++] = (float) zf;
          // }
        }
      }

      sw.Stop();

      // if (get3d)
      //   avg /= (index - 1) * 3;
      // else 
      avg /= (index - 1) * 2;

      index = 0;
      // float scale = 1 / (maxN - minN);

      for (var i = 0; i < ImageData.Length; i++)
      {
        // Color color = new Color();

        // if (get3d)
        // {
        //   color.R = (noiseValues[index++] - minN) * scale;
        //   color.G = (noiseValues[index++] - minN) * scale;
        //   color.B = (noiseValues[index++] - minN) * scale;
        // }
        // else
        // {
        var vx = (noiseValues[index++] - minN) / (maxN - minN) - 0.5f;
        var vy = (noiseValues[index++] - minN) / (maxN - minN) - 0.5f;

        var hue = (Mathf.Atan2(vy, vx) * (180 / Mathf.PI) + 180) / 360.0f;
        var saturation = 0.9f;
        var value = Math.Min(1.0f, Mathf.Sqrt(vx * vx + vy * vy) * 2);

        var color = Color.HSVToRGB(hue, saturation, value);
        if (invert)
        {
          color.r = 1.0f - color.r;
          color.g = 1.0f - color.g;
          color.b = 1.0f - color.b;
          color.a = 1.0f - color.a;
        }
        // }

        // if (Invert.Checked == true)
        // {
        //   color.Invert();
        // }

        ImageData[i] = color;
      }
    }


    for (var col = 0; col < w; col++)
    {
      for (var row = 0; row < w / 2; row++)
      {
        var col1 = ImageData[row * w + col];
        var col2 = ImageData[(w - row - 1) * w + col];
        ImageData[row * w + col] = col2;
        ImageData[(w - row - 1) * w + col] = col1;
      }
    }

    previewTexture.SetPixels32(0, 0, w, h, ImageData);
    previewTexture.Apply();
  }

#if UNITY_EDITOR

  [CustomEditor(typeof(FastNoise))]
  public class FastNoiseEditor : Editor
  {
    public override void OnInspectorGUI()
    {
      var fn = (FastNoise) target;
      var desiredWidth = 512;
      if (Screen.width < desiredWidth)
        desiredWidth = Screen.width;
      GUILayout.Space(desiredWidth + 8);
      if (GUILayout.Button("Generate") || fn.previewTexture == null)
        fn.GeneratePreview();

      if (fn.previewTexture != null)
      {
        var xPos = Screen.width / 2 - (desiredWidth / 2);
        EditorGUI.DrawPreviewTexture(new Rect(xPos, 10, desiredWidth, desiredWidth), fn.previewTexture);
      }
        
      if (fn.sw != null)
        GUILayout.Label($"Generation Time (ms): {fn.sw.ElapsedMilliseconds}ms");
      base.OnInspectorGUI();
    }
  }

#endif
}
