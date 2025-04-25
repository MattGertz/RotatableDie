using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
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

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            _textureService = new DieTextureService();
            _dieFactory = new DieFactory(_textureService);

            // Initialize UI components
            _controlsManager = new UIControlsManager(this, instructionsTextBlock);
            _visualizer = new DieVisualizer(viewport, _dieFactory);

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
