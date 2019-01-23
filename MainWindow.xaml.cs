using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
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
    private readonly string _startupFile =
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spirograph", "Startup.xml");

    private readonly OpenFileDialog _openFileDialog = new OpenFileDialog
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

    private readonly SaveFileDialog _saveFileDialog = new SaveFileDialog
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

      Trace.Drives.CollectionChanged += TraceDrives_OnCollectionChanged;
      Trace.Drives.Add(new SpirographDrive());

      if (File.Exists(_startupFile))
        Trace.LoadFromFile(_startupFile);

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

    private void ControlPanel_OnKeyDown(object sender, KeyEventArgs e)
    {
      if (!(e.OriginalSource is TextBox) || e.Key != Key.Enter)
        return;

      ((TextBox) e.OriginalSource).GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private void ControlPanel_OnSourceUpdated(object sender, DataTransferEventArgs e)
    {
      var index = DrivesList.SelectedIndex;
      BindingOperations.ClearBinding(DrivesList, ItemsControl.ItemsSourceProperty);
      BindingOperations.SetBinding(DrivesList, ItemsControl.ItemsSourceProperty, new Binding {Source = Trace.Drives});
      DrivesList.SelectedIndex = index;
    }

    private void TraceDrives_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      RemoveButton.IsEnabled = DrivesList.Items.Count > 1;

      if (DrivesList.HasItems && DrivesList.SelectedIndex == -1)
        DrivesList.SelectedIndex = 0;
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
      var random = new Random();

      Trace.Drives.Add(new SpirographDrive
      {
        Frequency = 0.1 + 0.1 * random.Next(10),
        StartAngle = 10.0 * random.Next(0, 36),
        Scale = 0.1 + 0.1 * random.Next(10),
        RotateCcw = random.Next(2) > 0
      });

      DrivesList.SelectedIndex = Trace.Drives.Count - 1;
    }

    private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (Trace.Drives.Count < 2)
        return;

      var index = DrivesList.SelectedIndex;
      Trace.Drives.RemoveAt(index);
      DrivesList.SelectedIndex = index < Trace.Drives.Count ? index : index - 1;
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
      Trace.Reset();
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
      Trace.Drives.Clear();
      Trace.Drives.Add(new SpirographDrive());
    }

    private void LoadButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (!(bool) _openFileDialog.ShowDialog(this))
        return;

      Trace.LoadFromFile(_openFileDialog.FileName);
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (!(bool) _saveFileDialog.ShowDialog(this))
        return;

      Trace.SaveToFile(_saveFileDialog.FileName, false);
    }

    private void MainWindow_OnClosing(object sender, CancelEventArgs e)
    {
      // ReSharper disable once AssignNullToNotNullAttribute
      if (!Directory.Exists(Path.GetDirectoryName(_startupFile)))
        Directory.CreateDirectory(Path.GetDirectoryName(_startupFile));

      Trace.SaveToFile(_startupFile, true);
    }
  }
}
