/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/ .
 *
 * Copyright (C) 2019 Maxim Yudin <stibiu@yandex.ru>.
 */

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Spirograph
{
  /// <summary>
  /// Spirograph drive model class.
  /// </summary>
  public class DriveModel : INotifyPropertyChanged
  {
    private float _frequency = 1F;
    private float _scale = 1F;
    private float _angle = 0F;
    private float _startAngle = 0F;
    private bool _rotateCcw = false;

    /// <summary>
    /// Rotation frequency.
    /// </summary>
    public float Frequency
    {
      get => _frequency;
      set
      {
        _frequency = value >= 0F ? value : 0F;
        OnPropertyChanged(nameof(Frequency));
      }
    }

    /// <summary>
    /// Relative scale.
    /// </summary>
    public float Scale
    {
      get => _scale;
      set
      {
        _scale = value;
        OnPropertyChanged(nameof(Scale));
      }
    }

    /// <summary>
    /// Current rotation angle in degrees.
    /// </summary>
    public float Angle
    {
      get => _angle;
      private set
      {
        _angle = (value < 0F || value >= 360F ? value % 360F : value);
        OnPropertyChanged(nameof(Angle));
      }
    }

    /// <summary>
    /// Start rotation angle in degrees.
    /// </summary>
    public float StartAngle
    {
      get => _startAngle;
      set
      {
        var newStartAngle = (value < 0F || value >= 360F ? value % 360F : value);
        Angle += (newStartAngle - _startAngle);
        _startAngle = newStartAngle;

        OnPropertyChanged(nameof(StartAngle));
      }
    }

    /// <summary>
    /// Counter-clockwise rotation flag.
    /// </summary>
    public bool RotateCcw
    {
      get => _rotateCcw;
      set
      {
        _rotateCcw = value;
        OnPropertyChanged(nameof(RotateCcw));
      }
    }

    /// <summary>
    /// Current offset vector calculated from the current angle and scale.
    /// </summary>
    public Vector2 OffsetVector => new Vector2(Scale * (float) Math.Sin(MathHelper.ToRadians(Angle)),
      -Scale * (float) Math.Cos(MathHelper.ToRadians(Angle)));

    /// <summary>
    /// Resets the drive to its start state.
    /// </summary>
    public void Reset()
    {
      Angle = _startAngle;
    }

    /// <summary>
    /// Updates the drive to its state after <i>timeStep</i> seconds from current state.
    /// </summary>
    /// <param name="timeStep">Time step in seconds.</param>
    public void Step(float timeStep)
    {
      Angle += 360F * Frequency * timeStep * (RotateCcw ? -1F : 1F);
    }

    /// <summary>
    /// Gets the label of the drive depending on its properties.
    /// </summary>
    /// <returns>String containing the label of the drive.</returns>
    public override string ToString()
    {
      return
        $"Drive {Frequency.ToString(CultureInfo.InvariantCulture)}/{Scale.ToString(CultureInfo.InvariantCulture)}/{StartAngle.ToString(CultureInfo.InvariantCulture)}/" +
        (RotateCcw ? "CCW" : "CW");
    }

    /// <summary>
    /// Event called on any property change.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Event caller routine.
    /// </summary>
    /// <param name="propertyName">Name of the changed property.</param>
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
