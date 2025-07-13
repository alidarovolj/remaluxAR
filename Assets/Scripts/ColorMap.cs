using UnityEngine;

public static class ColorMap
{
      // A pre-generated list of 34 visually distinct colors.
      private static readonly Color[] colorMap = new Color[]
      {
        new Color(0, 0, 0), new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.9f, 0.2f),
        new Color(0.2f, 0.2f, 0.9f), new Color(0.9f, 0.9f, 0.2f), new Color(0.9f, 0.2f, 0.9f),
        new Color(0.2f, 0.9f, 0.9f), new Color(0.7f, 0.5f, 0.2f), new Color(0.9f, 0.6f, 0.4f),
        new Color(0.4f, 0.9f, 0.6f), new Color(0.6f, 0.4f, 0.9f), new Color(0.9f, 0.8f, 0.6f),
        new Color(0.6f, 0.9f, 0.8f), new Color(0.8f, 0.6f, 0.9f), new Color(1.0f, 0.5f, 0.0f),
        new Color(0.0f, 1.0f, 0.5f), new Color(0.5f, 0.0f, 1.0f), new Color(1.0f, 0.0f, 0.5f),
        new Color(0.5f, 1.0f, 0.0f), new Color(0.0f, 0.5f, 1.0f), new Color(0.8f, 0.8f, 0.8f),
        new Color(0.5f, 0.5f, 0.5f), new Color(0.9f, 0.1f, 0.5f), new Color(0.1f, 0.9f, 0.5f),
        new Color(0.5f, 0.1f, 0.9f), new Color(0.9f, 0.5f, 0.1f), new Color(0.1f, 0.5f, 0.9f),
        new Color(0.5f, 0.9f, 0.1f), new Color(0.3f, 0.7f, 0.4f), new Color(0.7f, 0.3f, 0.4f),
        new Color(0.4f, 0.3f, 0.7f), new Color(0.7f, 0.4f, 0.3f), new Color(0.3f, 0.4f, 0.7f),
        new Color(0.4f, 0.7f, 0.3f)
      };

      // Placeholder class names, as we don't know the exact classes for this model.
      public static readonly string[] classNames = new string[]
      {
        "Class 0", "Class 1", "Class 2", "Class 3", "Class 4", "Class 5", "Class 6",
        "Class 7", "Class 8", "Class 9", "Class 10", "Class 11", "Class 12", "Class 13",
        "Class 14", "Class 15", "Class 16", "Class 17", "Class 18", "Class 19", "Class 20",
        "Class 21", "Class 22", "Class 23", "Class 24", "Class 25", "Class 26", "Class 27",
        "Class 28", "Class 29", "Class 30", "Class 31", "Class 32", "Class 33"
      };

      public static Color32 GetColor(int classIndex)
      {
            if (classIndex >= 0 && classIndex < colorMap.Length)
            {
                  return colorMap[classIndex];
            }
            return Color.black; // Return black for any unknown class
      }
}