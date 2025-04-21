using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RotatableDie.Converters;
using RotatableDie.Models;

namespace RotatableDie.UI
{
    /// <summary>
    /// Manages UI controls for the die application
    /// </summary>
    public class UIControlsManager
    {
        private readonly Window _window;
        private ComboBox? _colorComboBox;
        
        public event EventHandler<DieTypeChangedEventArgs>? DieTypeChanged;
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;
        
        public UIControlsManager(Window window)
        {
            _window = window;
        }
        
        public void SetupControls()
        {
            // Get or create the control panel first
            StackPanel controlPanel = GetOrCreateControlPanel();
            
            // Add both combo boxes to the control panel
            AddDieTypeComboBox(controlPanel);
            AddColorComboBox(controlPanel);
            
            // Populate the color combo box with color options
            PopulateColorComboBox();
        }
        
        private StackPanel GetOrCreateControlPanel()
        {
            // Try to get the existing control panel from XAML
            StackPanel? controlPanel = _window.FindName("controlPanel") as StackPanel;
            
            // If the panel doesn't exist, create it and add it to the Grid
            if (controlPanel == null)
            {
                controlPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(10, 5, 10, 5),
                    Name = "controlPanel"
                };
                
                Grid? mainGrid = _window.Content as Grid;
                if (mainGrid != null)
                {
                    controlPanel.SetValue(Grid.RowProperty, 0);
                    mainGrid.Children.Add(controlPanel);
                }
            }
            else
            {
                // Clear any existing controls if we're reusing the panel
                controlPanel.Children.Clear();
            }
            
            return controlPanel;
        }
        
        private void AddDieTypeComboBox(StackPanel controlPanel)
        {
            // Create the die type label
            TextBlock label = new TextBlock
            {
                Text = "Die Type:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            
            // Create the die type combo box
            ComboBox dieTypeComboBox = new ComboBox
            {
                Width = 120,
                Margin = new Thickness(0, 0, 20, 0),
                Name = "dieTypeComboBox"
            };
            dieTypeComboBox.SelectionChanged += DieTypeComboBox_SelectionChanged;
            
            // Add controls to panel
            controlPanel.Children.Add(label);
            controlPanel.Children.Add(dieTypeComboBox);
            
            // Populate the ComboBox
            var dieTypes = Enum.GetValues<DieType>()
                .Select(type => new DieTypeItem { Type = type, Name = type.ToString() })
                .ToList();
                
            dieTypeComboBox.DisplayMemberPath = "Name";
            dieTypeComboBox.ItemsSource = dieTypes;
            dieTypeComboBox.SelectedIndex = 1; // Default to Cube
        }
        
        private void AddColorComboBox(StackPanel controlPanel)
        {
            // Create the color label
            TextBlock colorLabel = new TextBlock
            {
                Text = "Die Color:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            
            // Create the color combo box
            ComboBox colorComboBox = new ComboBox
            {
                Width = 120,
                Margin = new Thickness(0, 0, 0, 0),
                Name = "colorComboBox"
            };
            
            // Register event handler
            colorComboBox.SelectionChanged += ColorComboBox_SelectionChanged;
            
            // Add controls to panel
            controlPanel.Children.Add(colorLabel);
            controlPanel.Children.Add(colorComboBox);
            
            // Store reference for later use in PopulateColorComboBox
            _colorComboBox = colorComboBox;
        }

        private void PopulateColorComboBox()
        {
            if (_colorComboBox == null) return;
            
            // Create a simple list of colors
            List<SolidColorBrush> colors = new List<SolidColorBrush>
            {
                new SolidColorBrush(Colors.White),
                new SolidColorBrush(Colors.Black),
                new SolidColorBrush(Colors.Red),
                new SolidColorBrush(Colors.Green),
                new SolidColorBrush(Colors.Blue),
                new SolidColorBrush(Colors.Yellow),
                new SolidColorBrush(Colors.Orange),
                new SolidColorBrush(Colors.Purple),
                new SolidColorBrush(Colors.Pink),
                new SolidColorBrush(Colors.Cyan),
                new SolidColorBrush(Colors.Brown),
                new SolidColorBrush(Colors.Gray),
                new SolidColorBrush(Colors.Silver),
                new SolidColorBrush(Color.FromRgb(255, 215, 0)), // Gold
                new SolidColorBrush(Colors.Lime),
                new SolidColorBrush(Colors.Teal),
                new SolidColorBrush(Colors.Indigo),
                new SolidColorBrush(Colors.Violet),
                new SolidColorBrush(Colors.Magenta),
                new SolidColorBrush(Colors.Navy),
                new SolidColorBrush(Colors.Olive),
                new SolidColorBrush(Colors.Maroon),
                new SolidColorBrush(Color.FromRgb(64, 224, 208)), // Turquoise
                new SolidColorBrush(Colors.Coral),
                new SolidColorBrush(Colors.Crimson),
                new SolidColorBrush(Colors.SlateBlue),
                new SolidColorBrush(Colors.ForestGreen),
                new SolidColorBrush(Colors.DeepPink),
                new SolidColorBrush(Colors.Khaki),
                new SolidColorBrush(Colors.SteelBlue)
            };

            // Create the item template for ComboBox
            DataTemplate template = new DataTemplate();
            FrameworkElementFactory stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.WidthProperty, 16.0);
            border.SetValue(Border.HeightProperty, 16.0);
            border.SetValue(Border.MarginProperty, new Thickness(0, 0, 5, 0));
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Colors.Gray));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetBinding(Border.BackgroundProperty, new Binding("."));
            
            FrameworkElementFactory textBlock = new FrameworkElementFactory(typeof(TextBlock));
            textBlock.SetBinding(TextBlock.TextProperty, new Binding(".") { Converter = new BrushToColorNameConverter() });
            textBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            
            stackPanel.AppendChild(border);
            stackPanel.AppendChild(textBlock);
            
            template.VisualTree = stackPanel;
            
            // Apply to ComboBox
            _colorComboBox.ItemTemplate = template;
            _colorComboBox.ItemsSource = colors;
            _colorComboBox.SelectedIndex = 0;
        }
        
        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is SolidColorBrush brush)
            {
                ColorChanged?.Invoke(this, new ColorChangedEventArgs(brush.Color));
            }
        }

        private void DieTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is DieTypeItem item)
            {
                DieTypeChanged?.Invoke(this, new DieTypeChangedEventArgs(item.Type));
            }
        }
    }
    
    public class DieTypeChangedEventArgs : EventArgs
    {
        public DieType DieType { get; }
        
        public DieTypeChangedEventArgs(DieType dieType)
        {
            DieType = dieType;
        }
    }
    
    public class ColorChangedEventArgs : EventArgs
    {
        public Color Color { get; }
        
        public ColorChangedEventArgs(Color color)
        {
            Color = color;
        }
    }
}
