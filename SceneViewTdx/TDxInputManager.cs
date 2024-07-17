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
        //is earth active
        private static bool _isEarthActive = true;
        //camera manager
        private EarthCameraManager _earthCameraManager = null;
        //initialize instance
        private static readonly TDxInputManager s_inst = new();
        //lock device
        private readonly object _lockDevice = new();

        private Sensor _senser = null;
        private Device _device = null;

        //if altitude less than the value, will rotate; if not, move up or down
        private readonly float _directInput_rotateXMaxAltitude = 2000000.0f;
        //no mouse navigator input max pitch value
        private readonly float _directInput_PitchMaxValue = 90.0f;
        //no mouse navigator input min pitch value
        private readonly float _directInput_PitchMinValue = 0.0f;
        //max angle delta at one update
        private readonly float _directInput_AngleDeltaMaxValue = 0.6f;
        //max altitude
        private readonly float _directInput_AltitudeMaxValue = 20000000.0f;
        private readonly float _directInput_MaxMoveSpeedScale = 0.0f;

        private SceneView _sceneView = null;
        private bool _isEnabled = false;
        private bool _isReversed = true;
        private float _moveSpeedFactor = 1.0f;
        private float _zoomSpeedFactor = 1.0f;
        private float _rotateXSpeedFactor = 1.0f;
        private float _rotateZSpeedFactor = 1.0f;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }

        public bool IsReversed
        {
            get { return _isReversed; }
            set { _isReversed = value; }
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
            _directInput_MaxMoveSpeedScale = (float)Math.Atan(_directInput_PitchMaxValue * Math.PI / 180.0) + 0.01f;
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


        private void OnSensorInput()
        {
            _earthCameraManager.Reset();

            System.Diagnostics.Debug.WriteLine(string.Format("----rotation angle:{0}, translation length {1}", _senser.Rotation.Angle, _senser.Translation.Length));

            bool translate = false;
            if (_senser.Translation.Length > 100.0)
            {
                translate = true;
            }
            else if (_senser.Rotation.Angle <= 0.00001 && _senser.Translation.Length > 0.1)
            {
                translate = true;
            }
            
            if (translate)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("----translation:{0}, {1}, {2}", _senser.Translation.X, _senser.Translation.Y, _senser.Translation.Z));

                double xDelta = Math.Abs(_senser.Translation.X);
                double yDelta = Math.Abs(_senser.Translation.Y);
                double zDelta = Math.Abs(_senser.Translation.Z);
                double maxOne = Math.Max(Math.Max(xDelta, yDelta), zDelta);

                if (Math.Abs(_senser.Translation.X) == maxOne)
                {
                    //compute distance you need to move
                    double moveSpeed = GetMoveSpeed(_sceneView.Camera);
                    double distance = _senser.Translation.X * moveSpeed;
                    distance = _isReversed ? (-distance) : distance;
                    _earthCameraManager.MoveLeftRight(distance);
                }
                else if (Math.Abs(_senser.Translation.Y) == maxOne)
                {
                    //compute distance you need to move
                    double moveSpeed = GetMoveSpeed(_sceneView.Camera);
                    double distance = -_senser.Translation.Y * moveSpeed;
                    _earthCameraManager.MoveUpDown(distance);
                } 
                else if (Math.Abs(_senser.Translation.Z) == maxOne)
                {
                    //compute distance you need to zoom
                    double zoomSpeed = GetZoomSpeed(_sceneView.Camera);
                    double distance = _senser.Translation.Z * zoomSpeed;
                    distance = _isReversed ? (-distance) : distance;
                    _earthCameraManager.Zoom(distance, _directInput_AltitudeMaxValue);                    
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(string.Format("----rotation:{0}, {1}, {2}", _senser.Rotation.X, _senser.Rotation.Y, _senser.Rotation.Z));

                double xRotation = Math.Abs(_senser.Rotation.X);
                double yRotation = Math.Abs(_senser.Rotation.Y);
                double zRotation = Math.Abs(_senser.Rotation.Z);
                double maxOne = Math.Max(Math.Max(xRotation, yRotation), zRotation);

                if (Math.Abs(_senser.Rotation.X) == maxOne)
                {
                    //compute angle you need to rotate
                    double rotateSpeed = GetRotateXSpeed(_sceneView.Camera);
                    double angleDelta = _senser.Rotation.X * rotateSpeed;
                    //angleDelta = Math.Clamp(angleDelta, -_directInput_AngleDeltaMaxValue, _directInput_AngleDeltaMaxValue);

                    //default is rotate
                    bool bRotate = true;
                    if (_sceneView.Camera.Pitch > _directInput_PitchMaxValue)
                    {
                        bRotate = angleDelta < 0 ? true : false;
                    }
                    else if (_sceneView.Camera.Pitch < _directInput_PitchMinValue)
                    {
                        bRotate = angleDelta < 0 ? false : true;
                    }

                    if (bRotate)
                    {
                        angleDelta = _isReversed ? (-angleDelta) : angleDelta;
                        _earthCameraManager.RotateX(angleDelta);
                    }
                }
                else if (Math.Abs(_senser.Rotation.Z) == maxOne)
                {
                    //compute angle you need to rotate
                    var rotateSpeed = GetRotateZSpeed(_sceneView.Camera);
                    double angleDelta = _senser.Rotation.Z * rotateSpeed;
                    //angleDelta = Math.Clamp(angleDelta, -_directInput_AngleDeltaMaxValue, _directInput_AngleDeltaMaxValue);

                    angleDelta = _isReversed ? (-angleDelta) : angleDelta;

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

            _earthCameraManager.Update();
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
            _isEarthActive = false;
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
            _isEarthActive = true;
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

            float fSpeedScale = _directInput_MaxMoveSpeedScale - (float)Math.Atan(camera.Pitch * Math.PI / 180);
            return cameraElevation * _moveSpeedFactor * 0.0001f * fSpeedScale;
        }

        private float GetZoomSpeed(Camera camera)
        {
            float cameraElevation = (float)camera.Location.Z;
            if (cameraElevation < 1)
            {
                cameraElevation = 1;
            }

            float fSpeedScale = (float)(Math.Tan(cameraElevation * Math.PI / (_directInput_AltitudeMaxValue * 4))) + 0.3f;
            return cameraElevation * _zoomSpeedFactor * 0.00012f * fSpeedScale;
        }

        private float GetRotateXSpeed(Camera camera)
        {
            float cameraElevation = (float)camera.Location.Z;
            float fScale = (cameraElevation > _directInput_rotateXMaxAltitude) ? 0.01f : 0.5f;

            return _rotateXSpeedFactor * fScale;
        }

        private float GetRotateZSpeed(Camera camera)
        {
            return _rotateZSpeedFactor * 0.4f;
        }

        public void Dispose()
        {
            Release();
        }
    }
}
