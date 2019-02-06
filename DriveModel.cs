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
  public class DriveModel : INotifyPropertyChanged
  {
    private float _frequency = 1F;
    public float Frequency
    {
      get => _frequency;
      set
      {
        _frequency = value >= 0F ? value : 0F;
        OnPropertyChanged(nameof(Frequency));
      }
    }

    private float _scale = 1F;
    public float Scale
    {
      get => _scale;
      set
      {
        _scale = value;
        OnPropertyChanged(nameof(Scale));
      }
    }

    private float _angle;
    public float Angle
    {
      get => _angle;
      private set
      {
        _angle = (value < 0F || value >= 360F ? value % 360F : value);
        OnPropertyChanged(nameof(Angle));
      }
    }

    private float _startAngle;
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

    private bool _rotateCcw;
    public bool RotateCcw
    {
      get => _rotateCcw;
      set
      {
        _rotateCcw = value;

        OnPropertyChanged(nameof(RotateCcw));
      }
    }

    public Vector2 OffsetVector => new Vector2(Scale * (float) Math.Sin(MathHelper.ToRadians(Angle)),
      -Scale * (float) Math.Cos(MathHelper.ToRadians(Angle)));

    public void Reset()
    {
      Angle = _startAngle;
    }

    public void Step(float timeStep)
    {
      Angle += 360F * Frequency * timeStep * (RotateCcw ? -1F : 1F);
    }

    public override string ToString()
    {
      return
        $"Drive {Frequency.ToString(CultureInfo.InvariantCulture)}/{Scale.ToString(CultureInfo.InvariantCulture)}/{StartAngle.ToString(CultureInfo.InvariantCulture)}/" +
        (RotateCcw ? "CCW" : "CW");
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
