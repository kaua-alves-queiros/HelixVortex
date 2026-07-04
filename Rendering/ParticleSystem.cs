using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HelixVortex.Rendering;

public class ParticleSystem
{
    public class Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Rotation;
        public Vector3 RotationSpeed;
        public float Scale;
        public float ScaleSpeed;
        public Color Color;
        public float Life; // 1.0f -> 0.0f
        public float DecaySpeed;
        public bool IsDebris;
        public Vector3 DebrisSize;
    }

    private readonly List<Particle> _particles = new();
    private readonly Random _random = new();

    // Box mesh for particles & debris
    private VertexPositionNormalColor[] _boxVertices;
    private short[] _boxIndices;

    public ParticleSystem()
    {
        BuildBoxMesh();
    }

    private void BuildBoxMesh()
    {
        List<VertexPositionNormalColor> verts = new();
        List<short> inds = new();

        void AddFace(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 normal)
        {
            short baseIdx = (short)verts.Count;
            verts.Add(new VertexPositionNormalColor(p1, normal, Color.White));
            verts.Add(new VertexPositionNormalColor(p2, normal, Color.White));
            verts.Add(new VertexPositionNormalColor(p3, normal, Color.White));
            verts.Add(new VertexPositionNormalColor(p4, normal, Color.White));

            inds.Add(baseIdx);
            inds.Add((short)(baseIdx + 1));
            inds.Add((short)(baseIdx + 2));

            inds.Add(baseIdx);
            inds.Add((short)(baseIdx + 2));
            inds.Add((short)(baseIdx + 3));
        }

        // Corner positions for a 1x1x1 cube centered at origin
        Vector3 nnn = new Vector3(-0.5f, -0.5f, -0.5f);
        Vector3 nnp = new Vector3(-0.5f, -0.5f, 0.5f);
        Vector3 npn = new Vector3(-0.5f, 0.5f, -0.5f);
        Vector3 npp = new Vector3(-0.5f, 0.5f, 0.5f);
        Vector3 pnn = new Vector3(0.5f, -0.5f, -0.5f);
        Vector3 pnp = new Vector3(0.5f, -0.5f, 0.5f);
        Vector3 ppn = new Vector3(0.5f, 0.5f, -0.5f);
        Vector3 ppp = new Vector3(0.5f, 0.5f, 0.5f);

        // Top face
        AddFace(npn, npp, ppp, ppn, Vector3.Up);
        // Bottom face
        AddFace(pnn, pnp, nnp, nnn, Vector3.Down);
        // Left face
        AddFace(nnn, nnp, npp, npn, Vector3.Left);
        // Right face
        AddFace(pnp, pnn, ppn, ppp, Vector3.Right);
        // Front face (towards positive Z)
        AddFace(nnp, pnp, ppp, npp, Vector3.Forward);
        // Back face (towards negative Z)
        AddFace(pnn, nnn, npn, ppn, Vector3.Backward);

        _boxVertices = verts.ToArray();
        _boxIndices = inds.ToArray();
    }

    public void SpawnBounceParticles(Vector3 position, Color color)
    {
        // Spawn small sparks expanding outwards
        int count = 12 + _random.Next(6);
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float speed = 2.0f + (float)_random.NextDouble() * 3.0f;
            Vector3 velocity = new Vector3(
                (float)Math.Cos(angle) * speed,
                3.0f + (float)_random.NextDouble() * 4.0f,
                (float)Math.Sin(angle) * speed
            );

            _particles.Add(new Particle
            {
                Position = position,
                Velocity = velocity,
                Rotation = Vector3.Zero,
                RotationSpeed = new Vector3(
                    (float)_random.NextDouble() * 5f,
                    (float)_random.NextDouble() * 5f,
                    (float)_random.NextDouble() * 5f
                ),
                Scale = 0.15f + (float)_random.NextDouble() * 0.1f,
                ScaleSpeed = -0.1f,
                Color = color,
                Life = 1.0f,
                DecaySpeed = 1.5f + (float)_random.NextDouble() * 1.0f,
                IsDebris = false
            });
        }
    }

    public void SpawnVortexTrail(Vector3 position, Color color)
    {
        // Trail particles floating upwards and shrinking
        _particles.Add(new Particle
        {
            Position = position + new Vector3(
                ((float)_random.NextDouble() - 0.5f) * 0.3f,
                ((float)_random.NextDouble() - 0.5f) * 0.3f,
                ((float)_random.NextDouble() - 0.5f) * 0.3f
            ),
            Velocity = new Vector3(
                ((float)_random.NextDouble() - 0.5f) * 0.5f,
                1.0f + (float)_random.NextDouble() * 2.0f,
                ((float)_random.NextDouble() - 0.5f) * 0.5f
            ),
            Rotation = new Vector3(
                (float)_random.NextDouble() * MathHelper.TwoPi,
                (float)_random.NextDouble() * MathHelper.TwoPi,
                (float)_random.NextDouble() * MathHelper.TwoPi
            ),
            RotationSpeed = new Vector3(
                (float)_random.NextDouble() * 3f,
                (float)_random.NextDouble() * 3f,
                (float)_random.NextDouble() * 3f
            ),
            Scale = 0.2f + (float)_random.NextDouble() * 0.15f,
            ScaleSpeed = -0.3f,
            Color = color,
            Life = 1.0f,
            DecaySpeed = 2.0f + (float)_random.NextDouble() * 2.0f,
            IsDebris = false
        });
    }

    public void SpawnRingExplosion(float ringHeight, List<Color> sliceColors, float rotationY)
    {
        // Explode the entire ring! Spawn 12 large debris shards flying outwards.
        // We know each slice represents a 30 degree sector.
        for (int i = 0; i < 12; i++)
        {
            Color color = sliceColors[i];
            if (color == Color.Transparent) continue; // Skip empty slices

            // Center angle of this slice in world coordinates (matching visual slice center)
            float sliceAngleRad = MathHelper.ToRadians(i * 30) + rotationY;
            float cos = (float)Math.Cos(sliceAngleRad);
            float sin = (float)Math.Sin(sliceAngleRad);

            // Ring radii: inner=1.2, outer=2.5. Shard starts around mid-radius=1.85
            float midRadius = 1.85f;
            Vector3 startPos = new Vector3(cos * midRadius, ringHeight, sin * midRadius);

            // Velocity explodes outward from center
            float speed = 4.0f + (float)_random.NextDouble() * 4.0f;
            Vector3 velocity = new Vector3(
                cos * speed,
                2.0f + (float)_random.NextDouble() * 4.0f,
                sin * speed
            );

            _particles.Add(new Particle
            {
                Position = startPos,
                Velocity = velocity,
                Rotation = new Vector3(
                    (float)_random.NextDouble() * MathHelper.TwoPi,
                    (float)_random.NextDouble() * MathHelper.TwoPi,
                    (float)_random.NextDouble() * MathHelper.TwoPi
                ),
                RotationSpeed = new Vector3(
                    (float)(_random.NextDouble() - 0.5f) * 10f,
                    (float)(_random.NextDouble() - 0.5f) * 10f,
                    (float)(_random.NextDouble() - 0.5f) * 10f
                ),
                Scale = 1.0f,
                ScaleSpeed = -0.4f, // shrink over time
                Color = color,
                Life = 1.0f,
                DecaySpeed = 0.8f + (float)_random.NextDouble() * 0.5f,
                IsDebris = true,
                DebrisSize = new Vector3(
                    0.6f + (float)_random.NextDouble() * 0.4f,
                    0.25f,
                    0.6f + (float)_random.NextDouble() * 0.4f
                )
            });
        }
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];

            // Apply gravity to bounce particles and debris
            p.Velocity.Y -= 9.8f * dt;

            p.Position += p.Velocity * dt;
            p.Rotation += p.RotationSpeed * dt;
            p.Scale = Math.Max(0.0f, p.Scale + p.ScaleSpeed * dt);
            p.Life -= p.DecaySpeed * dt;

            if (p.Life <= 0.0f || p.Scale <= 0.0f)
            {
                _particles.RemoveAt(i);
            }
        }
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        // Enable vertex colors for particles (or we can use DiffuseColor)
        bool prevVertexColorEnabled = effect.VertexColorEnabled;
        effect.VertexColorEnabled = false; // We will use DiffuseColor to dye the particle

        foreach (var p in _particles)
        {
            Vector3 finalScale = p.IsDebris ? p.DebrisSize * p.Scale : new Vector3(p.Scale);

            Matrix world = Matrix.CreateScale(finalScale) *
                           Matrix.CreateRotationX(p.Rotation.X) *
                           Matrix.CreateRotationY(p.Rotation.Y) *
                           Matrix.CreateRotationZ(p.Rotation.Z) *
                           Matrix.CreateTranslation(p.Position);

            effect.World = world;
            effect.DiffuseColor = p.Color.ToVector3();
            // Alpha fading
            effect.Alpha = p.Life;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _boxVertices,
                    0,
                    _boxVertices.Length,
                    _boxIndices,
                    0,
                    _boxIndices.Length / 3
                );
            }
        }

        effect.Alpha = 1.0f;
        effect.VertexColorEnabled = prevVertexColorEnabled;
    }

    public void Clear()
    {
        _particles.Clear();
    }
}
