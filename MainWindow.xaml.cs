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
using System.Windows.Markup;
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
        private DockPanel? _lastDockInput;
        
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
            
            CommandManager.Close += Close;
            CommandManager.ChangeDir += path => { LPath.Content = path; };
            CommandManager.SetNewNameUser += username =>
            {
                LUserNameBottom.Content = $"👤 {username}@root";
                LNameUserTop.Content = $"{username}@root";
            };
            
            CommandManager.SetStartTime(DateTime.Now);
            
            // TODO: Parse from args
            CommandManager.SetNameUser("root");

            AddNewDockInput();
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            if (e.Key != Key.Enter) return;

            var command = textBox.Text;
            
            var result = CommandManager.Execute(command);
            if (string.IsNullOrEmpty(result)) return;
            
            var lOutput = CloneLOutput(LOutput);
            
            if (lOutput != null)
            {
                lOutput.Visibility = Visibility.Visible;
                lOutput.Content = result;
                
                DockAllText.Children.Add(lOutput);
                DockPanel.SetDock(lOutput, Dock.Top);
            }
            
            AddNewDockInput();
        }

        private void AddNewDockInput()
        {
            var dockInput = CloneDockInput(DockInput);

            if (dockInput != null)
            {
                RemoveFocus();

                dockInput.Visibility = Visibility.Visible;
                
                DockAllText.Children.Add(dockInput);
                DockPanel.SetDock(dockInput, Dock.Top);
                
                var tbInput = GetTextboxDockInput(dockInput);

                if (tbInput != null)
                {
                    tbInput.Focusable = true;
                    tbInput.Focus();
                    tbInput.KeyDown += UIElement_OnKeyDown;
                }

                _lastDockInput = dockInput;
            }
        }
        
        private Label? CloneLOutput(Label lOutput)
        {
            return XamlReader.Parse(XamlWriter.Save(lOutput)) as Label;
        }
        
        private DockPanel? CloneDockInput(DockPanel dockInput)
        {
            return XamlReader.Parse(XamlWriter.Save(dockInput)) as DockPanel;
        }

        private void RemoveFocus()
        {
            if (_lastDockInput == null) return;
            
            for (var i = 0; i < _lastDockInput.Children.Count; i++)
            {
               
                if (_lastDockInput.Children[i] is TextBox textBox)
                {
                    textBox.Focusable = false;
                    Keyboard.ClearFocus();   
                    textBox.KeyDown -= UIElement_OnKeyDown;
                }
            }
        }
        
        private Label? GetLabelDockInput(DockPanel dockPanel)
        {
            for (var i = 0; i < dockPanel.Children.Count; i++)
            {
                if (dockPanel.Children[i] is Label label)
                {
                    return label;
                }
            }

            return null;
        }
        
        private TextBox? GetTextboxDockInput(DockPanel dockPanel)
        {
            for (var i = 0; i < dockPanel.Children.Count; i++)
            {
                if (dockPanel.Children[i] is TextBox textBox)
                {
                    return textBox;
                }
            }

            return null;
        }
    }
}