
namespace RuntimeSDKTest
{
    using System;
    using Esri.ArcGISRuntime.Mapping;
    using Esri.ArcGISRuntime.UI.Controls;
    using TDx.SpaceMouse.Navigation3D;

    public class TDxSpaceMouseNavigationController : INavigation3D
    {
        private bool _enable = false;
        private string _profile = default(string);
        private readonly SceneView _sceneView = null;
        private readonly Navigation3D _navigation3D = null;        

        public TDxSpaceMouseNavigationController(SceneView sceneView)
        {
            _sceneView = sceneView;            

            try
            {
                // Create the Navigation3D instance and hook up the event handlers.
                _navigation3D = new Navigation3D(this);

                //Call "Open3DMouse" at this just to test if the 3DXWare driver has been installed, should close it ASAP.
                _navigation3D.Open3DMouse(System.Windows.Application.Current.MainWindow.Title);
                _navigation3D.Close();

                _navigation3D.KeyUp += KeyUpHandler;
                _navigation3D.KeyDown += KeyDownHandler;
                _navigation3D.ExecuteCommand += OnExecuteCommand;
                _navigation3D.MotionChanged += MotionChangedHandler;
                _navigation3D.SettingsChanged += SettingsChangedHandler;
                _navigation3D.TransactionChanged += TransactionChangedHandler;
            }
            catch (Exception)
            {
                _haveDriver = false;
                throw;
            }
        }

        ~TDxSpaceMouseNavigationController()
        {
            if(_navigation3D != null && _enable)
            {
                _navigation3D.Close();
            }
        }

        public bool Enable
        {
            get
            {
                return _enable;
            }

            set
            {
                if (value != _enable  && _navigation3D != null)
                {
                    try
                    {
                        if (value)
                        {
                            _navigation3D.Open3DMouse(_profile);
                        }
                        else
                        {
                            _navigation3D.Close();
                        }

                        // Use the SpaceMouse as the source of the frame timing.
                        _navigation3D.FrameTiming = Navigation3D.TimingSource.SpaceMouse;
                        _navigation3D.EnableRaisingEvents = value;
                        _enable = value;
                    }
                    catch(Exception)
                    {
                        _haveDriver = false;
                        throw;
                    }                    
                }
            }
        }

        private bool _haveDriver = false;
        public bool HaveDriver
        {
            get { return _haveDriver; } 
        }

        /// <summary>
        /// Gets or sets the name for the profile to use.
        /// </summary>
        /// <remarks>
        /// The 3Dconnexion driver will use the name to locate the application configuration file.
        /// </remarks>
        public string Profile
        {
            get
            {
                return _profile;
            }
            set
            {
                try
                {
                    if (_profile != value)
                    {
                        _profile = value;
                        if (_enable && _navigation3D != null)
                        {
                            _navigation3D.Close();
                            _navigation3D.Open3DMouse(_profile);
                        }
                    }
                }
                catch(Exception)
                {
                    _haveDriver = false;
                    throw;
                }                
            }
        }

        #region Navigation3D event handlers

        private void OnExecuteCommand(object sender, CommandEventArgs eventArgs)
        {
        }

        private void KeyDownHandler(object sender, KeyEventArgs eventArgs)
        {
        }

        private void KeyUpHandler(object sender, KeyEventArgs eventArgs)
        {
        }

        private void MotionChangedHandler(object sender, MotionEventArgs eventArgs)
        {
            
        }

        private void SettingsChangedHandler(object sender, EventArgs eventArgs)
        {

        }

        private void TransactionChangedHandler(object sender, TransactionEventArgs eventArgs)
        {
            if (eventArgs.IsBegin)
            {
                System.Diagnostics.Debug.WriteLine("------NavigationModel Begin Transaction-------");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("------NavigationModel End Transaction-------");
            }
        }

        #endregion Navigation3D event handlers

        /// <summary>
        /// Synchronize the current matrix of the camera to the TDX driver
        /// </summary>
        /// <returns></returns>
        Matrix IView.GetCameraMatrix()
        {
            System.Diagnostics.Debug.WriteLine("------IView.GetCameraMatrix-------");
                        
            if(_sceneView != null)
            {
                //convert matrix of esri runtime camera to 3DConnexion Space Mouse matrix
                var cameraTransformation = _sceneView.Camera.Transformation;
                var quatX = (float)cameraTransformation.QuaternionX;
                var quatY = (float)cameraTransformation.QuaternionY;
                var quatZ = (float)cameraTransformation.QuaternionZ;
                var quatW = (float)cameraTransformation.QuaternionW;
                var quaternion = new System.Numerics.Quaternion(quatX, quatY, quatZ, quatW);
                var rotationMatrix = System.Numerics.Matrix4x4.CreateFromQuaternion(quaternion);

                var translationMatrix = System.Numerics.Matrix4x4.CreateTranslation((float)cameraTransformation.TranslationX, (float)cameraTransformation.TranslationY, (float)cameraTransformation.TranslationZ);

                var cameraMatrix = System.Numerics.Matrix4x4.Multiply(rotationMatrix, translationMatrix);

                return new Matrix(
                    cameraMatrix.M11, cameraMatrix.M12, cameraMatrix.M13, cameraMatrix.M14,
                    cameraMatrix.M21, cameraMatrix.M22, cameraMatrix.M23, cameraMatrix.M24,
                    cameraMatrix.M31, cameraMatrix.M32, cameraMatrix.M33, cameraMatrix.M34,
                    cameraMatrix.M41, cameraMatrix.M42, cameraMatrix.M43, cameraMatrix.M44);
            }

            return Matrix.Identity;
        }

        /// <summary>
        /// Update the camera matrix by the matrix from TDX driver
        /// </summary>
        /// <param name="matrix"></param>
        void IView.SetCameraMatrix(Matrix matrix)
        {
            System.Diagnostics.Debug.WriteLine("------IView.SetCameraMatrix-------");

            if (_enable && _sceneView != null)
            {
                

                //convert 3DConnexion Space Mouse matrix to esri runtime camera matrix
                var tmatrix = new System.Numerics.Matrix4x4((float)matrix.M11, (float)matrix.M12, (float)matrix.M13, (float)matrix.M14,
                                                        (float)matrix.M21, (float)matrix.M22, (float)matrix.M23, (float)matrix.M24,
                                                        (float)matrix.M31, (float)matrix.M32, (float)matrix.M33, (float)matrix.M34,
                                                        (float)matrix.M41, (float)matrix.M42, (float)matrix.M43, (float)matrix.M44);

                System.Numerics.Vector3 scale;
                System.Numerics.Vector3 translation;
                System.Numerics.Quaternion rotation;
                System.Numerics.Matrix4x4.Decompose(tmatrix, out scale, out rotation, out translation);


                var transformationMatrix = TransformationMatrix.Create(rotation.X, rotation.Y, rotation.Z, rotation.W, translation.X, translation.Y, translation.Z);
                var camera = new Camera(transformationMatrix);

                _sceneView.SetViewpointCamera(camera);
            }
        }

        Point IView.GetCameraTarget()
        {
            System.Diagnostics.Debug.WriteLine("------IView.GetCameraTarget-------");

            return new Point(0, 0, 0);
        }

        void IView.SetCameraTarget(Point target)
        {
            System.Diagnostics.Debug.WriteLine("------IView.SetCameraTarget-------");
        }

        Plane IView.GetViewConstructionPlane()
        {
            System.Diagnostics.Debug.WriteLine("------IView.GetViewConstructionPlane-------");
            return new Plane();
        }

        Box IView.GetViewExtents()
        {
            System.Diagnostics.Debug.WriteLine("------IView.GetViewExtents-------");
            return new Box();
        }

        double IView.GetViewFOV()
        {
            System.Diagnostics.Debug.WriteLine("------IView.GetViewFOV-------");
            return 45.0;
        }

        Frustum IView.GetViewFrustum()
        {
            System.Diagnostics.Debug.WriteLine("------IView.GetViewFrustum-------");

            return new Frustum();
        }

        Point IView.GetPointerPosition()
        {
            System.Diagnostics.Debug.WriteLine("------IView.GetPointerPosition-------");
            return new Point();
        }

        void IView.SetViewExtents(Box extents)
        {
            System.Diagnostics.Debug.WriteLine("------IView.SetViewExtents-------");
        }

        void IView.SetViewFOV(double fov)
        {
            System.Diagnostics.Debug.WriteLine("------IView.SetViewFOV-------");
        }

        void IView.SetViewFrustum(Frustum frustum)
        {
            System.Diagnostics.Debug.WriteLine("------IView.SetViewFrustum-------");
        }

        void IView.SetPointerPosition(Point position)
        {
            System.Diagnostics.Debug.WriteLine("------IView.SetPointerPosition-------");
        }

        bool IView.IsViewPerspective()
        {
            System.Diagnostics.Debug.WriteLine("------IView.IsViewPerspective-------");
            return true;
        }

        bool IView.IsViewRotatable()
        {
            System.Diagnostics.Debug.WriteLine("------IView.IsViewRotatable-------");
            return true;
        }                

        Matrix ISpace3D.GetCoordinateSystem()
        {
            System.Diagnostics.Debug.WriteLine("------ISpace3D.GetCoordinateSystem-------");

            var m = new Matrix
               (1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,   // right-handed
                0, 0, 0, 1);
            return m;
        }

        Matrix ISpace3D.GetFrontView()
        {
            System.Diagnostics.Debug.WriteLine("------ISpace3D.GetFrontView-------");

            return Matrix.Identity;
        }

        Point IHit.GetLookAt()
        {
            System.Diagnostics.Debug.WriteLine("------IHit.GetLookAt-------");
            var currentCamera = _sceneView.Camera;
            var lookatCamera = _sceneView.Camera.MoveForward(5000);

            double lookatX = lookatCamera.Transformation.TranslationX - currentCamera.Transformation.TranslationX;
            double lookatY = lookatCamera.Transformation.TranslationY - currentCamera.Transformation.TranslationY;
            double lookatZ = lookatCamera.Transformation.TranslationZ - currentCamera.Transformation.TranslationZ;

            return new Point(lookatX, lookatY, lookatZ);
        }

        Box IModel.GetModelExtents()
        {
            System.Diagnostics.Debug.WriteLine("------IModel.GetModelExtents-------");

            return new Box();
        }

        Point IPivot.GetPivotPosition()
        {
            System.Diagnostics.Debug.WriteLine("------IPivot.GetPivotPosition-------");
            return new Point();
        }       

        Box IModel.GetSelectionExtents()
        {
            System.Diagnostics.Debug.WriteLine("------IModel.GetSelectionExtents-------");
            return new Box();
        }

        Matrix IModel.GetSelectionTransform()
        {
            System.Diagnostics.Debug.WriteLine("------IModel.GetSelectionTransform-------");
            return new Matrix();
        }

        bool IModel.IsSelectionEmpty()
        {
            System.Diagnostics.Debug.WriteLine("------IModel.IsSelectionEmpty-------");
            return true;
        }

        bool IPivot.IsUserPivot()
        {
            System.Diagnostics.Debug.WriteLine("------IPivot.IsUserPivot-------");
            return false;
        }        

        void IHit.SetLookAperture(double aperture)
        {
            System.Diagnostics.Debug.WriteLine("------NavigationModel SetLookAperture-------");
        }

        void IHit.SetLookDirection(Vector direction)
        {
            System.Diagnostics.Debug.WriteLine("------IHit.SetLookDirection-------");
        }

        void IHit.SetLookFrom(Point eye)
        {
            System.Diagnostics.Debug.WriteLine("------IHit.SetLookFrom-------");
        }

        void IPivot.SetPivotPosition(Point position)
        {
            System.Diagnostics.Debug.WriteLine("------IPivot.SetPivotPosition-------");
        }

        void IPivot.SetPivotVisible(bool visible)
        {
            System.Diagnostics.Debug.WriteLine("------IPivot.SetPivotVisible-------");
        }

        void IHit.SetSelectionOnly(bool onlySelection)
        {
            System.Diagnostics.Debug.WriteLine("------IHit.SetSelectionOnly-------");
        }

        void IModel.SetSelectionTransform(Matrix matrix)
        {
            System.Diagnostics.Debug.WriteLine("------IModel.SetSelectionTransform-------");
        }
        
    }
}
