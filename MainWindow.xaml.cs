using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;


namespace Spirograph
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    private static int _driveIndex = 1;
    private static int DriveIndex => _driveIndex++;

    private OpenFileDialog _openFileDialog = new OpenFileDialog
    {
      Title = "Load spirograph setup",
      Filter = "Spirograph setup file|*.xml",
      Multiselect = false,
      CheckFileExists = true,
      CheckPathExists = true,
      AddExtension = true,
      DefaultExt = "*.xml",
      FileName = "",
      DereferenceLinks = true,
      ShowReadOnly = true
    };

    private SaveFileDialog _saveFileDialog = new SaveFileDialog
    {
      Title = "Load spirograph setup",
      Filter = "Spirograph setup file|*.xml",
      CheckFileExists = false,
      CheckPathExists = true,
      AddExtension = true,
      DefaultExt = "*.xml",
      FileName = "",
      DereferenceLinks = true,
      CreatePrompt = false,
      OverwritePrompt = true
    };

    public SpirographTrace Trace { get; } = new SpirographTrace();

    public MainWindow()
    {
      Dispatcher.UnhandledException += Dispatcher_OnUnhandledException;

      InitializeComponent();

      Trace.Drives.Add(new SpirographDrive {Name = $"Drive {DriveIndex}"});

      DrivesList.SelectedIndex = 0;

      Viewport.Children.Add(Trace.DriveCircles);
      Viewport.Children.Add(Trace.GlowPolyline);
      Viewport.Children.Add(Trace.ColoredPolyline);
      Viewport.Children.Add(Trace.CorePolyline);

      var timer = new DispatcherTimer();
      timer.Interval = TimeSpan.FromMilliseconds(25);
      timer.Tick += Timer_OnTick;
      timer.Start();
    }

    private void Dispatcher_OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
      e.Handled = true;
      MessageBox.Show(e.Exception.Message, SpirographWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
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

      Trace.Drives.Add(new SpirographDrive
      {
        Name = $"Drive {DriveIndex}",
        Frequency = 0.1 + 0.1 * random.Next(10),
        StartAngle = 10.0 * random.Next(0, 36),
        Scale = 0.1 + 0.1 * random.Next(10),
        RotateCcw = random.Next(2) > 0
      });

      DrivesList.SelectedIndex = Trace.Drives.Count - 1;

      RemoveButton.IsEnabled = true;
    }

    private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (Trace.Drives.Count < 2)
        return;

      var index = DrivesList.SelectedIndex;
      Trace.Drives.RemoveAt(index);
      DrivesList.SelectedIndex = index < Trace.Drives.Count ? index : index - 1;

      if (Trace.Drives.Count < 2)
        RemoveButton.IsEnabled = false;
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
      Trace.Reset();
    }

    private void LoadButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (!(bool) _openFileDialog.ShowDialog(this))
        return;

      Trace.LoadFromFile(_openFileDialog.FileName);
      DrivesList.SelectedIndex = 0;
      RemoveButton.IsEnabled = Trace.Drives.Count >= 2;
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (!(bool) _saveFileDialog.ShowDialog(this))
        return;

      Trace.SaveToFile(_saveFileDialog.FileName);
    }

    private void DriveName_OnSourceUpdated(object sender, DataTransferEventArgs e)
    {
      var index = DrivesList.SelectedIndex;
      BindingOperations.ClearBinding(DrivesList, ItemsControl.ItemsSourceProperty);
      BindingOperations.SetBinding(DrivesList, ItemsControl.ItemsSourceProperty, new Binding {Source = Trace.Drives});
      DrivesList.SelectedIndex = index;
    }
  }
}
