using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
  [SerializeField] FastNoise noiseAsset = null;
  [SerializeField] int heightmapResolution = 513;
  [SerializeField] float terrainSize = 100;

  void Awake() => Generate();

  void Generate()
  {
    var terrainData = new TerrainData
    {
      alphamapResolution = 64,
      baseMapResolution = heightmapResolution,
      heightmapResolution =  heightmapResolution,
      size = new Vector3(terrainSize, 20, terrainSize)
    };
    var squareSize = terrainSize / heightmapResolution;
    var arr = new float[heightmapResolution,heightmapResolution];
    for(var x = 0;x < heightmapResolution;x++)
    for (var y = 0; y < heightmapResolution; y++)
      arr[y, x] = (noiseAsset.GetNoiseValue(x * squareSize, y * squareSize) + 1) / 2.0f;
    terrainData.SetHeights(0, 0, arr);
    Terrain.CreateTerrainGameObject(terrainData);
  }
}
