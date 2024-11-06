using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            dispatcherTimer.Tick += (_, _) =>
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
            CommandManager.SetNewNameUser += username =>
            {
                LUserNameBottom.Content = $"👤 {username}@root";
                LNameUserTop.Content = $"{username}@root";
            };
            CommandManager.ActivateInputField += () =>
            {
                var dockInput = CloneDockInput(DpInputText);
                
                if (dockInput == null)
                    return;
                
                RemoveFocus();
                
                dockInput.Visibility = Visibility.Visible;
                
                DockAllText.Children.Add(dockInput);
                DockPanel.SetDock(dockInput, Dock.Top);

                var richTextBox = GetDockInput<RichTextBox>(dockInput);

                if (richTextBox != null)
                {
                    richTextBox.Focusable = true;
                    richTextBox.Focus();
                    richTextBox.KeyDown += InputText_OnKeyDown;
                }

                _lastDockInput = dockInput;
            };
            
            CommandManager.SetStartTime(DateTime.Now);
            
            // TODO: Parse from args
            CommandManager.NameUser = "root";
            CommandManager.PathArchive = "C:\\Users\\rund2\\Downloads\\archive.zip";

            AddNewDockInput();
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            if (e.Key != Key.Enter) return;

            var command = textBox.Text.TrimStart(' ');

            if (CommandManager.SpecialCommand(command))
            {
                CommandManager.ExecuteSpecialCommand(command);
                return;
            }

            if (command.StartsWith("ls"))
            {
                var res = CommandManager.LsCommand(command);
                
                if (res is { Count: > 0 })
                {
                    var tbOutput = CloneTextBlock(TbOutput);
            
                    if (tbOutput != null)
                    {
                        tbOutput.Visibility = Visibility.Visible;
                        tbOutput.Inlines.Clear();

                        if (res.Count == 1 && !res[0].isDir)
                            tbOutput.Text = res[0].name;
                        else if (command.StartsWith("ls -l") || command.StartsWith("ls --long"))
                        {
                            res.ForEach(x =>
                            {
                                tbOutput.Inlines.Add(
                                    new Run(x.isDir ? "d" : "-")
                                    {
                                        Foreground = !x.isDir ? Brushes.White : (Brush)new BrushConverter().ConvertFrom("#3b78ff")!
                                    }
                                );
                                tbOutput.Inlines.Add(
                                    new Run(string.Join(' ', x.name.Split()[..^1]) + " ")
                                    {
                                        Foreground = Brushes.White
                                    }
                                );
                                tbOutput.Inlines.Add(
                                    new Run(x.name.Split()[^1] + (res[^1] == x ? "" : "\n"))
                                    {
                                        Foreground = !x.isDir ? Brushes.White : (Brush)new BrushConverter().ConvertFrom("#3b78ff")!
                                    }
                                );
                            });
                        }
                        else
                        {
                            res.ForEach(x =>
                            {
                                tbOutput.Inlines.Add(
                                    new Run(x.name + "  ")
                                    {
                                        Foreground = !x.isDir ? Brushes.White : (Brush)new BrushConverter().ConvertFrom("#3b78ff")!
                                    });
                            }); 
                        }

                        DockAllText.Children.Add(tbOutput);
                        DockPanel.SetDock(tbOutput, Dock.Top);
                    }
                }
                
                AddNewDockInput();
            
                ScrollAllTest.ScrollToEnd();
                return;
            }
            
            var result = CommandManager.Execute(command);

            if (!string.IsNullOrEmpty(result))
            {
                var tbOutput = CloneTextBlock(TbOutput);
            
                if (tbOutput != null)
                {
                    tbOutput.Visibility = Visibility.Visible;
                    tbOutput.Text = result;
                
                    DockAllText.Children.Add(tbOutput);
                    DockPanel.SetDock(tbOutput, Dock.Top);
                }
            }

            AddNewDockInput();
            
            ScrollAllTest.ScrollToEnd();
        }
        
        private void InputText_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not RichTextBox richTextBox) return;
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || e.Key != Key.D) return;

            var command = GetRichTextBoxText(richTextBox);
            
            var result = CommandManager.WcCommand(command);
            if (string.IsNullOrEmpty(result)) return;
            
            var tbOutput = CloneTextBlock(TbOutput);
            
            if (tbOutput != null)
            {
                tbOutput.Visibility = Visibility.Visible;
                tbOutput.Text = result;
                
                DockAllText.Children.Add(tbOutput);
                DockPanel.SetDock(tbOutput, Dock.Top);
            }
            
            AddNewDockInput();
            
            ScrollAllTest.ScrollToEnd();
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
                
                var tbInput = GetDockInput<TextBox>(dockInput);

                if (tbInput != null)
                {
                    tbInput.Focusable = true;
                    tbInput.Focus();
                    tbInput.KeyDown += UIElement_OnKeyDown;
                }

                var warpPanel = GetDockInput<WrapPanel>(dockInput);

                if (warpPanel != null)
                {
                    var lPath = GetDockInput<Label>(warpPanel);

                    if (lPath != null)
                        lPath.Content = CommandManager.CurrDir;
                }

                _lastDockInput = dockInput;
            }
        }
        
        private Label? CloneLabel(Label label)
        {
            return XamlReader.Parse(XamlWriter.Save(label)) as Label;
        }
        
        private TextBlock? CloneTextBlock(TextBlock textBlock) => XamlReader.Parse(XamlWriter.Save(textBlock)) as TextBlock;
        
        private RichTextBox? CloneRichTextBox(RichTextBox richTextBox)
        {
            return XamlReader.Parse(XamlWriter.Save(richTextBox)) as RichTextBox;
        }
        
        private DockPanel? CloneDockInput(Panel panel)
        {
            return XamlReader.Parse(XamlWriter.Save(panel)) as DockPanel;
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
                
                if (_lastDockInput.Children[i] is RichTextBox richTextBox)
                {
                    richTextBox.Focusable = false;
                    Keyboard.ClearFocus();   
                    richTextBox.KeyDown -= UIElement_OnKeyDown;
                }
            }
        }
        
        private T? GetDockInput<T>(Panel panel)
        {
            for (var i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is T t)
                {
                    return t;
                }
            }

            return default;
        }
        
        private static string GetRichTextBoxText(RichTextBox richTextBox)
        {
            var textRange = new TextRange(
                richTextBox.Document.ContentStart, 
                richTextBox.Document.ContentEnd
            );
            return textRange.Text;
        }
    }
}