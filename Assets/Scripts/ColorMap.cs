using UnityEngine;

public static class ColorMap
{
      // ADE20K dataset colors for TopFormer semantic segmentation
      private static readonly Color[] colorMap = new Color[]
      {
        new Color(0.9f, 0.9f, 0.9f),  // 0 - Wall (Light Gray)
        new Color(0.8f, 0.6f, 0.4f),  // 1 - Building (Brown)
        new Color(0.5f, 0.8f, 1.0f),  // 2 - Sky (Light Blue)
        new Color(0.4f, 0.2f, 0.1f),  // 3 - Floor (Dark Brown)
        new Color(0.2f, 0.6f, 0.2f),  // 4 - Tree (Green)
        new Color(1.0f, 1.0f, 0.9f),  // 5 - Ceiling (Off White)
        new Color(0.3f, 0.3f, 0.3f),  // 6 - Road (Dark Gray)
        new Color(0.9f, 0.8f, 0.7f),  // 7 - Bed (Beige)
        new Color(0.7f, 0.9f, 1.0f),  // 8 - Window (Light Cyan)
        new Color(0.3f, 0.8f, 0.3f),  // 9 - Grass (Bright Green)
        new Color(0.8f, 0.6f, 0.4f),  // 10 - Cabinet (Wood Brown)
        new Color(0.6f, 0.6f, 0.6f),  // 11 - Sidewalk (Gray)
        new Color(1.0f, 0.8f, 0.6f),  // 12 - Person (Skin Tone)
        new Color(0.5f, 0.3f, 0.1f),  // 13 - Earth (Dark Brown)
        new Color(0.7f, 0.5f, 0.3f),  // 14 - Door (Wood Brown)
        new Color(0.9f, 0.7f, 0.4f),  // 15 - Table (Light Wood)
        new Color(0.4f, 0.3f, 0.2f),  // 16 - Mountain (Dark Brown)
        new Color(0.4f, 0.7f, 0.3f),  // 17 - Plant (Plant Green)
        new Color(0.8f, 0.2f, 0.2f),  // 18 - Curtain (Red)
        new Color(0.6f, 0.4f, 0.2f),  // 19 - Chair (Brown)
        new Color(0.8f, 0.0f, 0.0f),  // 20 - Car (Red)
        new Color(0.0f, 0.4f, 0.8f),  // 21 - Water (Blue)
        new Color(0.9f, 0.9f, 0.0f),  // 22 - Painting (Yellow)
        new Color(0.2f, 0.4f, 0.8f),  // 23 - Sofa (Blue)
        new Color(0.7f, 0.5f, 0.3f),  // 24 - Shelf (Wood)
        new Color(0.8f, 0.6f, 0.4f),  // 25 - House (Tan)
        new Color(0.0f, 0.3f, 0.6f),  // 26 - Sea (Deep Blue)
        new Color(0.9f, 0.9f, 0.9f),  // 27 - Mirror (Silver)
        new Color(0.6f, 0.2f, 0.2f),  // 28 - Rug (Dark Red)
        new Color(0.5f, 0.7f, 0.3f),  // 29 - Field (Light Green)
        new Color(0.4f, 0.6f, 0.8f),  // 30 - Armchair (Light Blue)
        new Color(0.7f, 0.3f, 0.1f),  // 31 - Seat (Orange Brown)
        new Color(0.5f, 0.4f, 0.2f),  // 32 - Fence (Brown)
        new Color(0.8f, 0.6f, 0.3f),  // 33 - Desk (Light Brown)
        new Color(0.4f, 0.4f, 0.4f),  // 34 - Rock (Gray)
        new Color(0.6f, 0.3f, 0.1f),  // 35 - Wardrobe (Dark Brown)
        new Color(1.0f, 1.0f, 0.8f),  // 36 - Lamp (Light Yellow)
        new Color(0.9f, 0.9f, 1.0f),  // 37 - Bathtub (White)
        new Color(0.5f, 0.5f, 0.5f),  // 38 - Railing (Gray)
        new Color(0.8f, 0.4f, 0.6f),  // 39 - Cushion (Pink)
        new Color(0.3f, 0.3f, 0.3f),  // 40 - Base (Dark Gray)
        new Color(0.7f, 0.5f, 0.2f),  // 41 - Box (Cardboard)
        new Color(0.8f, 0.8f, 0.8f),  // 42 - Column (Light Gray)
        new Color(0.9f, 0.7f, 0.0f),  // 43 - Signboard (Yellow)
        new Color(0.6f, 0.4f, 0.3f),  // 44 - Chest (Wood)
        new Color(0.4f, 0.4f, 0.4f),  // 45 - Counter (Gray)
        new Color(0.9f, 0.8f, 0.6f),  // 46 - Sand (Tan)
        new Color(0.9f, 0.9f, 0.9f),  // 47 - Sink (White)
        new Color(0.5f, 0.5f, 0.6f),  // 48 - Skyscraper (Blue Gray)
        new Color(0.6f, 0.3f, 0.1f),  // 49 - Fireplace (Brick Red)
        new Color(0.9f, 0.9f, 0.9f)   // 50 - Refrigerator (White)
      };

      // ADE20K dataset class names for TopFormer
      public static readonly string[] classNames = new string[]
      {
        "Wall",          // 0 - Стена
        "Building",      // 1 - Здание
        "Sky",           // 2 - Небо
        "Floor",         // 3 - Пол
        "Tree",          // 4 - Дерево
        "Ceiling",       // 5 - Потолок
        "Road",          // 6 - Дорога
        "Bed",           // 7 - Кровать
        "Window",        // 8 - Окно
        "Grass",         // 9 - Трава
        "Cabinet",       // 10 - Шкаф
        "Sidewalk",      // 11 - Тротуар
        "Person",        // 12 - Человек
        "Earth",         // 13 - Земля
        "Door",          // 14 - Дверь
        "Table",         // 15 - Стол
        "Mountain",      // 16 - Гора
        "Plant",         // 17 - Растение
        "Curtain",       // 18 - Занавеска
        "Chair",         // 19 - Стул
        "Car",           // 20 - Автомобиль
        "Water",         // 21 - Вода
        "Painting",      // 22 - Картина
        "Sofa",          // 23 - Диван
        "Shelf",         // 24 - Полка
        "House",         // 25 - Дом
        "Sea",           // 26 - Море
        "Mirror",        // 27 - Зеркало
        "Rug",           // 28 - Ковер
        "Field",         // 29 - Поле
        "Armchair",      // 30 - Кресло
        "Seat",          // 31 - Сиденье
        "Fence",         // 32 - Забор
        "Desk",          // 33 - Письменный стол
        "Rock",          // 34 - Камень
        "Wardrobe",      // 35 - Гардероб
        "Lamp",          // 36 - Лампа
        "Bathtub",       // 37 - Ванна
        "Railing",       // 38 - Перила
        "Cushion",       // 39 - Подушка
        "Base",          // 40 - Основание
        "Box",           // 41 - Коробка
        "Column",        // 42 - Колонна
        "Signboard",     // 43 - Вывеска
        "Chest",         // 44 - Комод
        "Counter",       // 45 - Стойка
        "Sand",          // 46 - Песок
        "Sink",          // 47 - Раковина
        "Skyscraper",    // 48 - Небоскреб
        "Fireplace",     // 49 - Камин
        "Refrigerator"   // 50 - Холодильник
      };

      public static Color32 GetColor(int classIndex)
      {
            if (classIndex >= 0 && classIndex < colorMap.Length)
            {
                  return colorMap[classIndex];
            }
            // If class index is higher than available colors, generate a pseudo-random color
            return GenerateColorFromIndex(classIndex);
      }

      private static Color32 GenerateColorFromIndex(int index)
      {
            // Generate a deterministic but varied color based on the index
            float hue = (index * 137.508f) % 360f / 360f; // Golden angle approximation for good distribution
            float saturation = 0.7f + (index % 3) * 0.1f; // Vary saturation
            float value = 0.8f + (index % 2) * 0.2f; // Vary brightness

            Color color = Color.HSVToRGB(hue, saturation, value);
            return new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), 120);
      }
}