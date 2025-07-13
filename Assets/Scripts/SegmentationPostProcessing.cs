using UnityEngine;
using Unity.Barracuda;

public static class SegmentationPostProcessing
{
      // This function takes the raw output from the neural network and updates a texture with the colored segmentation mask.
      public static void ProcessOutput(Tensor outputTensor, Texture2D texture, int classIndexToPaint, Color paintColor)
      {
            var height = outputTensor.height;
            var width = outputTensor.width;
            var pixels = new Color32[width * height];

            // The output tensor from DeepLabV3 has a shape of [1, height, width, num_classes].
            // For each pixel (y, x), we need to find which class has the highest score.
            for (int y = 0; y < height; y++)
            {
                  for (int x = 0; x < width; x++)
                  {
                        int classIndex = 0;
                        float maxScore = float.MinValue;

                        // Iterate through all the classes to find the one with the highest score for the current pixel.
                        for (int c = 0; c < outputTensor.channels; c++)
                        {
                              var score = outputTensor[0, y, x, c];
                              if (score > maxScore)
                              {
                                    maxScore = score;
                                    classIndex = c;
                              }
                        }

                        // If the current pixel's class is the one we want to paint, use the paint color with transparency.
                        // Otherwise, make the pixel completely transparent to show the camera feed.
                        if (classIndex == classIndexToPaint)
                        {
                              Color finalColor = paintColor;
                              finalColor.a = 0.7f; // Set alpha to 70% for a tinting effect
                              pixels[y * width + x] = finalColor;
                        }
                        else
                        {
                              pixels[y * width + x] = Color.clear;
                        }
                  }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
      }
}