/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/ .
 *
 * Copyright (C) 2019 Maxim Yudin <stibiu@yandex.ru>.
 */

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
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
    /// <summary>
    /// Program's version.
    /// </summary>
    private static readonly Version ProgramVersion = AssemblyName.GetAssemblyName(Assembly.GetExecutingAssembly().Location).Version;

    /// <summary>
    /// Startup XML file path.
    /// </summary>
    private static readonly string StartupFile =
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spirograph", "Startup.xml");

    /// <summary>
    /// XML file loading dialog.
    /// </summary>
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

    /// <summary>
    /// XML file saving dialog.
    /// </summary>
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

    /// <summary>
    /// Trace model instance bound to the main window.
    /// </summary>
    public TraceModel Trace { get; } = new TraceModel();

    /// <summary>
    /// Main window constructor.
    /// Loads setup from the XML file located by the path in <see cref="StartupFile"/>.
    /// </summary>
    public MainWindow()
    {
      #if !DEBUG
        Dispatcher.UnhandledException += Dispatcher_OnUnhandledException;
      #endif

      InitializeComponent();

      Title = $"Spirograph v{ProgramVersion.ToString(2)}";

      TraceViewport.Trace = Trace;

      Trace.Drives.CollectionChanged += TraceDrives_OnCollectionChanged;
      Trace.Drives.Add(new DriveModel());

      if (File.Exists(StartupFile))
        Trace.LoadFromFile(StartupFile);
    }

    /// <summary>
    /// Default exception handling routine.
    /// Shows the dialog with error message.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void Dispatcher_OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
      e.Handled = true;
      MessageBox.Show(e.Exception.Message, SpirographWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// Trace rendering viewport container resizing callback.
    /// Resizes the viewport to fit the window.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void TraceViewportContainer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
      TraceViewport.Width = ((FrameworkElement) TraceViewport.Parent).ActualWidth;
      TraceViewport.Height = ((FrameworkElement) TraceViewport.Parent).ActualHeight;
    }

    /// <summary>
    /// Control panel key press callback.
    /// Updates focused property's value on pressing <see cref="Key.Enter" />.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void ControlPanel_OnKeyDown(object sender, KeyEventArgs e)
    {
      if (!(e.OriginalSource is TextBox) || e.Key != Key.Enter)
        return;

      ((TextBox) e.OriginalSource).GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    /// <summary>
    /// Control panel data source update callback.
    /// Updates items of the <see cref="DrivesList" />.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void ControlPanel_OnSourceUpdated(object sender, DataTransferEventArgs e)
    {
      var index = DrivesList.SelectedIndex;
      BindingOperations.ClearBinding(DrivesList, ItemsControl.ItemsSourceProperty);
      BindingOperations.SetBinding(DrivesList, ItemsControl.ItemsSourceProperty, new Binding {Source = Trace.Drives});
      DrivesList.SelectedIndex = index;
    }

    /// <summary>
    /// Drives collection change callback.
    /// Updates <see cref="ResetButton" /> state and <see cref="DrivesList" /> current selection.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void TraceDrives_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      RemoveButton.IsEnabled = DrivesList.Items.Count > 1;

      if (DrivesList.HasItems && DrivesList.SelectedIndex == -1)
        DrivesList.SelectedIndex = 0;
    }

    /// <summary>
    /// <see cref="AddButton" /> click callback.
    /// Adds a drive to the <see cref="DrivesList" />.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
      var random = new Random();

      Trace.Drives.Add(new DriveModel
      {
        Frequency = 0.1F + 0.1F * random.Next(10),
        StartAngle = 10F * random.Next(0, 36),
        Scale = 0.1F + 0.1F * random.Next(10),
        RotateCcw = random.Next(2) > 0
      });

      DrivesList.SelectedIndex = Trace.Drives.Count - 1;
    }

    /// <summary>
    /// <see cref="RemoveButton" /> click callback.
    /// Removes current selected drive from the <see cref="DrivesList" />.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (Trace.Drives.Count < 2)
        return;

      var index = DrivesList.SelectedIndex;
      Trace.Drives.RemoveAt(index);
      DrivesList.SelectedIndex = index < Trace.Drives.Count ? index : index - 1;
    }

    /// <summary>
    /// <see cref="ResetButton" /> click callback.
    /// Resets the state of the trace.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
      Trace.Reset();
    }

    /// <summary>
    /// <see cref="ClearButton" /> click callback.
    /// Clears the drives setup to default.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
      Trace.Drives.Clear();
      Trace.Drives.Add(new DriveModel());
    }

    /// <summary>
    /// <see cref="LoadButton" /> click callback.
    /// Opens XML file loading dialog and loads setup from the selected file.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void LoadButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (!(bool) _openFileDialog.ShowDialog(this))
        return;

      Trace.LoadFromFile(_openFileDialog.FileName);
    }

    /// <summary>
    /// <see cref="SaveButton" /> click callback.
    /// Opens XML file saving dialog and saves current setup to the file.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
      if (!(bool) _saveFileDialog.ShowDialog(this))
        return;

      Trace.SaveToFile(_saveFileDialog.FileName, false);
    }

    /// <summary>
    /// Main window closing callback.
    /// Saves current setup to the XML file specified by <see cref="StartupFile"/> and terminates the trace model updating thread.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Events arguments.</param>
    private void MainWindow_OnClosing(object sender, CancelEventArgs e)
    {
      // ReSharper disable once AssignNullToNotNullAttribute
      if (!Directory.Exists(Path.GetDirectoryName(StartupFile)))
        Directory.CreateDirectory(Path.GetDirectoryName(StartupFile));

      Trace.SaveToFile(StartupFile, true);

      TraceViewport.Dispose();

      Dispatcher.UnhandledException -= Dispatcher_OnUnhandledException;
    }
  }
}
