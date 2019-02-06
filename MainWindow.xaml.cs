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
    private static readonly Version ProgramVersion = AssemblyName.GetAssemblyName(Assembly.GetExecutingAssembly().Location).Version;

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

    public TraceModel Trace { get; } = new TraceModel();

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

      if (File.Exists(_startupFile))
        Trace.LoadFromFile(_startupFile);
    }

    private void Dispatcher_OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
      e.Handled = true;
      MessageBox.Show(e.Exception.Message, SpirographWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void TraceViewportContainer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
      TraceViewport.Width = ((FrameworkElement) TraceViewport.Parent).ActualWidth;
      TraceViewport.Height = ((FrameworkElement) TraceViewport.Parent).ActualHeight;
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

      Trace.Drives.Add(new DriveModel
      {
        Frequency = 0.1F + 0.1F * random.Next(10),
        StartAngle = 10F * random.Next(0, 36),
        Scale = 0.1F + 0.1F * random.Next(10),
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
      Trace.Drives.Add(new DriveModel());
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

      TraceViewport.Dispose();
    }
  }
}
