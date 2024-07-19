// COPYRIGHT © 2024 ESRI
//
// TRADE SECRETS: ESRI PROPRIETARY AND CONFIDENTIAL
// Unpublished material - all rights reserved under the
// Copyright Laws of the United States and applicable international
// laws, treaties, and conventions.
//
// For additional information, contact:
// Environmental Systems Research Institute, Inc.
// Attn: Contracts and Legal Services Department
// 380 New York Street
// Redlands, California, 92373
// USA
//
// email: contracts@esri.com


using System;
using System.Windows;

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

using TDx.TDxInput;

namespace SceneViewTdx
{
    /// <summary>
    /// Parse device data to drive navigator
    /// </summary>
    public class TDxInputManager : IDisposable
    {
        //camera manager
        private EarthCameraManager _earthCameraManager = null;
        //initialize instance
        private static readonly TDxInputManager s_inst = new();

        private Sensor _senser = null;
        private Device _device = null;

        //if altitude less than the value, will rotate; if not, move up or down
        private readonly float NAVIGATION_ROTATIONX_MAX_ALTITUDE = 2000000.0f;
        //no mouse navigator input max pitch value
        private const float NAVIGATION_PITCH_MAX = 90.0f;
        //no mouse navigator input min pitch value
        private readonly float NAVIGATION_PITCH_MIN = 0.0f;
        //max angle delta at one update
        private readonly float NAVIGATION_HEADING_DELTA_MAX = 0.6f;
        //max altitude
        private readonly float NAVIGATION_ALTITUDE_MAX = 20000000.0f;
        private readonly float NAVIGATION_TRANSLATE_SPEED_SCALE_MAX = 0.0f;

        private SceneView _sceneView = null;
        private bool _isEnabled = false;
        private float _moveSpeedFactor = 1.0f;
        private float _zoomSpeedFactor = 1.0f;
        private float _rotateXSpeedFactor = 1.0f;
        private float _rotateZSpeedFactor = 1.0f;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }

        public float MoveSpeedFactor
        {
            get { return _moveSpeedFactor; }
            set { _moveSpeedFactor = value; }
        }

        public float ZoomSpeedFactor
        {
            get { return _zoomSpeedFactor; }
            set { _zoomSpeedFactor = value; }
        }

        public float RotateXSpeedFactor
        {
            get { return _rotateXSpeedFactor; }
            set { _rotateXSpeedFactor = value; }
        }

        public float RotateZSpeedFactor
        {
            get { return _rotateZSpeedFactor; }
            set { _rotateZSpeedFactor = value; }
        }

        protected TDxInputManager()
        {
            NAVIGATION_TRANSLATE_SPEED_SCALE_MAX = (float)Math.Atan(NAVIGATION_PITCH_MAX * Math.PI / 180.0) + 0.01f;
        }

        public static TDxInputManager GetInstance()
        {
            return s_inst;
        }

        public void Initialize(SceneView sceneView)
        {
            try
            {
                _sceneView = sceneView;
                Application.Current.Activated += OnEarthActivated;
                Application.Current.Deactivated += OnEarthDeactivated;

                _device = new DeviceClass();
                _device.Connect();
                _device.DeviceChange += OnDeviceChange;

                if (_device.IsConnected)
                {
                    _senser = _device.Sensor;
                    _senser.SensorInput += OnSensorInput;
                }

                //Initialize camera manager
                _earthCameraManager = new EarthCameraManager(sceneView);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private bool _isNavigating = false;
        private void OnSensorInput()
        {
            System.Diagnostics.Debug.WriteLine(string.Format("----rotation angle:{0}, translation length {1}", _senser.Rotation.Angle, _senser.Translation.Length));
            
            if (!_isNavigating)
            {
                _isNavigating = true;
            }

            _earthCameraManager.Reset();

            RotateCamera();

            TranslateCamera();

            _earthCameraManager.Update();
        }

        private void RotateCamera() 
        {
            if (_senser.Rotation.Angle > 0.0)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("----rotation:{0}, {1}, {2}", _senser.Rotation.X, _senser.Rotation.Y, _senser.Rotation.Z));

                if (Math.Abs(_senser.Rotation.X) > 0.0)
                {
                    //compute angle you need to rotate
                    double rotateSpeed = GetRotateXSpeed(_sceneView.Camera);
                    double angleDelta = _senser.Rotation.X * rotateSpeed;

                    //default is rotate
                    bool bRotate = true;
                    if (_sceneView.Camera.Pitch > NAVIGATION_PITCH_MAX)
                    {
                        bRotate = angleDelta < 0 ? true : false;
                    }
                    else if (_sceneView.Camera.Pitch < NAVIGATION_PITCH_MIN)
                    {
                        bRotate = angleDelta < 0 ? false : true;
                    }

                    if (bRotate)
                    {
                        _earthCameraManager.RotateX(angleDelta);
                    }
                }

                if (Math.Abs(_senser.Rotation.Z) > 0.0)
                {
                    //compute angle you need to rotate
                    var rotateSpeed = GetRotateZSpeed();
                    double angleDelta = -_senser.Rotation.Z * rotateSpeed;
                    angleDelta = Math.Clamp(angleDelta, -NAVIGATION_HEADING_DELTA_MAX, NAVIGATION_HEADING_DELTA_MAX);

                    if (_sceneView.Camera.Location.Z < 5000)
                    {
                        _earthCameraManager.RotateZ(angleDelta);
                    }
                    else
                    {
                        _earthCameraManager.RotateRoundByCenter(angleDelta);
                    }
                }
            }
        }

        private void TranslateCamera()
        {
            if (_senser.Translation.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("----translation:{0}, {1}, {2}", _senser.Translation.X, _senser.Translation.Y, _senser.Translation.Z));

                if (Math.Abs(_senser.Translation.X) > 0.0)
                {
                    //compute distance you need to move
                    double moveSpeed = GetMoveSpeed(_sceneView.Camera);
                    double distance = _senser.Translation.X * moveSpeed;
                    _earthCameraManager.MoveLeftRight(distance);
                }

                if (Math.Abs(_senser.Translation.Y) > 0.0)
                {
                    //compute distance you need to move
                    double moveSpeed = GetMoveSpeed(_sceneView.Camera);
                    double distance = -_senser.Translation.Y * moveSpeed;
                    _earthCameraManager.MoveUpDown(distance);
                }
                
                
                if (Math.Abs(_senser.Translation.Z) > 0.0)
                {
                    //compute distance you need to zoom
                    double zoomSpeed = GetZoomSpeed(_sceneView.Camera);
                    double distance = -_senser.Translation.Z * zoomSpeed;
                    _earthCameraManager.Zoom(distance, NAVIGATION_ALTITUDE_MAX);
                }
            }
        }

        private void OnDeviceChange(int reserved)
        {

        }

        public uint Release()
        {
            uint result = 0;
            return result;
        }

        private void OnEarthDeactivated(object sender, EventArgs e)
        {
            try
            {
                try
                {
                    _device?.Disconnect();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void OnEarthActivated(object sender, EventArgs e)
        {
            try
            {
                _device?.Connect();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private float GetMoveSpeed(Camera camera)
        {
            float cameraElevation = (float)camera.Location.Z;
            if (cameraElevation < 1)
            {
                cameraElevation = 20;
            }

            float fSpeedScale = NAVIGATION_TRANSLATE_SPEED_SCALE_MAX - (float)Math.Atan(camera.Pitch * Math.PI / 180);
            return cameraElevation * _moveSpeedFactor * 0.0005f * fSpeedScale;
        }

        private float GetZoomSpeed(Camera camera)
        {
            float cameraElevation = (float)camera.Location.Z;
            if (cameraElevation < 1)
            {
                cameraElevation = 1;
            }

            float fSpeedScale = (float)(Math.Tan(cameraElevation * Math.PI / (NAVIGATION_ALTITUDE_MAX * 4))) + 0.3f;
            return cameraElevation * _zoomSpeedFactor * 0.00012f * fSpeedScale;
        }

        private float GetRotateXSpeed(Camera camera)
        {
            float cameraElevation = (float)camera.Location.Z;
            float fScale = (cameraElevation > NAVIGATION_ROTATIONX_MAX_ALTITUDE) ? 0.1f : 0.5f;

            return _rotateXSpeedFactor * fScale;
        }

        private float GetRotateZSpeed()
        {
            return _rotateZSpeedFactor * 1.0f;
        }

        public void Dispose()
        {
            Release();
        }
    }
}
