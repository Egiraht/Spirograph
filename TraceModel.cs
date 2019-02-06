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
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Color = System.Windows.Media.Color;

namespace Spirograph
{
  public class TraceModel : INotifyPropertyChanged
  {
    public readonly Collection<Vector2> Points = new Collection<Vector2>();

    public ObservableCollection<DriveModel> Drives { get; } = new ObservableCollection<DriveModel>();

    private int _traceLength = 200;
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

    private float _traceThickness = 1F;
    public float TraceThickness
    {
      get => _traceThickness;
      set
      {
        _traceThickness = value;
        OnPropertyChanged(nameof(TraceThickness));
      }
    }

    private Color _traceColor = Colors.Gold;
    public Color TraceColor
    {
      get => _traceColor;
      set
      {
        _traceColor = value;
        OnPropertyChanged(nameof(TraceColor));
      }
    }

    // TODO: Return drive circles processing.
    private bool _showDriveCircles;
    public bool ShowDriveCircles
    {
      get => _showDriveCircles;
      set
      {
        _showDriveCircles = value;
        OnPropertyChanged(nameof(ShowDriveCircles));
      }
    }

    public TraceModel()
    {
      Drives.CollectionChanged += Drives_OnCollectionChanged;
    }

    private void Drives_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      Reset();
    }

    private void PushTracePoint(Vector2 newPoint)
    {
      if (Points.Count > TraceLength)
      {
        for (var index = TraceLength; index < Points.Count; index++)
          Points.RemoveAt(index);
      }

      Points.Insert(0, newPoint);
    }

    public void Reset()
    {
      Points.Clear();

      foreach (var drive in Drives)
        drive.Reset();
    }

    public void Step(float timeStep)
    {
      if (Drives.Count == 0)
        return;

      var fullScale = Drives.Sum(drive => drive.Scale);
      var tracePoint = Vector2.Zero;

      foreach (var drive in Drives)
      {
        drive.Step(timeStep);
        tracePoint += drive.OffsetVector / fullScale;
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
        (float.TryParse(rootElement.Element("trace-thickness")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var traceThickness)
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
        var drive = new DriveModel();
        drive.Frequency = (float.TryParse(driveElement.Element("frequency")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var frequency)
          ? frequency : drive.Frequency);
        drive.Scale = (float.TryParse(driveElement.Element("scale")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var scale)
          ? scale : drive.Scale);
        drive.StartAngle =
          (float.TryParse(driveElement.Element("start-angle")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var startAngle)
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
