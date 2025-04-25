using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using RotatableDie.Models;
using RotatableDie.Services;
using RotatableDie.Models.DieTypes4D;

namespace RotatableDie.UI
{
    /// <summary>
    /// Event arguments for rotation changes
    /// </summary>
    public class RotationChangedEventArgs : EventArgs
    {
        public double AngleX { get; }
        public double AngleY { get; }
        public double AngleZ { get; }
        public Die Die { get; }

        public RotationChangedEventArgs(double angleX, double angleY, double angleZ, Die die)
        {
            AngleX = angleX;
            AngleY = angleY;
            AngleZ = angleZ;
            Die = die;
        }
    }

    /// <summary>
    /// Handles visualization and interaction with the 3D die
    /// </summary>
    public class DieVisualizer
    {
        private readonly Viewport3D _viewport;
        private readonly DieFactory _dieFactory;
        
        private Point _lastMousePosition;
        private bool _isDragging = false;
        
        // We'll use a single transformation matrix instead of separate rotation transforms
        private Transform3DGroup _dieTransform;
        private RotateTransform3D _viewSpaceRotation;
        
        // Keep track of current rotation angles for UI or other purposes
        private double _rotationAngleX = 0;
        private double _rotationAngleY = 0;
        private double _rotationAngleZ = 0;
        
        // Track last reported values to avoid too-frequent updates
        private double _lastReportedX = 0;
        private double _lastReportedY = 0;
        private double _lastReportedZ = 0;
        private double _lastReportedW = 0; // Add tracking for W-axis rotation
        
        // Minimum change in degrees before reporting rotation change
        private const double MIN_ROTATION_CHANGE = 1.0;
        
        private ModelVisual3D _dieVisual;
        
        // Threshold for determining movement direction
        private const double MOVEMENT_THRESHOLD = 2.5;
        private const double MINIMUM_MOVEMENT = 0.5;
        private const int MOVEMENT_HISTORY_SIZE = 5;
        
        // Movement history tracking
        private Queue<Vector> _movementHistory = new Queue<Vector>();
        private MovementIntent _currentIntent = MovementIntent.None;
        
        // Wireframe mode support
        private bool _wireframeMode = false;
        
        // Event for notifying of rotation changes
        public event EventHandler<RotationChangedEventArgs> RotationChanged = delegate { };
        
        private enum MovementIntent
        {
            None,
            Vertical,
            Horizontal,
            Diagonal
        }

        private Die _currentDie; // Store reference to the current die instance
        
        /// <summary>
        /// Gets the type of the currently displayed die
        /// </summary>
        public DieType CurrentDieType => _currentDie.Type;
        
        /// <summary>
        /// Gets or sets whether wireframe mode is enabled
        /// </summary>
        public bool WireframeMode
        {
            get => _wireframeMode;
            set
            {
                if (_wireframeMode != value)
                {
                    _wireframeMode = value;
                    RefreshDie();
                }
            }
        }

        // Store the current die color to avoid relying on color extraction from geometry
        private Color _currentDieColor = Colors.White;
        
        public DieVisualizer(Viewport3D viewport, DieFactory dieFactory)
        {
            _viewport = viewport;
            _dieFactory = dieFactory;
            
            // Initialize _currentDie to avoid non-nullable warning
            _currentDie = _dieFactory.CreateDie(DieType.Cube);

            // Initialize transform - we'll use a single rotation transform with a matrix
            _dieTransform = new Transform3DGroup();
            _viewSpaceRotation = new RotateTransform3D(new QuaternionRotation3D(new Quaternion()));
            _dieTransform.Children.Add(_viewSpaceRotation);

            // Create the die visual container
            _dieVisual = new ModelVisual3D();
            _viewport.Children.Add(_dieVisual);

            // Create an invisible background plane that covers the entire viewport
            // This ensures we can capture mouse events anywhere
            ModelVisual3D backgroundVisual = CreateBackgroundPlane();
            _viewport.Children.Insert(0, backgroundVisual); // Add at the back

            // Set up mouse event handlers
            _viewport.MouseDown += Viewport_MouseDown;
            _viewport.MouseMove += Viewport_MouseMove;
            _viewport.MouseUp += Viewport_MouseUp;
            _viewport.MouseWheel += Viewport_MouseWheel; // Add wheel event for 4D rotation
        }


        private ModelVisual3D CreateBackgroundPlane()
        {
            // Create a large transparent plane at the back of the scene
            // This will catch mouse events even when clicking on empty space
            var backgroundModel = new GeometryModel3D();
            var backgroundMesh = new MeshGeometry3D();

            // Create a simple quad that covers a large area
            backgroundMesh.Positions.Add(new Point3D(-100, -100, -5));
            backgroundMesh.Positions.Add(new Point3D(100, -100, -5));
            backgroundMesh.Positions.Add(new Point3D(100, 100, -5));
            backgroundMesh.Positions.Add(new Point3D(-100, 100, -5));

            backgroundMesh.TriangleIndices.Add(0);
            backgroundMesh.TriangleIndices.Add(1);
            backgroundMesh.TriangleIndices.Add(2);
            backgroundMesh.TriangleIndices.Add(0);
            backgroundMesh.TriangleIndices.Add(2);
            backgroundMesh.TriangleIndices.Add(3);

            // Completely transparent material, but still receives input
            var transparentMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)));
            backgroundModel.Material = transparentMaterial;
            backgroundModel.Geometry = backgroundMesh;

            var backgroundVisual = new ModelVisual3D();
            backgroundVisual.Content = backgroundModel;

            return backgroundVisual;
        }

        /// <summary>
        /// Creates and renders a die with the specified type and color
        /// </summary>
        public void CreateDie(DieType dieType, Color color)
        {
            // Store the previous die's 4D rotation if it's a Tesseract
            double previousXW = 0;
            double previousYW = 0;
            double previousZW = 0;
            bool preserveRotation = false;
            
            if (_currentDie is Die4D previousDie4D && dieType == DieType.Tesseract)
            {
                // We're changing from a 4D die to another 4D die (or same one with different color)
                // Preserve the 4D rotation angles
                previousXW = ((Die4D)_currentDie).RotationXW;
                previousYW = ((Die4D)_currentDie).RotationYW;
                previousZW = ((Die4D)_currentDie).RotationZW;
                preserveRotation = true;
            }
            
            // Clear the current die visual content
            _dieVisual.Content = null;
            
            // Force UI update
            _viewport.InvalidateVisual();
            
            // Store the current color
            _currentDieColor = color;
            
            // Create a new model group for the die
            Model3DGroup dieModelGroup = new Model3DGroup();
            
            // Create solid based on selected type
            _currentDie = _dieFactory.CreateDie(dieType);
            
            // Restore 4D rotation if needed
            if (preserveRotation && _currentDie is Die4D newDie4D)
            {
                newDie4D.RotationXW = previousXW;
                newDie4D.RotationYW = previousYW;
                newDie4D.RotationZW = previousZW;
            }
            
            // Create the geometry with the preserved rotation and wireframe mode
            _currentDie.CreateGeometry(dieModelGroup, _currentDieColor, _wireframeMode);
            
            // Apply the unified transform to the model
            dieModelGroup.Transform = _dieTransform;
            
            // Set the content of the die visual
            _dieVisual.Content = dieModelGroup;
        }
        
        /// <summary>
        /// Refreshes the current die with current settings
        /// </summary>
        private void RefreshDie()
        {
            if (_currentDie == null || _dieVisual.Content == null)
                return;
                
            // Use the stored color instead of trying to extract it
            Model3DGroup dieModelGroup = new Model3DGroup();
            
            // If it's a 4D die, preserve the 4D rotations
            if (_currentDie is Die4D die4D)
            {
                // Recreate geometry with preserved 4D rotation and wireframe mode
                die4D.CreateGeometry(dieModelGroup, _currentDieColor, _wireframeMode);
            }
            else
            {
                // Recreate geometry with wireframe mode
                _currentDie.CreateGeometry(dieModelGroup, _currentDieColor, _wireframeMode);
            }
            
            // Apply the existing 3D transform
            dieModelGroup.Transform = _dieTransform;
            
            // Update the visual
            _dieVisual.Content = dieModelGroup;
        }
        
        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Capture mouse input regardless of where the click happens
            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed || 
                e.MiddleButton == MouseButtonState.Pressed) // Add middle button support
            {
                _isDragging = true;
                _lastMousePosition = e.GetPosition(_viewport);
                
                // Reset movement tracking on new drag
                _movementHistory.Clear();
                _currentIntent = MovementIntent.None;
                
                // Capture mouse to continue getting events even when outside the viewport
                Mouse.Capture(_viewport);
                
                // Important: Mark the event as handled to prevent it from bubbling up
                e.Handled = true;
            }
        }
        
        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentMousePosition = e.GetPosition(_viewport);
                
                // Calculate the difference in position
                double deltaX = currentMousePosition.X - _lastMousePosition.X;
                double deltaY = currentMousePosition.Y - _lastMousePosition.Y;
                
                // Left button: X and Y axis rotation in view space
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Get absolute deltas for direction comparison
                    double absDeltaX = Math.Abs(deltaX);
                    double absDeltaY = Math.Abs(deltaY);
                    
                    // Skip processing for very tiny movements (likely unintentional)
                    if (absDeltaX < 0.1 && absDeltaY < 0.1)
                    {
                        _lastMousePosition = currentMousePosition;
                        return;
                    }
                    
                    // Record this movement vector for intent tracking
                    Vector movement = new Vector(deltaX, deltaY);
                    UpdateMovementHistory(movement);
                    
                    // Analyze current movement intent based on recent history
                    MovementIntent newIntent = AnalyzeMovementIntent();
                    
                    // If we have a clear new intent or if the intent has changed
                    if (newIntent != MovementIntent.None && (_currentIntent == MovementIntent.None || newIntent != _currentIntent))
                    {
                        _currentIntent = newIntent;
                    }
                    
                    // Get the current rotation as a quaternion
                    QuaternionRotation3D? currentRotation = _viewSpaceRotation.Rotation as QuaternionRotation3D;
                    if (currentRotation == null)
                    {
                        // First time initialization or fallback
                        currentRotation = new QuaternionRotation3D(new Quaternion());
                    }
                    
                    // Apply rotations based on current intent
                    // We calculate rotations in view space (screen aligned)
                    Vector3D xAxis = new Vector3D(1, 0, 0);
                    Vector3D yAxis = new Vector3D(0, 1, 0);
                    
                    double angleX = 0;
                    double angleY = 0;
                    
                    switch (_currentIntent)
                    {
                        case MovementIntent.Vertical:
                            // Only rotate around X axis (horizontal axis of screen)
                            angleX = deltaY * 0.5;
                            _rotationAngleX += angleX; // Track angle for UI
                            break;
                            
                        case MovementIntent.Horizontal:
                            // Only rotate around Y axis (vertical axis of screen)
                            angleY = deltaX * 0.5;
                            _rotationAngleY += angleY; // Track angle for UI
                            break;
                            
                        case MovementIntent.Diagonal:
                            // Apply both rotations
                            angleX = deltaY * 0.5;
                            angleY = deltaX * 0.5;
                            _rotationAngleX += angleX;
                            _rotationAngleY += angleY;
                            break;
                            
                        case MovementIntent.None:
                            // First movement or unclear intent - use a stricter single-movement check
                            if (absDeltaY > MOVEMENT_THRESHOLD * absDeltaX && absDeltaY > MINIMUM_MOVEMENT)
                            {
                                angleX = deltaY * 0.5; // Vertical movement only
                                _rotationAngleX += angleX;
                            }
                            else if (absDeltaX > MOVEMENT_THRESHOLD * absDeltaY && absDeltaX > MINIMUM_MOVEMENT)
                            {
                                angleY = deltaX * 0.5; // Horizontal movement only
                                _rotationAngleY += angleY;
                            }
                            else if (absDeltaX > MINIMUM_MOVEMENT && absDeltaY > MINIMUM_MOVEMENT)
                            {
                                // Both axes have significant movement
                                angleX = deltaY * 0.5;
                                angleY = deltaX * 0.5;
                                _rotationAngleX += angleX;
                                _rotationAngleY += angleY;
                            }
                            break;
                    }
                    
                    // Apply rotations to the object using camera-aligned axes
                    if (Math.Abs(angleX) > 0.001 || Math.Abs(angleY) > 0.001)
                    {
                        // Convert rotation angles to quaternions (in radians)
                        Quaternion rotX = new Quaternion(xAxis, angleX);
                        Quaternion rotY = new Quaternion(yAxis, angleY);
                        
                        // Combine rotations: first X, then Y
                        Quaternion combinedRotation = Quaternion.Multiply(rotY, rotX);
                        
                        // Apply to current rotation
                        Quaternion newRotation = Quaternion.Multiply(combinedRotation, currentRotation.Quaternion);
                        
                        // Update the model's rotation
                        _viewSpaceRotation.Rotation = new QuaternionRotation3D(newRotation);
                    }
                }
                
                // Right mouse button: Z axis rotation (around view direction)
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    // Calculate rotation based on movement relative to center of viewport
                    Point viewportCenter = new Point(_viewport.ActualWidth / 2, _viewport.ActualHeight / 2);
                    Point previousVector = new Point(_lastMousePosition.X - viewportCenter.X, _lastMousePosition.Y - viewportCenter.Y);
                    Point currentVector = new Point(currentMousePosition.X - viewportCenter.X, currentMousePosition.Y - viewportCenter.Y);
                    
                    // Skip tiny movements to avoid erratic rotation
                    double prevMagnitude = Math.Sqrt(previousVector.X * previousVector.X + previousVector.Y * previousVector.Y);
                    double currMagnitude = Math.Sqrt(currentVector.X * currentVector.X + currentVector.Y * currentVector.Y);
                    
                    if (prevMagnitude > 5 && currMagnitude > 5) // Minimum threshold to detect intentional movement
                    {
                        // Calculate the angle between previous and current vectors (cross product)
                        double crossProduct = previousVector.X * currentVector.Y - previousVector.Y * currentVector.X;
                        
                        // The sign of the cross product tells us the rotation direction
                        double rotationFactor = 0.5; // Adjust sensitivity

                        // Apply rotation
                        double rotationAmount = (crossProduct / (prevMagnitude * currMagnitude)) * rotationFactor * 50;

                        // Track Z rotation for UI
                        _rotationAngleZ -= rotationAmount;

                        // Get the current rotation as a quaternion
                        QuaternionRotation3D? currentRotation = _viewSpaceRotation.Rotation as QuaternionRotation3D;
                        if (currentRotation == null)
                        {
                            currentRotation = new QuaternionRotation3D(new Quaternion());
                        }

                        // Create rotation around view Z axis (screen normal)
                        Vector3D zAxis = new Vector3D(0, 0, 1);
                        Quaternion rotZ = new Quaternion(zAxis, -rotationAmount);

                        // Apply Z rotation in view space
                        Quaternion newRotation = Quaternion.Multiply(rotZ, currentRotation.Quaternion);

                        // Update the model's rotation
                        _viewSpaceRotation.Rotation = new QuaternionRotation3D(newRotation);
                    }
                }
                
                // Middle mouse button: 4D rotations for 4D dice types
                if (e.MiddleButton == MouseButtonState.Pressed && _currentDie is Die4D die4D)
                {
                    // Get absolute deltas for direction comparison
                    double absDeltaX = Math.Abs(deltaX);
                    double absDeltaY = Math.Abs(deltaY);
                    
                    // Skip processing for very tiny movements
                    if (absDeltaX < 0.1 && absDeltaY < 0.1)
                    {
                        _lastMousePosition = currentMousePosition;
                        return;
                    }
                    
                    // Scale factors to make rotations more sensitive
                    const double ROTATION_SCALE = 0.01;
                    
                    // Apply appropriate 4D rotations based on mouse movement
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        // Shift + Middle Button: Rotate in Z-W plane
                        double deltaZW = deltaX * ROTATION_SCALE;
                        die4D.Rotate4D(0, 0, deltaZW);
                    }
                    else
                    {
                        // Middle Button: Rotate in X-W and Y-W planes
                        double deltaXW = -deltaY * ROTATION_SCALE; // Invert Y for more intuitive control
                        double deltaYW = deltaX * ROTATION_SCALE;
                        die4D.Rotate4D(deltaXW, deltaYW, 0);
                    }
                        
                    // Update the geometry with the new 4D rotation
                    Model3DGroup dieModelGroup = new Model3DGroup();
                    
                    // Use the stored current color instead of trying to extract it
                    die4D.CreateGeometry(dieModelGroup, _currentDieColor, _wireframeMode);
                    
                    // Apply the existing 3D transform
                    dieModelGroup.Transform = _dieTransform;
                    
                    // Update the visual
                    _dieVisual.Content = dieModelGroup;
                    
                    // Report 4D rotation change to update the W label
                    ReportRotationChange();
                }
                
                // Save the current position for the next move event
                _lastMousePosition = currentMousePosition;

                // Report rotation change if significant
                ReportRotationChange();
            }
        }
        
        private void UpdateMovementHistory(Vector movement)
        {
            _movementHistory.Enqueue(movement);
            
            // Keep only the most recent movements
            while (_movementHistory.Count > MOVEMENT_HISTORY_SIZE)
            {
                _movementHistory.Dequeue();
            }
        }
        
        private MovementIntent AnalyzeMovementIntent()
        {
            if (_movementHistory.Count < 3) // Need at least a few samples to determine intent
                return MovementIntent.None;
            
            // Analyze the movement vectors to determine dominant direction
            double totalDeltaX = 0;
            double totalDeltaY = 0;
            
            foreach (Vector movement in _movementHistory)
            {
                totalDeltaX += Math.Abs(movement.X);
                totalDeltaY += Math.Abs(movement.Y);
            }
            
            // Calculate average to smooth out jitter
            double avgDeltaX = totalDeltaX / _movementHistory.Count;
            double avgDeltaY = totalDeltaY / _movementHistory.Count;
            
            // Determine movement intent based on the average movement
            if (avgDeltaY > MOVEMENT_THRESHOLD * avgDeltaX && avgDeltaY > MINIMUM_MOVEMENT)
            {
                return MovementIntent.Vertical;
            }
            else if (avgDeltaX > MOVEMENT_THRESHOLD * avgDeltaY && avgDeltaX > MINIMUM_MOVEMENT)
            {
                return MovementIntent.Horizontal;
            }
            else if (avgDeltaX > MINIMUM_MOVEMENT && avgDeltaY > MINIMUM_MOVEMENT)
            {
                return MovementIntent.Diagonal;
            }
            
            return MovementIntent.None;
        }
        
        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released && 
                e.RightButton == MouseButtonState.Released &&
                e.MiddleButton == MouseButtonState.Released) // Add middle button check
            {
                _isDragging = false;
                _currentIntent = MovementIntent.None;
                _movementHistory.Clear();
                
                // Release mouse capture
                Mouse.Capture(null);
            }
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Handle mouse wheel for additional 4D rotation control
            if (_currentDie is Die4D die4D)
            {
                // Convert wheel delta to a small rotation angle (wheel delta comes in multiples of 120)
                // Important: Preserve the sign to allow both positive and negative rotations
                double rotationDelta = e.Delta / 1200.0;
                
                // Apply rotation based on modifier keys
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    // Ctrl + Wheel: Rotate in X-W plane
                    die4D.Rotate4D(rotationDelta, 0, 0);
                }
                else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    // Shift + Wheel: Rotate in Y-W plane
                    die4D.Rotate4D(0, rotationDelta, 0);
                }
                else
                {
                    // Just Wheel: Rotate in Z-W plane
                    die4D.Rotate4D(0, 0, rotationDelta);
                }
                
                // Normalize 4D rotation angles to keep within -π to +π range
                NormalizeRotation4D(die4D);
                
                // Update the geometry with the new rotation
                Model3DGroup dieModelGroup = new Model3DGroup();
                
                // Use the stored current color instead of trying to extract it
                die4D.CreateGeometry(dieModelGroup, _currentDieColor, _wireframeMode);
                
                // Apply the existing 3D transform
                dieModelGroup.Transform = _dieTransform;
                
                // Update the visual
                _dieVisual.Content = dieModelGroup;
                
                e.Handled = true;
                
                // Report rotation change to update the W label
                ReportRotationChange();
            }
        }

        // Helper method to keep 4D rotation angles within a consistent range
        private void NormalizeRotation4D(Die4D die4D)
        {
            const double TwoPi = Math.PI * 2;
            
            // Normalize XW rotation
            while (die4D.RotationXW > Math.PI) die4D.RotationXW -= TwoPi;
            while (die4D.RotationXW < -Math.PI) die4D.RotationXW += TwoPi;
            
            // Normalize YW rotation
            while (die4D.RotationYW > Math.PI) die4D.RotationYW -= TwoPi;
            while (die4D.RotationYW < -Math.PI) die4D.RotationYW += TwoPi;
            
            // Normalize ZW rotation
            while (die4D.RotationZW > Math.PI) die4D.RotationZW -= TwoPi;
            while (die4D.RotationZW < -Math.PI) die4D.RotationZW += TwoPi;
        }
        
        // Helper method to extract the current die color
        private Color GetCurrentDieColor()
        {
            // Default color if we can't determine the current color
            Color defaultColor = Colors.White;
            
            // Try to extract the color from the existing model
            if (_dieVisual.Content is Model3DGroup currentGroup && 
                currentGroup.Children.Count > 0)
            {
                foreach (var model in currentGroup.Children)
                {
                    if (model is GeometryModel3D geometryModel && 
                        geometryModel.Material is DiffuseMaterial diffuseMaterial &&
                        diffuseMaterial.Brush is SolidColorBrush brush)
                    {
                        // Found a solid color - use this as our die color
                        // Make sure we're getting the full opacity version
                        return Color.FromRgb(brush.Color.R, brush.Color.G, brush.Color.B);
                    }
                }
            }
            
            return defaultColor;
        }

        private void ReportRotationChange()
        {
            // For 4D dice, check W rotation changes
            double currentW = 0;
            bool is4DDie = false;
            
            // Check if we have a 4D die and get its W rotation
            if (_currentDie is Die4D die4D)
            {
                is4DDie = true;
                currentW = die4D.RotationWAngle;
            }
            
            // Check if the change is significant enough to report
            if (Math.Abs(_rotationAngleX - _lastReportedX) >= MIN_ROTATION_CHANGE ||
                Math.Abs(_rotationAngleY - _lastReportedY) >= MIN_ROTATION_CHANGE ||
                Math.Abs(_rotationAngleZ - _lastReportedZ) >= MIN_ROTATION_CHANGE ||
                (is4DDie && Math.Abs(currentW - _lastReportedW) >= MIN_ROTATION_CHANGE))
            {
                // Update last reported values
                _lastReportedX = _rotationAngleX;
                _lastReportedY = _rotationAngleY;
                _lastReportedZ = _rotationAngleZ;
                
                // Update last reported W value if we have a 4D die
                if (is4DDie)
                {
                    _lastReportedW = currentW;
                }

                // Raise the event
                RotationChanged?.Invoke(this, new RotationChangedEventArgs(_rotationAngleX, _rotationAngleY, _rotationAngleZ, _currentDie));
            }
        }

        /// <summary>
        /// Applies random rotation increments to the die in 3D space
        /// </summary>
        /// <param name="incrementX">X-axis rotation increment in degrees</param>
        /// <param name="incrementY">Y-axis rotation increment in degrees</param>
        /// <param name="incrementZ">Z-axis rotation increment in degrees</param>
        public void ApplyRandomRotation(double incrementX, double incrementY, double incrementZ)
        {
            // Skip if there's no die to rotate
            if (_dieVisual.Content == null)
                return;
            
            // Get the current rotation as a quaternion
            QuaternionRotation3D? currentRotation = _viewSpaceRotation.Rotation as QuaternionRotation3D;
            if (currentRotation == null)
            {
                // First time initialization or fallback
                currentRotation = new QuaternionRotation3D(new Quaternion());
            }
            
            // Define rotation axes in view space
            Vector3D xAxis = new Vector3D(1, 0, 0);
            Vector3D yAxis = new Vector3D(0, 1, 0);
            Vector3D zAxis = new Vector3D(0, 0, 1);
            
            // Track angles for UI
            _rotationAngleX += incrementX;
            _rotationAngleY += incrementY;
            _rotationAngleZ += incrementZ;
            
            // Convert rotation angles to quaternions (in radians)
            Quaternion rotX = new Quaternion(xAxis, incrementX);
            Quaternion rotY = new Quaternion(yAxis, incrementY);
            Quaternion rotZ = new Quaternion(zAxis, incrementZ);
            
            // Combine rotations: X, then Y, then Z
            Quaternion combinedRotation = Quaternion.Multiply(rotZ, Quaternion.Multiply(rotY, rotX));
            
            // Apply to current rotation
            Quaternion newRotation = Quaternion.Multiply(combinedRotation, currentRotation.Quaternion);
            
            // Update the model's rotation
            _viewSpaceRotation.Rotation = new QuaternionRotation3D(newRotation);
            
            // Report rotation change
            ReportRotationChange();
        }
        
        /// <summary>
        /// Applies random 4D rotation to the die
        /// </summary>
        /// <param name="incrementW">W-axis rotation increment in degrees</param>
        public void ApplyRandom4DRotation(double incrementW)
        {
            if (_currentDie is Die4D die4D)
            {
                // Apply 4D rotation increment
                die4D.Rotate4D(incrementW, incrementW, incrementW);
                
                // Update the geometry with the new 4D rotation
                Model3DGroup dieModelGroup = new Model3DGroup();
                
                // Use the stored color instead of trying to extract it
                die4D.CreateGeometry(dieModelGroup, _currentDieColor, _wireframeMode);
                
                // Apply the existing 3D transform
                dieModelGroup.Transform = _dieTransform;
                
                // Update the visual
                _dieVisual.Content = dieModelGroup;
                
                // Report rotation change to update the UI labels
                ReportRotationChange();
            }
        }
    }
}
