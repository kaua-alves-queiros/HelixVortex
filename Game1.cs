using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HelixVortex.Entities;
using HelixVortex.Rendering;

namespace HelixVortex;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    LevelComplete
}

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // Entities & Systems
    private Ball _ball;
    private Tower _tower;
    private ParticleSystem _particleSystem;
    private Texture2D _pixelTexture;
    private Texture2D _gradientTexture;

    // Game States
    private GameState _gameState = GameState.MainMenu;
    private int _score = 0;
    private int _highScore = 0;
    private int _currentLevel = 1;
    private const string HighScoreFile = "highscore.txt";

    // Camera Matrices
    private Matrix _worldMatrix;
    private Matrix _viewMatrix;
    private Matrix _projectionMatrix;
    private BasicEffect _basicEffect;

    // Camera follow fields
    private float _cameraY = 0f;
    private float _cameraYTarget = 0f;

    // Screen Shake fields
    private float _shakeTime = 0f;
    private float _shakeIntensity = 0f;
    private Vector3 _shakeOffset = Vector3.Zero;
    private readonly Random _random = new();

    // Visuals & Transitions
    private float _stateTransitionAlpha = 0f;
    private float _logoPulseTimer = 0f;
    private float _victoryCelebrationTimer = 0f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Portrait window size - perfect for a vertical stack game
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 900;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        // Setup 3D Matrices
        float aspectRatio = (float)GraphicsDevice.Viewport.Width / GraphicsDevice.Viewport.Height;
        _worldMatrix = Matrix.Identity;
        _viewMatrix = Matrix.CreateLookAt(new Vector3(0, 5, 10), new Vector3(0, 2, 0), Vector3.Up);
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(45),
            aspectRatio,
            0.1f,
            100f
        );

        // BasicEffect setup with lighting and specular highlights for a glossy look
        _basicEffect = new BasicEffect(GraphicsDevice)
        {
            World = _worldMatrix,
            View = _viewMatrix,
            Projection = _projectionMatrix,
            LightingEnabled = true
        };

        // Directional Light 0 (Front-Right-Top)
        _basicEffect.DirectionalLight0.Enabled = true;
        _basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1, -1.5f, 2f));
        _basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1.0f, 0.95f, 0.9f);
        _basicEffect.DirectionalLight0.SpecularColor = new Vector3(0.8f, 0.8f, 0.8f);

        // Directional Light 1 (Back-Left Fill Light)
        _basicEffect.DirectionalLight1.Enabled = true;
        _basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1f, 1f, -2f));
        _basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.2f, 0.25f, 0.4f);

        _basicEffect.AmbientLightColor = new Vector3(0.25f, 0.25f, 0.35f);

        // Load High Score
        LoadHighScore();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create 1x1 white texture for simple drawing and lines
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // Create 1x2 texture for smooth fullscreen vertical background gradient
        _gradientTexture = new Texture2D(GraphicsDevice, 1, 2);
        _gradientTexture.SetData(new[]
        {
            new Color(11, 12, 33),  // Deep space dark blue (top)
            new Color(33, 17, 49)   // Dark violet/magenta (bottom)
        });

        // Initialize Systems & Entities
        _particleSystem = new ParticleSystem();
        _ball = new Ball(GraphicsDevice, new Vector3(0, 1.5f, Tower.OuterRadius));
        _tower = new Tower(GraphicsDevice);

        // Generate Level 1
        LoadLevel(_currentLevel);
    }

    private void LoadLevel(int level)
    {
        _currentLevel = level;
        _tower.GenerateLevel(_currentLevel);

        // Reset ball position to top ring bounce point
        _ball.Reset(new Vector3(0, 1.5f, _ball.RadialDistance));

        // Center camera immediately
        _cameraY = 0f;
        _cameraYTarget = 0f;
        _shakeTime = 0f;
        _shakeOffset = Vector3.Zero;
    }

    private void TriggerCameraShake(float intensity, float time)
    {
        _shakeIntensity = intensity;
        _shakeTime = time;
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Process Global Keys (Exit and Pause)
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
            Exit();

        // Logo Pulsing effect for Main Menu
        _logoPulseTimer += dt * 3f;

        // State Machine Updates
        switch (_gameState)
        {
            case GameState.MainMenu:
                UpdateMainMenu(keyboard);
                break;

            case GameState.Playing:
                UpdatePlaying(gameTime, keyboard);
                break;

            case GameState.Paused:
                if (keyboard.IsKeyDown(Keys.Space) && _stateTransitionAlpha >= 1f)
                {
                    _gameState = GameState.Playing;
                }
                break;

            case GameState.GameOver:
                UpdateGameOver(keyboard);
                break;

            case GameState.LevelComplete:
                UpdateLevelComplete(gameTime);
                break;
        }
        // Always update particles
        _particleSystem.Update(gameTime);
        // Update Camera Shake
        if (_shakeTime > 0)
        {
            _shakeTime -= dt;
            float progress = MathHelper.Clamp(_shakeTime / 0.35f, 0f, 1f);
            float currentPower = _shakeIntensity * progress;
            _shakeOffset = new Vector3(
                ((float)_random.NextDouble() - 0.5f) * currentPower,
                ((float)_random.NextDouble() - 0.5f) * currentPower,
                ((float)_random.NextDouble() - 0.5f) * currentPower
            );
        }
        else
        {
            _shakeOffset = Vector3.Zero;
        }

        base.Update(gameTime);
    }

    private void UpdateMainMenu(KeyboardState keyboard)
    {
        // Press Space or click to start
        if (keyboard.IsKeyDown(Keys.Space) || Mouse.GetState().LeftButton == ButtonState.Pressed)
        {
            _score = 0;
            _gameState = GameState.Playing;
            LoadLevel(1);
        }
    }

    private void UpdatePlaying(GameTime gameTime, KeyboardState keyboard)
    {
        // Tower rotation logic
        _tower.Update(gameTime);


        // Ball physics and particles
        float previousY = _ball.Position.Y;
        _ball.Update(gameTime, _particleSystem);
        float currentY = _ball.Position.Y;

        // Smooth camera follow target logic
        _cameraYTarget = _ball.Position.Y - 0.5f;
        _cameraY = MathHelper.Lerp(_cameraY, _cameraYTarget, 6f * (float)gameTime.ElapsedGameTime.TotalSeconds);

        // Collision Check (when moving downwards)
        Ring collidedRing = null;
        if (_ball.VelocityY <= 0)
        {
            collidedRing = _tower.CheckCollisions(_ball, previousY, currentY, out SliceType sliceType, out int sliceIndex);

            if (collidedRing != null)
            {
                // Align ball exactly on the platform height during impact
                _ball.Position.Y = collidedRing.HeightY;

                // 1. Victory Ring Collided? (last ring of the tower)
                if (collidedRing.RingIndex == _tower.Rings.Count - 1)
                {
                    _gameState = GameState.LevelComplete;
                    _victoryCelebrationTimer = 0f;
                    TriggerCameraShake(0.6f, 0.5f);
                    
                    // Spawn celebratory green spark particles
                    // for (int i = 0; i < 30; i++)
                    for (int i = 0; i < 20; i++)
                    {
                        _particleSystem.SpawnBounceParticles(_ball.Position, Color.LimeGreen);
                    }
                    return;
                }

                // 2. Safe slice or Vortex smash?
                if (sliceType == SliceType.Safe)
                {
                    if (_ball.IsVortexMode)
                    {
                        // Vortex mode destroys the ring
                        ShatterRing(collidedRing);
                    }
                    else
                    {
                        // Normal Bounce
                        _ball.TriggerBounce(_particleSystem);
                        TriggerCameraShake(0.12f, 0.15f);
                    }
                }
                // 3. Fatal obstacle collided?
                else if (sliceType == SliceType.Fatal)
                {
                    if (_ball.IsVortexMode)
                    {
                        // Vortex mode smashes through obstacles too!
                        ShatterRing(collidedRing);
                    }
                    else
                    {
                        // Game Over!
                        _gameState = GameState.GameOver;
                        TriggerCameraShake(0.8f, 0.45f);
                        // Spawn death red particles
                        for (int i = 0; i < 25; i++)
                        {
                            _particleSystem.SpawnBounceParticles(_ball.Position, Color.Red);
                        }
                    }
                }
            }
        }

        // Check if the ball passed any rings to accumulate score (holes/empty slices)
        foreach (var ring in _tower.Rings)
        {
            // If ball passes below a ring height that hasn't been destroyed, and it was not counted
            if (ring != collidedRing && !ring.IsDestroyed && previousY > ring.HeightY && currentY <= ring.HeightY)
            {
                // Ensure it was an empty slice (hole)
                // (Note: if it was safe/fatal, collision would have handled it. So this is a clean pass!)
                _ball.ConsecutiveRingsPassed++;

                // Give points based on consecutive falls
                int points = 10 * _ball.ConsecutiveRingsPassed;
                _score += points;

                if (_score > _highScore)
                {
                    _highScore = _score;
                    SaveHighScore();
                }

                // Camera shake on high drops
                if (_ball.ConsecutiveRingsPassed >= 2)
                {
                    TriggerCameraShake(0.35f, 0.25f);
                }
            }
        }
    }

    private void ShatterRing(Ring ring)
    {
        ring.IsDestroyed = true;
        _ball.VelocityY = -2f; // slight downwards push so it keeps falling smoothly
        _ball.ConsecutiveRingsPassed = 0; // reset combo after smash

        // Dynamic points bonus
        _score += 50;
        if (_score > _highScore)
        {
            _highScore = _score;
            SaveHighScore();
        }

        // Sound trigger visual (camera shake)
        TriggerCameraShake(0.7f, 0.35f);

        // Fetch colors of all slices on the ring to color the explosion debris
        List<Color> sliceColors = ring.GetSliceColors(
            new Color(0, 180, 255),
            new Color(255, 40, 0));
        _particleSystem.SpawnRingExplosion(ring.HeightY, sliceColors, _tower.RotationY);
    }

    private void UpdateGameOver(KeyboardState keyboard)
    {
        // Restart on Enter or Space
        if (keyboard.IsKeyDown(Keys.Enter) || keyboard.IsKeyDown(Keys.Space))
        {
            _gameState = GameState.Playing;
            _score = 0;
            // Regen/Shuffle the level to keep fatal positions dynamic
            LoadLevel(_currentLevel);
        }
    }

    private void UpdateLevelComplete(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _victoryCelebrationTimer += dt;

        // Auto advance to next level after 2 seconds
        if (_victoryCelebrationTimer >= 2.0f)
        {
            _gameState = GameState.Playing;
            LoadLevel(_currentLevel + 1);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        // Clear Color target and Depth buffer to start with a clean state each frame
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

        // 1. Draw Fullscreen Gradient Background in 2D
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        _spriteBatch.Draw(_gradientTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White);
        _spriteBatch.End();

        // 2. Setup 3D State
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        // Track Camera look matrices with shake offset
        Vector3 cameraEye = new Vector3(0, _cameraY + 3.2f, 7.5f) + _shakeOffset;
        Vector3 cameraLookAt = new Vector3(0, _cameraY - 0.4f, 0) + _shakeOffset;
        _viewMatrix = Matrix.CreateLookAt(cameraEye, cameraLookAt, Vector3.Up);
        _basicEffect.View = _viewMatrix;
        _tower.Draw(GraphicsDevice, _basicEffect, _currentLevel);
        _ball.Draw(GraphicsDevice, _basicEffect);

        // Render 3D Particles (with Alpha Transparency)
        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        _particleSystem.Draw(GraphicsDevice, _basicEffect);
        // 3. Render 2D UI overlay
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        DrawUI();
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawUI()
    {
        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;

        switch (_gameState)
        {
            case GameState.MainMenu:
                // Logo pulse sizing
                float pulse = 1f + (float)Math.Sin(_logoPulseTimer) * 0.05f;
                int scale = (int)(6f * pulse);

                // Draw title
                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, "HELIX VORTEX", screenWidth / 2, 220, scale, new Color(0, 220, 255));
                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, "3D ARCADE", screenWidth / 2, 310, 3, new Color(130, 0, 255));

                // Pulsing glow instruction
                float textPulse = 0.6f + (float)Math.Sin(_logoPulseTimer * 1.5f) * 0.4f;
                Color glowColor = new Color(Color.White, textPulse);
                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, "PRESS SPACE OR CLICK TO PLAY", screenWidth / 2, 550, 2, glowColor);

                // High score
                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, $"HIGH SCORE: {_highScore}", screenWidth / 2, 750, 3, Color.Gold);
                break;

            case GameState.Playing:
                DrawGameplayUI(screenWidth);
                break;

            case GameState.GameOver:
                // Dark overlay
                _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), new Color(0, 0, 0, 180));

                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, "GAME OVER", screenWidth / 2, 300, 7, new Color(255, 40, 40));
                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, $"SCORE: {_score}", screenWidth / 2, 420, 4, Color.White);
                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, $"HIGH SCORE: {_highScore}", screenWidth / 2, 470, 3, Color.Gold);

                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, "PRESS ENTER TO RETRY", screenWidth / 2, 600, 2, Color.LightGray);
                break;

            case GameState.LevelComplete:
                // Green overlay
                _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), new Color(0, 80, 0, 120));

                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, "STAGE CLEAR", screenWidth / 2, 300, 7, new Color(50, 255, 100));
                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, $"SCORE: {_score}", screenWidth / 2, 420, 4, Color.White);
                PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, "LOADING NEXT STAGE...", screenWidth / 2, 600, 2, Color.LightGreen);
                break;
        }
    }

    private void DrawGameplayUI(int screenWidth)
    {
        // 1. Current Score
        PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, $"{_score}", screenWidth / 2, 40, 5, Color.White);

        // 2. Combo text (if active)
        if (_ball.ConsecutiveRingsPassed >= 2)
        {
            Color comboColor = _ball.ConsecutiveRingsPassed >= 3 ? Color.OrangeRed : Color.Orange;
            string vortexText = _ball.ConsecutiveRingsPassed >= 3 ? "MEGA VORTEX!!!" : "VORTEX MODE!";
            PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, vortexText, screenWidth / 2, 100, 3, comboColor);
        }

        // 3. Level progress bar
        int barWidth = 240;
        int barHeight = 8;
        int barX = (screenWidth - barWidth) / 2;
        int barY = 160;

        // Progress fraction (ratio of how low the ball is compared to bottom)
        float endY = -(_tower.Rings.Count - 1) * Tower.RingSpacing;
        float currentY = _ball.Position.Y;
        float progress = MathHelper.Clamp(currentY / endY, 0f, 1f);

        // Draw track background
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, barWidth, barHeight), new Color(255, 255, 255, 50));
        // Draw progress fill
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, (int)(barWidth * progress), barHeight), Color.White);

        // Level text indicators
        string currentLevelStr = $"{_currentLevel}";
        string nextLevelStr = $"{_currentLevel + 1}";
        
        // Draw left level circle (current)
        int nodeSize = 24;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX - nodeSize, barY + (barHeight/2) - (nodeSize/2), nodeSize, nodeSize), new Color(0, 220, 255));
        PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, currentLevelStr, barX - (nodeSize/2), barY + (barHeight/2) - 6, 1, Color.Black);

        // Draw right level circle (next)
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX + barWidth, barY + (barHeight/2) - (nodeSize/2), nodeSize, nodeSize), new Color(255, 255, 255, 100));
        PixelFontRenderer.DrawTextCentered(_spriteBatch, _pixelTexture, nextLevelStr, barX + barWidth + (nodeSize/2), barY + (barHeight/2) - 6, 1, Color.Black);
    }

    private void LoadHighScore()
    {
        try
        {
            if (File.Exists(HighScoreFile))
            {
                string text = File.ReadAllText(HighScoreFile);
                if (int.TryParse(text, out int val))
                {
                    _highScore = val;
                }
            }
        }
        catch
        {
            _highScore = 0;
        }
    }

    private void SaveHighScore()
    {
        try
        {
            File.WriteAllText(HighScoreFile, _highScore.ToString());
        }
        catch
        {
            // Ignore
        }
    }
}
