/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/ .
 *
 * Copyright (C) 2019 Maxim Yudin <stibiu@yandex.ru>.
 */

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;

namespace Spirograph
{
  /// <summary>
  /// WPF control class for trace model rendering.
  /// </summary>
  public class TraceRenderer : WpfGame, IDisposable
  {
    /// <summary>
    /// WPF graphics device.
    /// </summary>
    private WpfGraphicsDeviceService _graphics;

    /// <summary>
    /// Sprite batch.
    /// </summary>
    private SpriteBatch _spriteBatch;

    /// <summary>
    /// Sprite for the trace rendering.
    /// </summary>
    private Texture2D _glowSprite;

    /// <summary>
    /// Sprite for simple lines rendering.
    /// </summary>
    private Texture2D _lineSprite;

    /// <summary>
    /// Trace state updater thread.
    /// </summary>
    private Thread _traceUpdater;

    /// <summary>
    /// Trace model instance being rendered.
    /// </summary>
    public TraceModel Trace;

    /// <summary>
    /// Graphics initialization callback.
    /// </summary>
    protected override void Initialize()
    {
      _graphics = new WpfGraphicsDeviceService(this);
      _graphics.PreferMultiSampling = true;

      _traceUpdater = new Thread(context =>
      {
        while (Thread.CurrentThread.IsAlive)
        {
          (context as SynchronizationContext)?.Post(state => Trace.Step(0.002F), null);
          Thread.Sleep(2);
        }
      });
      _traceUpdater.Start(SynchronizationContext.Current);

      base.Initialize();
    }

    /// <summary>
    /// Content loading callback.
    /// </summary>
    protected override void LoadContent()
    {
      _spriteBatch = new SpriteBatch(GraphicsDevice);

      _glowSprite = Texture2D.FromStream(GraphicsDevice, new FileStream("Sprites/Glow.png", FileMode.Open));

      _lineSprite = new Texture2D(GraphicsDevice, 1, 1);
      _lineSprite.SetData(new[] {Color.White});

      base.LoadContent();
    }

    /// <summary>
    /// Frame drawing cycle callback.
    /// </summary>
    /// <param name="gameTime"></param>
    protected override void Draw(GameTime gameTime)
    {
      GraphicsDevice.Clear(Color.Black);

      if (Trace == null || Trace.Points.Count == 0 || Trace.Drives.Count == 0)
        return;

      var viewportScale = (float) Math.Min(Width, Height) * 0.45F;
      var traceColor = new Color(Trace.Color.R, Trace.Color.G, Trace.Color.B, Trace.Color.A);

      // Drawing drive circles if needed.
      if (Trace.ShowDriveCircles)
      {
        var drivesFullScale = Trace.Drives.Sum(drive => drive.Scale);
        var driveCenter = Vector2.Zero;
        var driveCircleColor = new Color(0.2F, 0.2F, 0.2F);

        _spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Opaque, SamplerState.AnisotropicClamp, DepthStencilState.Default,
          RasterizerState.CullNone, null, Matrix.CreateTranslation(new Vector3((float) Width / 2F, (float) Height / 2F, 0F)));

        foreach (var drive in Trace.Drives)
        {
          DrawCircle(_lineSprite, driveCenter, drive.Scale / drivesFullScale, driveCircleColor, 0.005F, viewportScale, 1F);

          var drivePoint = driveCenter + drive.OffsetVector / drivesFullScale;
          DrawLine(_lineSprite, driveCenter, drivePoint, driveCircleColor, 0.005F, viewportScale, 1F);

          driveCenter = drivePoint;
        }

        _spriteBatch.End();
      }

      // Drawing trace.
      _spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.Default,
        RasterizerState.CullNone, null, Matrix.CreateTranslation(new Vector3((float) Width / 2F, (float) Height / 2F, 0F)));

      for (var index = 0; index < Trace.Points.Count - 1; index++)
      {
        var start = new Vector2(Trace.Points[index].X, Trace.Points[index].Y);
        var end = new Vector2(Trace.Points[index + 1].X, Trace.Points[index + 1].Y);
        var mainColor = new Color(traceColor, (traceColor.A / 255F) * (Trace.Fading ? 1F - (1F * index / Trace.Length) : 1F));
        var lightingColor = new Color(Color.Lerp(mainColor, Color.White, 0.5F), mainColor.A / 255F * 0.2F);

        DrawLine(_glowSprite, start, end, mainColor, Trace.Thickness * 0.04F, viewportScale, 0F);
        DrawLine(_glowSprite, start, end, lightingColor, Trace.Thickness * 0.02F, viewportScale, 0F);
      }

      _spriteBatch.End();

      base.Draw(gameTime);
    }

    /// <summary>
    /// Draws a line using a textured trail.
    /// </summary>
    /// <param name="texture">The texture used for line drawing.</param>
    /// <param name="start">Start vector of the line.</param>
    /// <param name="end">End vector of the line.</param>
    /// <param name="color">Color of the line.</param>
    /// <param name="thickness">Visual thickness of the line in pixels.</param>
    /// <param name="scale">Scaling factor of the line.</param>
    /// <param name="depth">Virtual drawing depth.</param>
    private void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, float thickness, float scale, float depth)
    {
      var lineVector = (end - start) * scale;
      var length = (float) Math.Ceiling(lineVector.Length());
      var lineStep = lineVector / length;
      var vector = start * scale;
      for (var step = 0; step < length; step++)
      {
        _spriteBatch.Draw(texture, vector, null, color, 0F, new Vector2(texture.Width / 2F, texture.Height / 2F),
          new Vector2(thickness * scale / texture.Width, thickness * scale / texture.Height), SpriteEffects.None, depth);

        vector += lineStep;
      }
    }

    /// <summary>
    /// Draws a circle using a textured trail.
    /// </summary>
    /// <param name="texture">The texture used for line drawing.</param>
    /// <param name="center">Vector of the circle's center.</param>
    /// <param name="radius">Circle's radius.</param>
    /// <param name="color">Color of the circle.</param>
    /// <param name="thickness">Visual thickness of the circle's line in pixels.</param>
    /// <param name="scale">Scaling factor of the circle.</param>
    /// <param name="depth">Virtual drawing depth.</param>
    private void DrawCircle(Texture2D texture, Vector2 center, float radius, Color color, float thickness, float scale, float depth)
    {
      for (var angle = 0F; angle < 360F; angle++)
      {
        var start = center +
          new Vector2(radius * (float) Math.Cos(MathHelper.ToRadians(angle)), radius * (float) Math.Sin(MathHelper.ToRadians(angle)));
        var end = center +
          new Vector2(radius * (float) Math.Cos(MathHelper.ToRadians(angle + 1F)), radius * (float) Math.Sin(MathHelper.ToRadians(angle + 1F)));
        DrawLine(texture, start, end, color, thickness, scale, depth);
      }
    }

    /// <summary>
    /// Stops the trace state updater thread.
    /// </summary>
    public new void Dispose()
    {
      _traceUpdater.Abort();
    }
  }
}
