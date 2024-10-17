using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LabConfig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LTime.Content = "🕘 " + DateTime.Now.ToString("HH:mm:ss");
                    LDate.Content = "📅 " + DateTime.Now.ToString("yyyy-MM-dd");
                }, DispatcherPriority.Background);
            };
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            
            TbInput.Focusable = true;
            TbInput.Focus();
            
            RtbHistory.Document.Blocks.Clear();
            
            CommandManager.Close += Close;
            CommandManager.ChangeDir += path => { LPath.Content = path; };
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            RtbHistory.MaxHeight = DockAllText.ActualHeight - DockInput.ActualHeight;
            RtbHistory.ScrollToEnd();
        }

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RtbHistory.MaxHeight = DockAllText.ActualHeight - DockInput.ActualHeight;
        }

        private void RtbHistory_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not RichTextBox richTextBox) return;

            richTextBox.Visibility = richTextBox.Document.Blocks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            if (e.Key != Key.Enter) return;

            var command = textBox.Text;
            textBox.Text = string.Empty;
            
            var result = CommandManager.Execute(command);
            if (string.IsNullOrEmpty(result)) return;
            
            if (RtbHistory.Document.Blocks.Count > 0)
                RtbHistory.AppendText(Environment.NewLine);
            
            RtbHistory.AppendText(result);
        }
    }
}