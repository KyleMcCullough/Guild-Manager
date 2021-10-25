using UnityEngine;

public static class NoiseCreator
{  
  /// scale : The scale of the "perlin noise" view
  /// heightMultiplier : The maximum height of the terrain
  /// octaves : Number of iterations (the more there is, the more detailed the terrain will be)
  /// persistance : The higher it is, the rougher the terrain will be (this value should be between 0 and 1 excluded)
  /// lacunarity : The higher it is, the more "feature" the terrain will have (should be strictly positive)
  public static float GetNoiseAt(int x, int y, int seed, float scale = 1, float heightMultiplier = 1, int octaves = 5, float persistance = .3f, float lacunarity = .032f)
  {
      float PerlinValue = 0f;
      float amplitude = 1f;
      float frequency = .032f;

      for(int i = 0; i < octaves; i++)
      {
      	 // Get the perlin value at that octave and add it to the sum
		 PerlinValue += Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency) * amplitude;
         
         // Decrease the amplitude and the frequency
         amplitude *= persistance;
         frequency *= lacunarity;
      }
      
      // Return the noise value
      return PerlinValue * heightMultiplier;
  }
}