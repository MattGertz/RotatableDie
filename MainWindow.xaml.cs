using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using RotatableDie.Models;
using RotatableDie.Services;
using RotatableDie.UI;
using RotatableDie.Models.DieTypes4D;

namespace RotatableDie
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DieTextureService _textureService;
        private readonly DieFactory _dieFactory;
        private readonly UIControlsManager _controlsManager;
        private readonly DieVisualizer _visualizer;

        private Color _currentDieColor = Colors.White;
        private DieType _currentDieType = DieType.Cube;
        
        // Random rotation fields
        private readonly DispatcherTimer _rotationDirectionTimer;  // Timer to change rotation direction
        private readonly Random _random = new Random();
        private int _directionDuration = 10; // Default to 10 seconds
        private int _ticks = 0;              // Count ticks of the current direction
        private int _ticksPerDirection = 10; // How many ticks before direction change
        private int _rotationStepX = 0;
        private int _rotationStepY = 0;
        private int _rotationStepZ = 0;
        private int _rotationStepW = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            _textureService = new DieTextureService();
            _dieFactory = new DieFactory(_textureService);

            // Initialize UI components
            _controlsManager = new UIControlsManager(this, instructionsTextBlock);
            _visualizer = new DieVisualizer(viewport, _dieFactory);

            // Initialize timer for random rotation with a default rate (will be adjusted when started)
            _rotationDirectionTimer = new DispatcherTimer();
            _rotationDirectionTimer.Tick += RotationDirectionTimer_Tick;

            // Set up event handlers
            _controlsManager.ColorChanged += OnColorChanged;
            _controlsManager.DieTypeChanged += OnDieTypeChanged;
            _controlsManager.WireframeModeChanged += OnWireframeModeChanged;
            _visualizer.RotationChanged += OnRotationChanged;

            // Set up controls and create initial die
            _controlsManager.SetupControls();
            _visualizer.CreateDie(_currentDieType, _currentDieColor);
            
            // Initialize W rotation panel visibility based on current die type
            UpdateWRotationControlVisibility(_currentDieType);
        }
        
        // Single unified rotation timer tick handler - rotates the die at varying intervals
        // and changes direction after the full duration has elapsed
        private void RotationDirectionTimer_Tick(object? sender, EventArgs e)
        {
            // Apply the 3D rotation steps directly
            _visualizer.ApplyRandomRotation(_rotationStepX, _rotationStepY, _rotationStepZ);
            
            // Apply 4D rotation if the die is 4D, with adjusted increment values
            if (Is4DDie(_currentDieType))
            {
                // Adjust 4D rotation scale based on wireframe mode
                if (_visualizer.WireframeMode)
                {
                    // For wireframe mode with 0.1 second updates, use a small increment
                    _visualizer.ApplyRandom4DRotation(_rotationStepW * 0.2);
                }
                else 
                {
                    // For textured mode with 3 second updates, use a much smaller increment
                    // to avoid large jumps in orientation - just 1 degree per update
                    _visualizer.ApplyRandom4DRotation(_rotationStepW * 0.05);
                }
            }
            
            // Increment tick counter
            _ticks++;
            
            // If we've reached the target number of ticks for this direction, generate new random direction
            if (_ticks >= _ticksPerDirection)
            {
                GenerateRandomRotationSteps();
                _ticks = 0;
            }
        }
        
        // Generate random rotation steps (-1, 0, or 1 for each axis)
        private void GenerateRandomRotationSteps()
        {
            // Generate random values (-1, 0, or 1) for X, Y, and Z axes
            _rotationStepX = _random.Next(3) - 1; // -1, 0, or 1
            _rotationStepY = _random.Next(3) - 1; // -1, 0, or 1
            _rotationStepZ = _random.Next(3) - 1; // -1, 0, or 1
            
            // Only set W rotation for 4D objects
            if (Is4DDie(_currentDieType))
            {
                _rotationStepW = _random.Next(3) - 1; // -1, 0, or 1
            }
            else
            {
                _rotationStepW = 0; // Always 0 for 3D objects
            }
        }
        
        // Start random rotation
        private void StartRandomRotation()
        {
            // Parse direction duration from text box
            if (!int.TryParse(DirectionDurationTextBox.Text, out _directionDuration))
            {
                _directionDuration = 10; // Default to 10 seconds
                DirectionDurationTextBox.Text = "10";
            }
            
            // Ensure direction duration is within valid range
            _directionDuration = Math.Clamp(_directionDuration, 1, 60);
            DirectionDurationTextBox.Text = _directionDuration.ToString();
            
            // Set rotation interval based on die type and wireframe mode
            if (_visualizer.WireframeMode)
            {
                // Wireframe mode: 0.1 second update rate
                _rotationDirectionTimer.Interval = TimeSpan.FromSeconds(0.1);
                // Calculate how many ticks for the full duration
                _ticksPerDirection = (int)Math.Ceiling(_directionDuration / 0.1);
            }
            else if (Is4DDie(_currentDieType))
            {
                // Textured 4D solid: 3 second update rate
                _rotationDirectionTimer.Interval = TimeSpan.FromSeconds(3.0);
                // Calculate how many ticks for the full duration
                _ticksPerDirection = (int)Math.Ceiling(_directionDuration / 3.0);
            }
            else
            {
                // Textured 3D solid: 0.5 second update rate
                _rotationDirectionTimer.Interval = TimeSpan.FromSeconds(0.5);
                // Calculate how many ticks for the full duration
                _ticksPerDirection = (int)Math.Ceiling(_directionDuration / 0.5);
            }
            
            // Always ensure at least one tick per direction
            _ticksPerDirection = Math.Max(1, _ticksPerDirection);
            
            // Generate initial random rotation steps
            GenerateRandomRotationSteps();
            _ticks = 0;
            
            // Start timer
            _rotationDirectionTimer.Start();
        }
        
        // Stop random rotation
        private void StopRandomRotation()
        {
            _rotationDirectionTimer.Stop();
        }

        // Helper method to check if a die type is 4D
        private bool Is4DDie(DieType dieType)
        {
            return dieType == DieType.Tesseract || 
                   dieType == DieType.Pentachoron ||
                   dieType == DieType.Hexadecachoron ||
                   dieType == DieType.Octaplex;
        }

        // Event handler for RandomRotationCheckBox checked/unchecked
        private void RandomRotationCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (RandomRotationCheckBox.IsChecked == true)
            {
                StartRandomRotation();
            }
            else
            {
                StopRandomRotation();
            }
        }
        
        // Event handler for DirectionDurationTextBox text input validation
        private void DirectionDurationTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow digits
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        
        // Event handler for DirectionDurationTextBox text changed
        private void DirectionDurationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DirectionDurationTextBox == null) return;
            
            string text = DirectionDurationTextBox.Text;
            
            if (string.IsNullOrEmpty(text))
            {
                // Allow empty string to enable typing
                return;
            }
            
            // Try parse the input
            if (int.TryParse(text, out int value))
            {
                // Clamp value between 1 and 60
                if (value < 1)
                {
                    DirectionDurationTextBox.Text = "1";
                    DirectionDurationTextBox.CaretIndex = 1;
                }
                else if (value > 60)
                {
                    DirectionDurationTextBox.Text = "60";
                    DirectionDurationTextBox.CaretIndex = 2;
                }
                
                // Update duration if random rotation is active
                if (RandomRotationCheckBox.IsChecked == true)
                {
                    _directionDuration = int.Parse(DirectionDurationTextBox.Text);
                    
                    // Restart the rotation with the new duration
                    StopRandomRotation();
                    StartRandomRotation();
                }
                else
                {
                    // Just store the value for later use if rotation is activated
                    _directionDuration = int.Parse(DirectionDurationTextBox.Text);
                }
            }
            else
            {
                // Invalid input, revert to default
                DirectionDurationTextBox.Text = "10";
                DirectionDurationTextBox.CaretIndex = 2;
            }
        }

        private void OnColorChanged(object? sender, ColorChangedEventArgs e)
        {
            _currentDieColor = e.Color;
            _visualizer.CreateDie(_currentDieType, _currentDieColor);
        }

        private void OnDieTypeChanged(object? sender, DieTypeChangedEventArgs e)
        {
            bool wasRotating = RandomRotationCheckBox.IsChecked == true;
            
            // If rotation was active, stop it temporarily
            if (wasRotating)
            {
                StopRandomRotation();
            }
            
            // Update the die type and create the new die
            _currentDieType = e.DieType;
            _visualizer.CreateDie(_currentDieType, _currentDieColor);
            
            // Update W rotation panel visibility based on new die type
            UpdateWRotationControlVisibility(_currentDieType);
            
            // If rotation was active, restart it with the appropriate timing
            if (wasRotating)
            {
                StartRandomRotation();
            }
        }
        
        private void OnWireframeModeChanged(object? sender, WireframeModeChangedEventArgs e)
        {
            bool wasRotating = RandomRotationCheckBox.IsChecked == true;
            
            // If rotation was active, stop it temporarily
            if (wasRotating)
            {
                StopRandomRotation();
            }
            
            // Update wireframe mode
            _visualizer.WireframeMode = e.IsWireframeMode;
            
            // If rotation was active, restart it with the appropriate timing
            if (wasRotating)
            {
                StartRandomRotation();
            }
        }
        
        private void OnRotationChanged(object? sender, RotationChangedEventArgs e)
        {
            // Update rotation labels with current angles
            XRotationLabel.Text = $"{e.AngleX:F1}°";
            YRotationLabel.Text = $"{e.AngleY:F1}°";
            ZRotationLabel.Text = $"{e.AngleZ:F1}°";
            
            // Update W rotation if we have a 4D die
            if (e.Die is Die4D die4D)
            {
                WRotationLabel.Text = $"{die4D.RotationWAngle:F1}°";
            }
        }
        
        private void UpdateWRotationControlVisibility(DieType dieType)
        {
            // Show W rotation panel for all 4D dice
            WRotationPanel.Visibility = (dieType == DieType.Tesseract || 
                                         dieType == DieType.Pentachoron ||
                                         dieType == DieType.Hexadecachoron ||
                                         dieType == DieType.Octaplex) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
    }
}
