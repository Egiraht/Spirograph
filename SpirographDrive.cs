using System;

namespace Spirograph
{
  public class SpirographDrive
  {
    public readonly string Name;

    public double Frequency { get; set; } = 1.0;

    public double Scale { get; set; } = 1.0;

    public bool RotateCcw { get; set; } = false;

    private double _startAngle = 0.0;
    private double _angle = 0.0;
    public double StartAngle
    {
      get => _startAngle;
      set
      {
        var newStartAngle = (value < 0.0 || value >= 360.0 ? value % 360.0 : value);
        _angle += (newStartAngle - _startAngle);
        _startAngle = newStartAngle;
      }
    }

    public double XOffset => Scale * Math.Sin(_angle / 180.0 * Math.PI);

    public double YOffset => Scale * Math.Cos(_angle / 180.0 * Math.PI) * -1.0;

    public SpirographDrive(string name)
    {
      Name = name;
    }

    public void Reset()
    {
      _angle = _startAngle;
    }

    public void Step(double timeStep)
    {
      var newAngle = _angle + 360.0 * Frequency * timeStep * (RotateCcw ? -1.0 : 1.0);
      _angle = (newAngle < 0.0 || newAngle >= 360.0 ? newAngle % 360.0 : newAngle);
    }

    public override string ToString() => Name;
  }
}
