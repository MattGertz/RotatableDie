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
        // Initialize in constructor instead of using 'required'
        private readonly DispatcherTimer _randomRotationTimer;
        private readonly DispatcherTimer _directionChangeTimer;
        private Random _random = new Random();
        private int _directionDuration = 10; // Default to 10 seconds
        private int _secondsElapsed = 0;
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

            // Initialize timers for random rotation
            _randomRotationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Update 10 times per second for smooth rotation
            };
            _randomRotationTimer.Tick += RandomRotationTimer_Tick;
            
            _directionChangeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _directionChangeTimer.Tick += DirectionChangeTimer_Tick;

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
        
        // Random rotation timer tick handler - rotates the die
        // Fixing CS8622 warning by making the parameters nullable
        private void RandomRotationTimer_Tick(object? sender, EventArgs e)
        {
            // Apply rotation steps (dividing by 10 because we update 10 times per second)
            double incrementX = _rotationStepX * 0.1;
            double incrementY = _rotationStepY * 0.1;
            double incrementZ = _rotationStepZ * 0.1;
            
            // Apply 3D rotation
            _visualizer.ApplyRandomRotation(incrementX, incrementY, incrementZ);
            
            // Apply 4D rotation if the die is 4D
            if (_currentDieType == DieType.Tesseract || 
                _currentDieType == DieType.Pentachoron ||
                _currentDieType == DieType.Hexadecachoron ||
                _currentDieType == DieType.Octaplex)
            {
                double incrementW = _rotationStepW * 0.1;
                _visualizer.ApplyRandom4DRotation(incrementW);
            }
        }
        
        // Direction change timer tick handler - counts seconds and changes direction
        // Fixing CS8622 warning by making the parameters nullable
        private void DirectionChangeTimer_Tick(object? sender, EventArgs e)
        {
            _secondsElapsed++;
            
            // If we've reached the duration, change direction
            if (_secondsElapsed >= _directionDuration)
            {
                GenerateRandomRotationSteps();
                _secondsElapsed = 0;
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
            if (_currentDieType == DieType.Tesseract || 
                _currentDieType == DieType.Pentachoron ||
                _currentDieType == DieType.Hexadecachoron ||
                _currentDieType == DieType.Octaplex)
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
            
            // Generate initial random rotation steps
            GenerateRandomRotationSteps();
            _secondsElapsed = 0;
            
            // Start timers
            _randomRotationTimer.Start();
            _directionChangeTimer.Start();
        }
        
        // Stop random rotation
        private void StopRandomRotation()
        {
            _randomRotationTimer.Stop();
            _directionChangeTimer.Stop();
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
            _currentDieType = e.DieType;
            _visualizer.CreateDie(_currentDieType, _currentDieColor);
            
            // Update W rotation panel visibility based on new die type
            UpdateWRotationControlVisibility(_currentDieType);
        }
        
        private void OnWireframeModeChanged(object? sender, WireframeModeChangedEventArgs e)
        {
            _visualizer.WireframeMode = e.IsWireframeMode;
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
