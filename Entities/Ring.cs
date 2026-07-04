using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HelixVortex.Entities;

public enum SliceType
{
    Safe,
    Fatal,
    Empty
}

public class Ring
{
    public float HeightY { get; set; }
    public SliceType[] Slices { get; } = new SliceType[12];
    public bool IsDestroyed { get; set; } = false;
    public int RingIndex { get; }

    public Ring(int ringIndex, float heightY, IList<SliceType> slices)
    {
        RingIndex = ringIndex;
        HeightY = heightY;
        IsDestroyed = false;

        for (int i = 0; i < 12; i++)
        {
            Slices[i] = slices[i];
        }
    }

    public List<Color> GetSliceColors(Color safeColor, Color fatalColor)
    {
        List<Color> colors = new List<Color>();
        for (int i = 0; i < 12; i++)
        {
            if (Slices[i] == SliceType.Safe)
                colors.Add(safeColor);
            else if (Slices[i] == SliceType.Fatal)
                colors.Add(fatalColor);
            else
                colors.Add(Color.Transparent);
        }
        return colors;
    }
}
