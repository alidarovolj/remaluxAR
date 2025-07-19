using UnityEngine;
using Unity.Barracuda;

public static class SegmentationPostProcessing
{
      // This function takes the raw output from the neural network and updates a texture with the colored segmentation mask.
      public static void ProcessOutput(Tensor outputTensor, Texture2D texture, int classIndexToPaint, Color paintColor)
      {
            var modelHeight = outputTensor.height;
            var modelWidth = outputTensor.width;

            var textureHeight = texture.height;
            var textureWidth = texture.width;

            var pixels = new Color32[textureWidth * textureHeight];

            // The output tensor from the model has a shape of [1, height, width, num_classes].
            // For each pixel in the final texture, sample from the model output
            for (int texY = 0; texY < textureHeight; texY++)
            {
                  for (int texX = 0; texX < textureWidth; texX++)
                  {
                        // Map texture coordinates to model coordinates with bilinear sampling
                        float modelX = (float)texX / textureWidth * modelWidth;
                        float modelY = (float)texY / textureHeight * modelHeight;

                        // Get the class for this pixel using bilinear interpolation
                        int classIndex = GetClassAtPosition(outputTensor, modelX, modelY, modelWidth, modelHeight);

                        // Get the color for this class from the ColorMap
                        Color32 pixelColor = ColorMap.GetColor(classIndex);

                        // Apply selection effects
                        if (classIndexToPaint != -1 && classIndex == classIndexToPaint)
                        {
                              // Make selected class very bright and opaque
                              pixelColor.r = (byte)Mathf.Min(255, (int)(pixelColor.r * 1.5f) + 80);
                              pixelColor.g = (byte)Mathf.Min(255, (int)(pixelColor.g * 1.5f) + 80);
                              pixelColor.b = (byte)Mathf.Min(255, (int)(pixelColor.b * 1.5f) + 80);
                              pixelColor.a = 220; // Very opaque for selected class
                        }
                        else if (classIndexToPaint == -1)
                        {
                              // Show all classes with medium transparency when no specific class is selected
                              pixelColor.a = 140;
                        }
                        else
                        {
                              // Dim other classes significantly when something is selected
                              pixelColor.r = (byte)((int)pixelColor.r * 0.4f);
                              pixelColor.g = (byte)((int)pixelColor.g * 0.4f);
                              pixelColor.b = (byte)((int)pixelColor.b * 0.4f);
                              pixelColor.a = 60; // Very transparent for non-selected classes
                        }

                        pixels[texY * textureWidth + texX] = pixelColor;
                  }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
      }

      private static int GetClassAtPosition(Tensor outputTensor, float x, float y, int modelWidth, int modelHeight)
      {
            // Clamp coordinates
            x = Mathf.Clamp(x, 0, modelWidth - 1);
            y = Mathf.Clamp(y, 0, modelHeight - 1);

            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, modelWidth - 1);
            int y1 = Mathf.Min(y0 + 1, modelHeight - 1);

            // Get class indices for the four corner pixels
            int class00 = GetDominantClass(outputTensor, x0, y0);
            int class01 = GetDominantClass(outputTensor, x0, y1);
            int class10 = GetDominantClass(outputTensor, x1, y0);
            int class11 = GetDominantClass(outputTensor, x1, y1);

            // Simple majority voting for smoother transitions
            var classCount = new System.Collections.Generic.Dictionary<int, int>();
            var classes = new int[] { class00, class01, class10, class11 };

            foreach (int cls in classes)
            {
                  if (classCount.ContainsKey(cls))
                        classCount[cls]++;
                  else
                        classCount[cls] = 1;
            }

            int bestClass = class00;
            int maxCount = 0;
            foreach (var pair in classCount)
            {
                  if (pair.Value > maxCount)
                  {
                        maxCount = pair.Value;
                        bestClass = pair.Key;
                  }
            }

            return bestClass;
      }

      private static int GetDominantClass(Tensor outputTensor, int x, int y)
      {
            int classIndex = 0;
            float maxScore = float.MinValue;

            for (int c = 0; c < outputTensor.channels; c++)
            {
                  var score = outputTensor[0, y, x, c];
                  if (score > maxScore)
                  {
                        maxScore = score;
                        classIndex = c;
                  }
            }

            return classIndex;
      }
}