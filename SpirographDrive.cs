using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Spirograph
{
  public class SpirographDrive : INotifyPropertyChanged
  {
    private readonly EllipseGeometry _drivePathCircle = new EllipseGeometry(new Point(0.0, 0.0), 1.0, 1.0);

    private readonly LineGeometry _driveAngleLine = new LineGeometry(new Point(0.0, 0.0), new Point(-1.0, 0.0));

    public readonly TranslateTransform DriveCircleOffset = new TranslateTransform(0.0, 0.0);

    public readonly ScaleTransform DriveCircleScale = new ScaleTransform(1.0, 1.0, 0.0, 0.0);

    public readonly GeometryGroup DriveCircle = new GeometryGroup {Transform = new TransformGroup()};

    private string _name = "";
    public string Name
    {
      get => _name;
      set
      {
        _name = value;

        OnPropertyChanged(nameof(Name));
      }
    }

    private double _frequency = 1.0;
    public double Frequency
    {
      get => _frequency;
      set
      {
        _frequency = value >= 0.0 ? value : 0.0;

        OnPropertyChanged(nameof(Frequency));
      }
    }

    private double _scale = 1.0;
    public double Scale
    {
      get => _scale;
      set
      {
        _scale = value;
        _drivePathCircle.RadiusX = _scale;
        _drivePathCircle.RadiusY = _scale;

        OnPropertyChanged(nameof(Scale));
      }
    }

    private double _angle;
    public double Angle
    {
      get => _angle;
      private set
      {
        _angle = (value < 0.0 || value >= 360.0 ? value % 360.0 : value);
        _driveAngleLine.EndPoint = _driveAngleLine.StartPoint + OffsetVector;

        OnPropertyChanged(nameof(Angle));
      }
    }

    private double _startAngle;
    public double StartAngle
    {
      get => _startAngle;
      set
      {
        var newStartAngle = (value < 0.0 || value >= 360.0 ? value % 360.0 : value);
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

    public Vector OffsetVector => new Vector(Scale * Math.Sin(Angle / 180.0 * Math.PI), -Scale * Math.Cos(Angle / 180.0 * Math.PI));

    public SpirographDrive()
    {
      DriveCircle.Children.Add(_drivePathCircle);
      DriveCircle.Children.Add(_driveAngleLine);

      var driveCircleTransformGroup = ((TransformGroup) DriveCircle.Transform);
      driveCircleTransformGroup.Children.Add(DriveCircleOffset);
      driveCircleTransformGroup.Children.Add(DriveCircleScale);
    }

    public void Reset()
    {
      Angle = _startAngle;
    }

    public void Step(double timeStep)
    {
      Angle += 360.0 * Frequency * timeStep * (RotateCcw ? -1.0 : 1.0);
    }

    public override string ToString() => !string.IsNullOrWhiteSpace(Name) ? Name.Trim() : "Spirograph drive";

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
