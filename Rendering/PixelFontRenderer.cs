using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HelixVortex.Rendering;

public static class PixelFontRenderer
{
    private static readonly Dictionary<char, byte[]> Glyphs = new()
    {
        { 'A', new byte[] { 0x7E, 0x11, 0x11, 0x11, 0x7E } },
        { 'B', new byte[] { 0x7F, 0x49, 0x49, 0x49, 0x36 } },
        { 'C', new byte[] { 0x3E, 0x41, 0x41, 0x41, 0x22 } },
        { 'D', new byte[] { 0x7F, 0x41, 0x41, 0x22, 0x1C } },
        { 'E', new byte[] { 0x7F, 0x49, 0x49, 0x49, 0x41 } },
        { 'F', new byte[] { 0x7F, 0x09, 0x09, 0x09, 0x01 } },
        { 'G', new byte[] { 0x3E, 0x41, 0x49, 0x49, 0x7A } },
        { 'H', new byte[] { 0x7F, 0x08, 0x08, 0x08, 0x7F } },
        { 'I', new byte[] { 0x41, 0x41, 0x7F, 0x41, 0x41 } },
        { 'J', new byte[] { 0x20, 0x40, 0x41, 0x3F, 0x01 } },
        { 'K', new byte[] { 0x7F, 0x08, 0x14, 0x22, 0x41 } },
        { 'L', new byte[] { 0x7F, 0x40, 0x40, 0x40, 0x40 } },
        { 'M', new byte[] { 0x7F, 0x02, 0x0C, 0x02, 0x7F } },
        { 'N', new byte[] { 0x7F, 0x04, 0x08, 0x10, 0x7F } },
        { 'O', new byte[] { 0x3E, 0x41, 0x41, 0x41, 0x3E } },
        { 'P', new byte[] { 0x7F, 0x09, 0x09, 0x09, 0x06 } },
        { 'Q', new byte[] { 0x3E, 0x41, 0x51, 0x21, 0x5E } },
        { 'R', new byte[] { 0x7F, 0x09, 0x19, 0x29, 0x46 } },
        { 'S', new byte[] { 0x46, 0x49, 0x49, 0x49, 0x31 } },
        { 'T', new byte[] { 0x01, 0x01, 0x7F, 0x01, 0x01 } },
        { 'U', new byte[] { 0x3F, 0x40, 0x40, 0x40, 0x3F } },
        { 'V', new byte[] { 0x1F, 0x20, 0x40, 0x20, 0x1F } },
        { 'W', new byte[] { 0x3F, 0x40, 0x38, 0x40, 0x3F } },
        { 'X', new byte[] { 0x63, 0x14, 0x08, 0x14, 0x63 } },
        { 'Y', new byte[] { 0x07, 0x08, 0x70, 0x08, 0x07 } },
        { 'Z', new byte[] { 0x61, 0x51, 0x49, 0x45, 0x43 } },
        { '0', new byte[] { 0x3E, 0x51, 0x49, 0x45, 0x3E } },
        { '1', new byte[] { 0x00, 0x42, 0x7F, 0x40, 0x00 } },
        { '2', new byte[] { 0x42, 0x61, 0x51, 0x49, 0x46 } },
        { '3', new byte[] { 0x21, 0x41, 0x45, 0x4B, 0x31 } },
        { '4', new byte[] { 0x18, 0x14, 0x12, 0x7F, 0x10 } },
        { '5', new byte[] { 0x27, 0x45, 0x45, 0x45, 0x39 } },
        { '6', new byte[] { 0x3C, 0x4A, 0x49, 0x49, 0x30 } },
        { '7', new byte[] { 0x01, 0x71, 0x09, 0x05, 0x03 } },
        { '8', new byte[] { 0x36, 0x49, 0x49, 0x49, 0x36 } },
        { '9', new byte[] { 0x06, 0x49, 0x49, 0x29, 0x1E } },
        { ' ', new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 } },
        { '-', new byte[] { 0x08, 0x08, 0x08, 0x08, 0x08 } },
        { ':', new byte[] { 0x00, 0x36, 0x36, 0x00, 0x00 } },
        { '>', new byte[] { 0x08, 0x14, 0x22, 0x41, 0x00 } },
        { '<', new byte[] { 0x00, 0x41, 0x22, 0x14, 0x08 } },
        { '+', new byte[] { 0x08, 0x08, 0x3E, 0x08, 0x08 } },
        { '/', new byte[] { 0x20, 0x10, 0x08, 0x04, 0x02 } }
    };

    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;
    private const int Spacing = 1;

    public static void DrawText(SpriteBatch spriteBatch, Texture2D pixelTexture, string text, int x, int y, int scale, Color color)
    {
        if (text == null) return;
        text = text.ToUpperInvariant();

        int currentX = x;

        foreach (char c in text)
        {
            if (Glyphs.TryGetValue(c, out var cols))
            {
                for (int col = 0; col < GlyphWidth; col++)
                {
                    byte colData = cols[col];
                    for (int row = 0; row < GlyphHeight; row++)
                    {
                        if ((colData & (1 << row)) != 0)
                        {
                            spriteBatch.Draw(
                                pixelTexture,
                                new Rectangle(currentX + col * scale, y + row * scale, scale, scale),
                                color
                            );
                        }
                    }
                }
            }
            currentX += (GlyphWidth + Spacing) * scale;
        }
    }

    public static void DrawTextCentered(SpriteBatch spriteBatch, Texture2D pixelTexture, string text, int centerX, int y, int scale, Color color)
    {
        Vector2 size = MeasureText(text, scale);
        DrawText(spriteBatch, pixelTexture, text, centerX - (int)(size.X / 2), y, scale, color);
    }

    public static Vector2 MeasureText(string text, int scale)
    {
        if (string.IsNullOrEmpty(text)) return Vector2.Zero;
        int width = text.Length * (GlyphWidth + Spacing) - Spacing;
        return new Vector2(width * scale, GlyphHeight * scale);
    }
}
