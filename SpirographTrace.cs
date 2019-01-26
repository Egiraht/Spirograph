/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/ .
 *
 * Copyright (C) 2019 Maxim Yudin <stibiu@yandex.ru>.
 */

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using Path = System.Windows.Shapes.Path;

namespace Spirograph
{
  public class SpirographTrace : INotifyPropertyChanged
  {
    private readonly PointCollection _points;

    public ObservableCollection<SpirographDrive> Drives { get; } = new ObservableCollection<SpirographDrive>();

    private double _traceDiameter = 100.0;
    public double TraceDiameter
    {
      get => _traceDiameter;
      set
      {
        _traceDiameter = value;
        Reset();

        OnPropertyChanged(nameof(TraceDiameter));
      }
    }

    private int _traceLength = 1000;
    public int TraceLength
    {
      get => _traceLength;
      set
      {
        _traceLength = value >= 1 ? value : 1;
        Reset();

        OnPropertyChanged(nameof(TraceLength));
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
        CorePolyline.StrokeThickness = _traceThickness * 0.4;

        OnPropertyChanged(nameof(TraceThickness));
      }
    }

    private Color _traceColor;
    public Color TraceColor
    {
      get => _traceColor;
      set
      {
        _traceColor = value;
        ((SolidColorBrush) ColoredPolyline.Stroke).Color = _traceColor;
        ((SolidColorBrush) CorePolyline.Stroke).Color = _traceColor + Color.FromArgb(0, 192, 192, 192);

        OnPropertyChanged(nameof(TraceColor));
      }
    }

    private bool _showDriveCircles;
    public bool ShowDriveCircles
    {
      get => _showDriveCircles;
      set
      {
        _showDriveCircles = value;
        DriveCircles.Visibility = _showDriveCircles ? Visibility.Visible : Visibility.Collapsed;

        OnPropertyChanged(nameof(ShowDriveCircles));
      }
    }

    public readonly Polyline ColoredPolyline = new Polyline
    {
      Stroke = new SolidColorBrush(),
      StrokeStartLineCap = PenLineCap.Round,
      StrokeEndLineCap = PenLineCap.Round,
      StrokeLineJoin = PenLineJoin.Round
    };

    public readonly Polyline CorePolyline = new Polyline
    {
      Stroke = new SolidColorBrush(),
      StrokeStartLineCap = PenLineCap.Round,
      StrokeEndLineCap = PenLineCap.Round,
      StrokeLineJoin = PenLineJoin.Round
    };

    public readonly Path DriveCircles = new Path()
    {
      Data = new GeometryGroup(),
      Stroke = new SolidColorBrush(Colors.DimGray),
      StrokeStartLineCap = PenLineCap.Round,
      StrokeEndLineCap = PenLineCap.Round,
      StrokeLineJoin = PenLineJoin.Round,
      StrokeThickness = 0.2
    };

    public SpirographTrace()
    {
      _points = new PointCollection(_traceLength);
      ColoredPolyline.Points = _points;
      CorePolyline.Points = _points;

      TraceThickness = 1.0;
      TraceColor = Colors.Gold;
      ShowDriveCircles = false;

      Drives.CollectionChanged += Drives_OnCollectionChanged;
    }

    private void Drives_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      Reset();

      var driveCircles = (GeometryGroup) DriveCircles.Data;
      driveCircles.Children.Clear();
      foreach (var drive in Drives)
        driveCircles.Children.Add(drive.DriveCircle);
    }

    private void PushTracePoint(Point newPoint)
    {
      if (_points.Count >= TraceLength)
      {
        for (var index = TraceLength - 1; index < _points.Count; index++)
          _points.RemoveAt(index);
      }

      _points.Insert(0, newPoint);
    }

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

      var fullScale = Drives.Sum(drive => drive.Scale);
      var tracePoint = new Point(_traceDiameter / 2, _traceDiameter / 2);

      foreach (var drive in Drives)
      {
        drive.DriveCircleOffset.X = tracePoint.X;
        drive.DriveCircleOffset.Y = tracePoint.Y;
        drive.DriveCircleScale.CenterX = tracePoint.X;
        drive.DriveCircleScale.CenterY = tracePoint.Y;
        drive.DriveCircleScale.ScaleX = (_traceDiameter / 2) / fullScale;
        drive.DriveCircleScale.ScaleY = (_traceDiameter / 2) / fullScale;

        drive.Step(timeStep);

        tracePoint += drive.OffsetVector * (_traceDiameter / 2) / fullScale;
      }

      PushTracePoint(tracePoint);
    }

    public void SaveToFile(string fileName, bool saveTraceSetup)
    {
      var xmlFile = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("spirograph-trace"));

      if (saveTraceSetup)
      {
        xmlFile.Root?.Add(
          new XElement("trace-length", TraceLength),
          new XElement("trace-thickness", TraceThickness),
          new XElement("trace-color", TraceColor),
          new XElement("show-drive-circles", ShowDriveCircles));
      }

      var drivesElement = new XElement("drives");
      foreach (var drive in Drives)
      {
        drivesElement.Add(
          new XElement("drive",
            new XElement("frequency", drive.Frequency),
            new XElement("scale", drive.Scale),
            new XElement("start-angle", drive.StartAngle),
            new XElement("rotate-ccw", drive.RotateCcw)));
      }
      xmlFile.Root?.Add(drivesElement);

      xmlFile.Save(fileName);
    }

    public void LoadFromFile(string fileName)
    {
      if (!File.Exists(fileName))
        return;

      var rootElement = XDocument.Load(fileName).Root;
      if (rootElement == null)
        return;

      TraceLength = (int.TryParse(rootElement.Element("trace-length")?.Value, out var traceLength) ? traceLength : TraceLength);
      TraceThickness =
        (double.TryParse(rootElement.Element("trace-thickness")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var traceThickness)
          ? traceThickness : TraceThickness);

      try
      {
        TraceColor = (Color) (ColorConverter.ConvertFromString(rootElement.Element("trace-color")?.Value) ?? TraceColor);
      }
      catch
      {
         //
      }

      ShowDriveCircles = (bool.TryParse(rootElement.Element("show-drive-circles")?.Value, out var showDriveCircles)
        ? showDriveCircles : ShowDriveCircles);

      if (!(rootElement.Element("drives")?.HasElements ?? false))
        return;

      Drives.CollectionChanged -= Drives_OnCollectionChanged;
      Drives.Clear();
      foreach (var driveElement in rootElement.Element("drives")?.Descendants("drive") ?? new XElement[0])
      {
        var drive = new SpirographDrive();
        drive.Frequency = (double.TryParse(driveElement.Element("frequency")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var frequency)
          ? frequency : drive.Frequency);
        drive.Scale = (double.TryParse(driveElement.Element("scale")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var scale)
          ? scale : drive.Scale);
        drive.StartAngle =
          (double.TryParse(driveElement.Element("start-angle")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var startAngle)
            ? startAngle : drive.StartAngle);
        drive.RotateCcw = (bool.TryParse(driveElement.Element("rotate-ccw")?.Value, out var rotateCcw) ? rotateCcw : drive.RotateCcw);

        Drives.Add(drive);
      }
      Drives_OnCollectionChanged(this, null);
      Drives.CollectionChanged += Drives_OnCollectionChanged;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
