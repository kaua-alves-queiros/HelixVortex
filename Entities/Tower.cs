using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HelixVortex.Rendering;

namespace HelixVortex.Entities;

public class Tower
{
    public float RotationY { get; set; }
    public List<Ring> Rings { get; } = new();

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Random _random = new();

    // Shared meshes
    private VertexPositionNormalColor[] _sliceVertices;
    private short[] _sliceIndices;
    private VertexPositionNormalColor[] _poleVertices;
    private short[] _poleIndices;

    // Dimensions
    public const float RingSpacing = 4.5f;
    public const float InnerRadius = 1.1f;
    public const float OuterRadius = 2.5f;
    public const float RingThickness = 0.25f;
    public const float PoleRadius = 0.95f;

    // Controls
    private MouseState _prevMouseState;
    private float _keyboardRotationSpeed = 3.5f; // Rads per second
    private float _mouseSensitivity = 0.007f;   // Rads per pixel

    public Tower(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        RotationY = 0f;

        // Build shared meshes
        // Single slice representing a 30 degree arc (from 0 to MathHelper.Pi / 6)
        MeshBuilder.CreateSlice(
            InnerRadius, OuterRadius, RingThickness,
            0f, MathHelper.Pi / 6f, // 0 to 30 degrees
            Color.White,
            out _sliceVertices, out _sliceIndices
        );

        // Cylinder for central pole (height will be adjusted on level load)
        MeshBuilder.CreateCylinder(PoleRadius, 1f, 24, Color.White, out _poleVertices, out _poleIndices);

        _prevMouseState = Mouse.GetState();
    }

    public void GenerateLevel(int level)
    {
        Rings.Clear();
        RotationY = 0f;

        // Level config
        int ringCount = 5 + (level - 1) * 5; // Level 1: 5 rings, Level 2: 10, Level 3: 15, etc.
        float currentHeight = 0f;

        for (int r = 0; r < ringCount; r++)
        {
            SliceType[] slices = new SliceType[12];

            if (r == 0)
            {
                // Top Ring: Safe start at slice index 10 (front), hole at slice index 3-4 (back)
                for (int s = 0; s < 12; s++) slices[s] = SliceType.Safe;
                slices[3] = SliceType.Empty;
                slices[4] = SliceType.Empty;
            }
            else
            {
                // Standard Ring Generation
                int holeWidth = 1;
                int fatalCount = 0;

                if (level == 1)
                {
                    holeWidth = _random.Next(2, 4); // 2 to 3 empty slices
                    fatalCount = 0;
                }
                else if (level == 2)
                {
                    holeWidth = 2;
                    fatalCount = _random.Next(1, 3); // 1 to 2 fatals
                }
                else // level >= 3
                {
                    holeWidth = 1;
                    fatalCount = _random.Next(2, 5); // 2 to 4 fatals
                }

                // Fill with Safe initially
                for (int s = 0; s < 12; s++) slices[s] = SliceType.Safe;

                // Create hole
                int holeStart = _random.Next(12);
                for (int w = 0; w < holeWidth; w++)
                {
                    slices[(holeStart + w) % 12] = SliceType.Empty;
                }

                // Place fatals (avoiding holes)
                int fatalsPlaced = 0;
                int attempts = 0;
                while (fatalsPlaced < fatalCount && attempts < 50)
                {
                    attempts++;
                    int idx = _random.Next(12);
                    if (slices[idx] == SliceType.Safe)
                    {
                        slices[idx] = SliceType.Fatal;
                        fatalsPlaced++;
                    }
                }
            }

            Rings.Add(new Ring(r, currentHeight, slices));
            currentHeight -= RingSpacing;
        }

        // Add final victory platform (Green, complete ring)
        SliceType[] victorySlices = new SliceType[12];
        for (int s = 0; s < 12; s++) victorySlices[s] = SliceType.Safe;
        Rings.Add(new Ring(ringCount, currentHeight, victorySlices));

        // Rebuild pole with proper height for this level
        float totalHeight = Math.Abs(currentHeight) + 15f;
        float poleCenterY = currentHeight / 2f;
        MeshBuilder.CreateCylinder(PoleRadius, totalHeight, 24, Color.White, out _poleVertices, out _poleIndices);
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();

        // 1. Keyboard rotation
        if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A))
        {
            RotationY += _keyboardRotationSpeed * dt;
        }
        if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
        {
            RotationY -= _keyboardRotationSpeed * dt;
        }

        // 2. Mouse drag rotation
        if (mouse.LeftButton == ButtonState.Pressed)
        {
            float dx = mouse.X - _prevMouseState.X;
            RotationY -= dx * _mouseSensitivity;
        }

        _prevMouseState = mouse;

        // Keep RotationY within [0, TwoPi] to prevent overflow and keep trig clean
        RotationY = (RotationY % MathHelper.TwoPi + MathHelper.TwoPi) % MathHelper.TwoPi;
    }

    public Ring CheckCollisions(Ball ball, float prevY, float currY, out SliceType landedSliceType, out int landedSliceIndex)
    {
        landedSliceType = SliceType.Empty;
        landedSliceIndex = -1;

        // Bouncing logic only checks when moving downwards
        if (ball.VelocityY > 0)
            return null;

        foreach (var ring in Rings)
        {
            if (ring.IsDestroyed) continue;

            // Trigger collision when ball crosses the ring plane downward
            if (prevY >= ring.HeightY && currY <= ring.HeightY)
            {
                // World angle of ball is 270 degrees (positive Z). Slice 10 center is at 270 degrees.
                // The formula (315 - rotationDeg) correctly maps to the centered slice index.
                float rotationDeg = MathHelper.ToDegrees(RotationY);
                float localAngleDeg = ((315f - rotationDeg) % 360 + 360) % 360;
                int sliceIndex = (int)(localAngleDeg / 30f) % 12;

                // If the slice is empty (a hole), let the ball pass through
                if (ring.Slices[sliceIndex] == SliceType.Empty)
                    continue;

                landedSliceIndex = sliceIndex;
                landedSliceType = ring.Slices[sliceIndex];
                return ring;
            }
        }

        return null;
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, int currentLevel)
    {
        // 1. Draw central pole
        // Calculate pole Y offset so it aligns with level height
        float bottomHeight = -(Rings.Count - 1) * RingSpacing;
        float poleCenterY = bottomHeight / 2f;

        effect.World = Matrix.CreateTranslation(0, poleCenterY, 0) * Matrix.CreateRotationY(RotationY);
        // Pole color: cool dark metal gradient or glowing dark indigo
        effect.DiffuseColor = new Vector3(0.08f, 0.08f, 0.15f); // Indigo pole
        effect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.3f);
        
        bool prevVertexColorEnabled = effect.VertexColorEnabled;
        effect.VertexColorEnabled = false;

        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _poleVertices,
                0,
                _poleVertices.Length,
                _poleIndices,
                0,
                _poleIndices.Length / 3
            );
        }

        // 2. Draw rings
        // Safe slice color gradient based on level index and ring position
        // Level 1: Teal to Blue, Level 2: Cyan to Green, Level 3+: Purple to Pink
        Color safeStartColor = new Color(0, 180, 255);
        Color safeEndColor = new Color(130, 0, 255);

        if (currentLevel == 2)
        {
            safeStartColor = new Color(0, 210, 120); // Green
            safeEndColor = new Color(0, 150, 255);   // Teal
        }
        else if (currentLevel >= 3)
        {
            safeStartColor = new Color(200, 0, 255); // Neon Purple
            safeEndColor = new Color(255, 0, 100);   // Hot Pink
        }

        Color obstacleColor = new Color(255, 40, 0); // Hot neon red/orange for obstacles
        Color victoryColor = new Color(30, 230, 80); // Bright emerald green for victory pad

        int ringCount = Rings.Count;

        for (int r = 0; r < ringCount; r++)
        {
            var ring = Rings[r];
            if (ring.IsDestroyed) continue;

            // Interpolate safe platform color based on ring depth
            float ratio = (float)r / Math.Max(1, ringCount - 1);
            Color ringSafeColor = Color.Lerp(safeStartColor, safeEndColor, ratio);

            for (int s = 0; s < 12; s++)
            {
                SliceType slice = ring.Slices[s];
                if (slice == SliceType.Empty) continue;

                Color sliceColor = ringSafeColor;
                if (r == ringCount - 1)
                {
                    sliceColor = victoryColor; // Victory pad color
                }
                else if (slice == SliceType.Fatal)
                {
                    sliceColor = obstacleColor;
                }

                 // Slice is drawn rotated by its index (with a 15-degree offset to center it under the ball at start) and the tower's current rotation
                 float sliceAngleOffsetRad = s * MathHelper.ToRadians(30f) - MathHelper.ToRadians(15f);
                 Matrix world = Matrix.CreateRotationY(sliceAngleOffsetRad) *
                                Matrix.CreateTranslation(0, ring.HeightY, 0) *
                                Matrix.CreateRotationY(RotationY);

                effect.World = world;
                effect.DiffuseColor = sliceColor.ToVector3();

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        _sliceVertices,
                        0,
                        _sliceVertices.Length,
                        _sliceIndices,
                        0,
                        _sliceIndices.Length / 3
                    );
                }
            }
        }

        effect.VertexColorEnabled = prevVertexColorEnabled;
    }
}
