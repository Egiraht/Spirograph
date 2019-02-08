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
  /// <summary>
  /// Spirograph trace model class.
  /// </summary>
  public class TraceModel : INotifyPropertyChanged
  {
    private int _length = 500;
    private float _thickness = 1F;
    private Color _color = Colors.Gold;
    private bool _showDriveCircles = false;
    private bool _fading = true;

    /// <summary>
    /// Trace points queue.
    /// </summary>
    public readonly Collection<Vector2> Points = new Collection<Vector2>();

    /// <summary>
    /// Drives collection.
    /// </summary>
    public ObservableCollection<DriveModel> Drives { get; } = new ObservableCollection<DriveModel>();

    /// <summary>
    /// Maximal trace length in line segments.
    /// </summary>
    public int Length
    {
      get => _length;
      set
      {
        _length = value >= 1 ? value : 1;
        Reset();
        OnPropertyChanged(nameof(Length));
      }
    }

    /// <summary>
    /// Trace visual thickness.
    /// </summary>
    public float Thickness
    {
      get => _thickness;
      set
      {
        _thickness = value;
        OnPropertyChanged(nameof(Thickness));
      }
    }

    /// <summary>
    /// Trace color.
    /// </summary>
    public Color Color
    {
      get => _color;
      set
      {
        _color = value;
        OnPropertyChanged(nameof(Color));
      }
    }

    /// <summary>
    /// Flag that allows drawing of drive circles.
    /// </summary>
    public bool ShowDriveCircles
    {
      get => _showDriveCircles;
      set
      {
        _showDriveCircles = value;
        OnPropertyChanged(nameof(ShowDriveCircles));
      }
    }

    /// <summary>
    /// Trace visual fading flag.
    /// </summary>
    public bool Fading
    {
      get => _fading;
      set
      {
        _fading = value;
        OnPropertyChanged(nameof(Fading));
      }
    }

    /// <summary>
    /// Sum of all drive scales.
    /// </summary>
    private float _fullScale;

    /// <summary>
    /// Trace model constructor.
    /// </summary>
    public TraceModel()
    {
      Drives.CollectionChanged += Drives_OnCollectionChanged;
    }

    /// <summary>
    /// Callback for drives collection change event.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void Drives_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      _fullScale = Drives.Sum(drive => drive.Scale);
      Reset();
    }

    /// <summary>
    /// Pushes a new trace point into the queue and removes old points from its end.
    /// </summary>
    /// <param name="newPoint"></param>
    private void PushTracePoint(Vector2 newPoint)
    {
      if (Points.Count > Length)
      {
        for (var index = Length; index < Points.Count; index++)
          Points.RemoveAt(index);
      }

      Points.Insert(0, newPoint);
    }

    /// <summary>
    /// Resets the trace by cleaning the points queue and resetting the drives.
    /// </summary>
    public void Reset()
    {
      Points.Clear();

      foreach (var drive in Drives)
        drive.Reset();
    }

    /// <summary>
    /// Updates the trace to its state after <i>timeStep</i> seconds from current state.
    /// </summary>
    /// <param name="timeStep">Time step in seconds.</param>
    public void Step(float timeStep)
    {
      if (Drives.Count == 0)
        return;

      var tracePoint = Vector2.Zero;
      foreach (var drive in Drives)
      {
        drive.Step(timeStep);
        tracePoint += drive.OffsetVector / _fullScale;
      }

      PushTracePoint(tracePoint);
    }

    /// <summary>
    /// Saves current trace and/or drives setup to the XML file.
    /// </summary>
    /// <param name="fileName">Path to the XML file.</param>
    /// <param name="saveTraceSetup">If <i>true</i> save also trace setup, otherwise save only drives setup.</param>
    public void SaveToFile(string fileName, bool saveTraceSetup)
    {
      var xmlFile = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("spirograph-trace"));

      // Saving trace setup if needed.
      if (saveTraceSetup)
      {
        xmlFile.Root?.Add(
          new XElement("trace-length", Length),
          new XElement("trace-thickness", Thickness),
          new XElement("trace-color", Color),
          new XElement("trace-fading", Fading),
          new XElement("show-drive-circles", ShowDriveCircles));
      }

      // Saving drives setup.
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

    /// <summary>
    /// Loads trace and drives setup from the XML file.
    /// </summary>
    /// <param name="fileName">Path to the XML file.</param>
    public void LoadFromFile(string fileName)
    {
      if (!File.Exists(fileName))
        return;

      var rootElement = XDocument.Load(fileName).Root;
      if (rootElement == null)
        return;

      // Loading trace setup.
      Length = (int.TryParse(rootElement.Element("trace-length")?.Value, out var traceLength) ? traceLength : Length);
      Thickness =
        (float.TryParse(rootElement.Element("trace-thickness")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var traceThickness)
          ? traceThickness : Thickness);

      try
      {
        Color = (Color) (ColorConverter.ConvertFromString(rootElement.Element("trace-color")?.Value) ?? Color);
      }
      catch
      {
         //
      }

      Fading = (bool.TryParse(rootElement.Element("trace-fading")?.Value, out var traceFading)
        ? traceFading : Fading);

      ShowDriveCircles = (bool.TryParse(rootElement.Element("show-drive-circles")?.Value, out var showDriveCircles)
        ? showDriveCircles : ShowDriveCircles);

      // Loading drives setup.
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
