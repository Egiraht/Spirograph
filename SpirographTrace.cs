using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Spirograph
{
  public class SpirographTrace
  {
    public ObservableCollection<SpirographDrive> Drives { get; } = new ObservableCollection<SpirographDrive>();

    private double _traceDiameter;
    public double TraceDiameter
    {
      get => _traceDiameter;
      set
      {
        _traceDiameter = value;
        Reset();
      }
    }

    private int _traceLength;
    public int TraceLength
    {
      get => _traceLength;
      set
      {
        _traceLength = value;
        Reset();
      }
    }

    private double _traceThickness;
    public double TraceThickness
    {
      get => _traceThickness;
      set
      {
        _traceThickness = value;
        ColoredPolyline.StrokeThickness = _traceThickness;
        GlowPolyline.StrokeThickness = _traceThickness / 1.5;
        CorePolyline.StrokeThickness = _traceThickness / 4.0;
      }
    }

    private Color _traceColor;
    public Color TraceColor
    {
      get => _traceColor;
      set
      {
        _traceColor = value;
        ColoredPolyline.Stroke = new SolidColorBrush(_traceColor);
      }
    }

    public readonly Polyline ColoredPolyline;
    public readonly Polyline GlowPolyline;
    public readonly Polyline CorePolyline;

    public SpirographTrace()
    {
      ColoredPolyline = new Polyline
      {
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round
      };

      GlowPolyline = new Polyline
      {
        Stroke = new SolidColorBrush(Color.FromArgb(192, 255, 255, 255)),
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        Points = ColoredPolyline.Points
      };

      CorePolyline = new Polyline
      {
        Stroke = new SolidColorBrush(Colors.White),
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        Points = ColoredPolyline.Points
      };

      Drives.CollectionChanged += DrivesOnCollectionChanged;
    }

    private void DrivesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      Reset();
    }

    private void PushPoint(Point newPoint)
    {
      ColoredPolyline.Points.Insert(0, newPoint);

      if (ColoredPolyline.Points.Count > TraceLength)
        ColoredPolyline.Points.RemoveAt(TraceLength);
    }

    private void PushPoint(double x, double y) => PushPoint(new Point(x, y));

    public void Reset()
    {
      ColoredPolyline.Points.Clear();

      foreach (var drive in Drives)
        drive.Reset();
    }

    public void Step(double timeStep)
    {
      if (Drives.Count == 0)
        return;

      var scale = 0.0;
      var xOffset = 0.0;
      var yOffset = 0.0;

      foreach (var drive in Drives)
      {
        drive.Step(timeStep);
        scale += drive.Scale;
        xOffset += drive.XOffset;
        yOffset += drive.YOffset;
      }

      PushPoint((TraceDiameter / 2) * (1 + xOffset / scale), (TraceDiameter / 2) * (1 + yOffset / scale));
    }
  }
}
