using UnityEngine;
using System.Collections.Generic;

public static class ColorManager
{
    private static List<Color> availableColors = new List<Color>
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    private static List<Color> usedColors = new List<Color>();

    public static Color GetUniqueColor()
    {
        if (availableColors.Count == 0)
        {
            Debug.LogWarning("No more unique colors available.");
            return Color.white; // Default color if no more unique colors are available
        }

        int index = Random.Range(0, availableColors.Count);
        Color color = availableColors[index];
        availableColors.RemoveAt(index);
        usedColors.Add(color);
        Debug.Log($"Assigned color {color}, Available colors left: {availableColors.Count}");
        return color;
    }

    public static void ResetColors()
    {
        availableColors.AddRange(usedColors);
        usedColors.Clear();
        Debug.Log("Colors reset.");
    }
}