using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HelixVortex.Rendering;

namespace HelixVortex.Entities;

public class Ball
{
    public Vector3 Position;
    public float VelocityY;
    public float Radius { get; } = 0.2f;
    public float RadialDistance { get; } = 1.85f; // Must match ring radius center

    public const float Gravity = -26f;
    public const float BounceVelocity = 10f;

    // Combo/Vortex fields
    public int ConsecutiveRingsPassed { get; set; } = 0;
    public bool IsVortexMode => ConsecutiveRingsPassed >= 2;

    // Visual buffers
    private VertexPositionNormalColor[] _sphereVertices;
    private short[] _sphereIndices;

    // Animation / Squish effect
    private float _squishFactorY = 1.0f;
    private float _squishFactorX = 1.0f;

    public Ball(GraphicsDevice graphicsDevice, Vector3 startPosition)
    {
        Position = startPosition;
        Position.X = 0;
        Position.Z = RadialDistance;
        VelocityY = 0;

        // Generate sphere geometry
        MeshBuilder.CreateSphere(Radius, 16, 16, Color.White, out _sphereVertices, out _sphereIndices);
    }

    public void Reset(Vector3 startPosition)
    {
        Position = startPosition;
        Position.X = 0;
        Position.Z = RadialDistance;
        VelocityY = 0;
        ConsecutiveRingsPassed = 0;
        _squishFactorY = 1.0f;
        _squishFactorX = 1.0f;
    }

    public void Update(GameTime gameTime, ParticleSystem particleSystem)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Apply physics
        VelocityY += Gravity * dt;
        Position.Y += VelocityY * dt;

        // Visual squish calculations (stretch when moving fast, squish on bounce)
        if (VelocityY > 0)
        {
            // Moving up: stretch slightly
            _squishFactorY = MathHelper.Lerp(_squishFactorY, 1.15f, 15f * dt);
            _squishFactorX = MathHelper.Lerp(_squishFactorX, 0.92f, 15f * dt);
        }
        else
        {
            // Falling: stretch down
            _squishFactorY = MathHelper.Lerp(_squishFactorY, 1.25f, 10f * dt);
            _squishFactorX = MathHelper.Lerp(_squishFactorX, 0.88f, 10f * dt);
        }

        // Emit trail particles
        if (IsVortexMode)
        {
            // Vibrant fiery trail in Vortex mode
            Color fireColor = Color.Lerp(Color.Orange, Color.Red, (float)new Random().NextDouble());
            particleSystem.SpawnVortexTrail(Position, fireColor);
            particleSystem.SpawnVortexTrail(Position, Color.Yellow);
        }
        else
        {
            // Soft trail for normal ball movement
            if (new Random().NextDouble() < 0.3)
            {
                particleSystem.SpawnVortexTrail(Position, new Color(0, 210, 255, 100)); // Teal trail
            }
        }
    }

    public void TriggerBounce(ParticleSystem particleSystem)
    {
        VelocityY = BounceVelocity;
        ConsecutiveRingsPassed = 0;

        // Visual bounce impact (flatten the ball)
        _squishFactorY = 0.5f;
        _squishFactorX = 1.4f;

        // Spawn splash particles
        particleSystem.SpawnBounceParticles(Position, new Color(0, 210, 255));
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        // Calculate ball matrices
        Matrix world = Matrix.CreateScale(new Vector3(_squishFactorX, _squishFactorY, _squishFactorX)) *
                       Matrix.CreateTranslation(Position);

        effect.World = world;

        // Ball Color: fire red/orange in Vortex mode, clean neon teal in normal mode
        Color ballColor = IsVortexMode ? Color.Orange : new Color(0, 220, 255);
        effect.DiffuseColor = ballColor.ToVector3();

        // Enable specular lighting for a shiny marble/ball look
        Vector3 prevSpecular = effect.SpecularColor;
        float prevPower = effect.SpecularPower;

        effect.SpecularColor = new Vector3(1.0f, 1.0f, 1.0f);
        effect.SpecularPower = 32f;

        bool prevVertexColorEnabled = effect.VertexColorEnabled;
        effect.VertexColorEnabled = false;

        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _sphereVertices,
                0,
                _sphereVertices.Length,
                _sphereIndices,
                0,
                _sphereIndices.Length / 3
            );
        }

        effect.VertexColorEnabled = prevVertexColorEnabled;
        effect.SpecularColor = prevSpecular;
        effect.SpecularPower = prevPower;
    }
}
