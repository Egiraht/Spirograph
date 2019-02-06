/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/ .
 *
 * Copyright (C) 2019 Maxim Yudin <stibiu@yandex.ru>.
 */

using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;

namespace Spirograph
{
  public class TraceRenderer : WpfGame, IDisposable
  {
    private WpfGraphicsDeviceService _graphics;

    private SpriteBatch _spriteBatch;

    private Texture2D _linePoint;

    private Thread _traceUpdater;

    public TraceModel Trace;

    protected override void Initialize()
    {
      _graphics = new WpfGraphicsDeviceService(this);
      _graphics.PreferMultiSampling = true;

      _traceUpdater = new Thread(context =>
      {
        while (Thread.CurrentThread.IsAlive)
        {
          (context as SynchronizationContext)?.Post(state => Trace.Step(0.005F), null);
          Thread.Sleep(5);
        }
      });
      _traceUpdater.Start(SynchronizationContext.Current);

      base.Initialize();
    }

    protected override void LoadContent()
    {
      _spriteBatch = new SpriteBatch(GraphicsDevice);

      // TODO: Replace sprite with the new that is less square on scaling. Bind file name to the program's location.
      _linePoint = Texture2D.FromStream(GraphicsDevice, new FileStream("Sprites/Glow.png", FileMode.Open));

      base.LoadContent();
    }

    // TODO: Add constant trace brightness mode.
    protected override void Draw(GameTime gameTime)
    {
      GraphicsDevice.Clear(Color.Black);

      if (Trace == null)
        return;

      var points = Trace.Points;
      if (points.Count == 0)
        return;

      _spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.Default,
        RasterizerState.CullNone, null, Matrix.CreateTranslation(new Vector3((float) Width / 2F, (float) Height / 2F, 0F)));

      var color = new Color(Trace.TraceColor.R, Trace.TraceColor.G, Trace.TraceColor.B, Trace.TraceColor.A);
      var scale = (float) Math.Min(Width, Height) * 0.45F;
      for (var index = 0; index < points.Count - 1; index++)
      {
        DrawLine(new Vector2(points[index].X, points[index].Y), new Vector2(points[index + 1].X, points[index + 1].Y),
          Trace.TraceThickness, scale, new Color(color, 1F - (1F * index / Trace.TraceLength)));
        DrawLine(new Vector2(points[index].X, points[index].Y), new Vector2(points[index + 1].X, points[index + 1].Y),
          Trace.TraceThickness / 2F, scale, new Color(Color.White, 0.25F - (0.25F * index / Trace.TraceLength)));
      }

      _spriteBatch.End();

      base.Draw(gameTime);
    }

    private void DrawLine(Vector2 start, Vector2 end, float thickness, float scale, Color color)
    {
      var lineVector = (end - start) * scale;
      var length = (float) Math.Ceiling(lineVector.Length());
      var lineStep = lineVector / length;
      var vector = start * scale;
      for (var step = 0; step < length; step++)
      {
        _spriteBatch.Draw(_linePoint, vector, null, color, 0F, new Vector2(_linePoint.Width / 2F, _linePoint.Height / 2F),
          new Vector2(thickness * scale / 50F / _linePoint.Width, thickness * scale / 50F / _linePoint.Height), SpriteEffects.None, 0F);

        vector += lineStep;
      }
    }

    public new void Dispose()
    {
      _traceUpdater.Abort();

      base.Dispose();
    }
  }
}
