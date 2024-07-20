using System;
using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Controls;

namespace McHMR_Updater_v2
{
    public partial class GlobalExceptionHandleWindow : FluentWindow
    {
        private readonly string _errorText;
        private readonly Exception _exception;

        public GlobalExceptionHandleWindow(Exception exception)
        {
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
            };

            _exception = exception;

            _errorText = exception.Message + "\r\n" + exception + "\r\n" + exception.Data + "\r\n" + exception.StackTrace;
            errorText.Text += exception.Message + "\r\n" + exception + "\r\n" + exception.Data + "\r\n" + exception.StackTrace;
            errorTitle.Title = exception.Message;
        }

        private void copyBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_errorText);
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_exception.Message.Equals("网络未连接"))
            {
                Process.GetCurrentProcess().Kill();
            }
            this.Close();
        }
    }
}
