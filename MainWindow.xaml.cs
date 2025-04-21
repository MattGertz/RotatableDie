using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using RotatableDie.Models;
using RotatableDie.Services;
using RotatableDie.UI;

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
            _controlsManager = new UIControlsManager(this);
            _visualizer = new DieVisualizer(viewport, _dieFactory);

            // Set up event handlers
            _controlsManager.ColorChanged += OnColorChanged;
            _controlsManager.DieTypeChanged += OnDieTypeChanged;

            // Set up controls and create initial die
            _controlsManager.SetupControls();
            _visualizer.CreateDie(_currentDieType, _currentDieColor);
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
        }
    }
}
