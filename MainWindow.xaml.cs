using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Spirograph
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    public SpirographTrace Trace { get; } = new SpirographTrace();

    public MainWindow()
    {
      Trace.TraceDiameter = 100;
      Trace.TraceLength = 2001;
      Trace.TraceThickness = 1;
      Trace.TraceColor = Colors.Gold;
      Trace.Drives.Add(new SpirographDrive("- Base drive -"));

      InitializeComponent();

      DrivesList.SelectedIndex = 0;

      Viewport.Children.Add(Trace.ColoredPolyline);
      Viewport.Children.Add(Trace.GlowPolyline);
      Viewport.Children.Add(Trace.CorePolyline);

      var timer = new DispatcherTimer();
      timer.Interval = TimeSpan.FromMilliseconds(25);
      timer.Tick += Timer_OnTick;
      timer.Start();
    }

    private void Timer_OnTick(object sender, EventArgs e)
    {
      Trace.Step(0.005);
    }

    private void TextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
      if (!(sender is TextBox) || e.Key != Key.Enter)
        return;

      ((TextBox) sender).GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
      var random = new Random();

      Trace.Drives.Insert(0, new SpirographDrive($"Drive {Trace.Drives.Count}")
      {
        Frequency = 0.1 + 0.1 * random.Next(10),
        StartAngle = 10.0 * random.Next(0, 36),
        Scale = 0.1 + 0.1 * random.Next(10),
        RotateCcw = random.Next(2) > 0
      });

      DrivesList.SelectedIndex = 0;

      RemoveButton.IsEnabled = true;
    }

    private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (Trace.Drives.Count < 2)
        return;

      Trace.Drives.RemoveAt(0);

      if (DrivesList.SelectedIndex == -1)
        DrivesList.SelectedIndex = 0;

      if (Trace.Drives.Count < 2)
        RemoveButton.IsEnabled = false;
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
      Trace.Reset();
    }
  }
}
