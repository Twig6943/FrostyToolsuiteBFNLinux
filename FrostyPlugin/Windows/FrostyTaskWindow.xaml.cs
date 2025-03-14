﻿using FrostyModManager;
using FrostySdk.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shell;

namespace Frosty.Core.Windows
{
    public delegate void FrostyTaskCallback(FrostyTaskLogger logger);
    public delegate void FrostyTaskCancelCallback(FrostyTaskWindow owner);

    /// <summary>
    /// Interaction logic for FrostyTaskWindow.xaml
    /// </summary>
    public partial class FrostyTaskWindow : Window, INotifyPropertyChanged
    {
        private FrostyTaskCallback _callback;

        private FrostyTaskCancelCallback _cancelCallback;

        private double progress;

        private string status;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The progress of the inner task.
        /// </summary>
        public double Progress
        {
            get
            {
                return progress;
            }
            set
            {
                if (value != progress)
                {
                    progress = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The status of the inner task.
        /// </summary>
        public string Status
        {
            get
            {
                return status;
            }
            set
            {
                if (value != status)
                {
                    status = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public FrostyTaskLogger TaskLogger { get; private set; }

        private FrostyTaskWindow(Window owner, string task, string initialStatus, FrostyTaskCallback callback, bool showCancelButton, FrostyTaskCancelCallback cancelCallback = null)
        {
            InitializeComponent();

            taskTextBlock.Text = task;
            Progress = 0.0;
            Status = initialStatus;

            _callback = callback;
            _cancelCallback = cancelCallback;

            Owner = owner;
            TaskLogger = new FrostyTaskLogger(this);
            Loaded += FrostyTaskWindow_Loaded;

            // ensure the current MainWindow has a TaskbarItemInfo assigned
            if (Application.Current.MainWindow.TaskbarItemInfo == null)
            {
                Application.Current.MainWindow.TaskbarItemInfo = new TaskbarItemInfo();
            }

            if (!OperatingSystemHelper.IsWine())
            {
                Application.Current.MainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;

                BindingOperations.SetBinding(Application.Current.MainWindow.TaskbarItemInfo, TaskbarItemInfo.ProgressValueProperty, new Binding("Progress")
                {
                    Converter = new FunctionBasedValueConverter(),
                    ConverterParameter = new Func<object, object>(delegate (object value)
                    {
                        return (double)value / 100.0;
                    }),
                    Source = this,
                });
            }

            if (showCancelButton)
            {
                cancelButton.Visibility = Visibility.Visible;
                if (_cancelCallback != null)
                {
                    cancelButton.Click += CancelButton_Click;

                    if (!OperatingSystemHelper.IsWine())
                    {
                        // register the "Esc" keybinding to the cancel button click event
                        CommandBindings.RegisterKeyBindings(new Dictionary<KeyGesture, ExecutedRoutedEventHandler>
                        {
                            { new KeyGesture(Key.Escape), CancelButton_Click }
                        });
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            cancelButton.IsEnabled = false;
            _cancelCallback(this);
        }

        private void FrostyTaskWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_callback == null)
            {
                return;
            }

            var bw = new BackgroundWorker { WorkerReportsProgress = true };

            bw.DoWork += (s, evt) =>
            {
                try
                {
                    _callback(TaskLogger);
                }
                catch (Exception ex)
                {
                    FileLogger.Init();
                    FileLogger.Info($"Exception in Task Window: {ex}");
                }
            };

            bw.RunWorkerCompleted += (s, evt) =>
            {
                if (!OperatingSystemHelper.IsWine())
                {
                    Application.Current.MainWindow.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                }

                Close();
            };

            bw.RunWorkerAsync();
        }

        public void Update(string status = null, double? progress = null)
        {
            // null is reserved for preserving the current status
            if (status != null)
            {
                Status = status;
            }

            if (progress.HasValue)
            {
                Progress = progress.Value;
            }
        }

        public void SetIndeterminate(bool newIndeterminate)
        {
            if (OperatingSystemHelper.IsWine())
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                taskProgressBar.IsIndeterminate = newIndeterminate;
                Application.Current.MainWindow.TaskbarItemInfo.ProgressState = (newIndeterminate) ? System.Windows.Shell.TaskbarItemProgressState.Indeterminate : System.Windows.Shell.TaskbarItemProgressState.Normal;
            });
        }

        public static void Show(Window owner, string task, string initialStatus, FrostyTaskCallback callback, bool showCancelButton = false, FrostyTaskCancelCallback cancelCallback = null, string cancelButtonName = null)
        {
            FrostyTaskWindow win = new FrostyTaskWindow(owner, task, initialStatus, callback, showCancelButton, cancelCallback);

            if (!string.IsNullOrEmpty(cancelButtonName))
            {
                
            }

            win.ShowDialog();
        }

        public static void Show(string task, string initialStatus, FrostyTaskCallback callback, bool showCancelButton = false, FrostyTaskCancelCallback cancelCallback = null)
        {
            Show(Application.Current.MainWindow, task, initialStatus, callback, showCancelButton, cancelCallback);
        }

        public static FrostyTaskWindow ShowSimple(string task, string initialStatus)
        {
            FrostyTaskWindow win = new FrostyTaskWindow(Application.Current.MainWindow, task, initialStatus, null, false, null);

            win.Show();

            return win;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
