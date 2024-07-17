// COPYRIGHT © 2019 ESRI
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


﻿
using System;
using System.Windows;

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace SceneViewTdx
{
    /// <summary>
    /// earth camera manager: move left/right/up/down, zoom, rotate
    /// </summary>
    internal class EarthCameraManager
    {
        private readonly SceneView _sceneView = null;
        private Camera _camera = null;
        private const float _earthCameraDefaultMaxAltitude = 20000000.0f;

        public EarthCameraManager(SceneView sceneView)
        {
            _sceneView = sceneView;
            _camera = _sceneView.Camera;
        }

        public void Reset()
        {
            var camera = _sceneView.Camera;
            if (!camera.Location.IsEqual(_camera.Location))
            {
                _camera = camera;
            }
        }

        /// <summary>
        /// cancel camera opration
        /// </summary>
        public void Cancel()
        {
            _sceneView.CancelSetViewpointOperations();
        }

        /// <summary>
        /// move to the left or right
        /// </summary>
        /// <param name="delta">delta</param>
        public void MoveLeftRight(double delta)
        {
            var curCamera = _camera;
            var camera = curCamera;
            double z = camera.Location.Z;

            camera = camera.RotateTo(camera.Heading, 90, 0);
            camera = camera.RotateTo(camera.Heading + 90, camera.Pitch, camera.Roll);
            camera = camera.MoveForward(delta);
            camera = camera.RotateTo(camera.Heading - 90, camera.Pitch, camera.Roll);
            //Maintain Z
            camera = camera.Elevate(z - camera.Location.Z);

            //Maintain pitch and roll
            _camera = new Camera(camera.Location, camera.Heading, curCamera.Pitch, curCamera.Roll);
        }

        /// <summary>
        /// move to the up or down
        /// </summary>
        /// <param name="delta">delta</param>
        public void MoveUpDown(double delta)
        {
            var curCamera = _camera;
            var camera = curCamera;
            double z = camera.Location.Z;

            camera = camera.RotateTo(camera.Heading, 90, 0);
            camera = camera.MoveForward(-delta);
            //Maintain Z
            camera = camera.Elevate(z - camera.Location.Z);
            //Maintain pitch and roll
            _camera = new Camera(camera.Location, camera.Heading, curCamera.Pitch, curCamera.Roll);
        }

        /// <summary>
        /// zoom in or out
        /// </summary>
        /// <param name="delta">delta</param>
        public void Zoom(double delta, double maxAltitude = _earthCameraDefaultMaxAltitude)
        {
            var curCamera = _camera;
            MapPoint location = new(curCamera.Location.X, curCamera.Location.Y, curCamera.Location.Z - delta);
            if (location.Z > maxAltitude)
            {
                location = new MapPoint(location.X, location.Y, maxAltitude);
            }

            _camera = new Camera(location, curCamera.Heading, curCamera.Pitch, curCamera.Roll);
        }

        /// <summary>
        /// rotate around x axis
        /// </summary>
        /// <param name="delta">delta</param>
        public void RotateX(double delta)
        {
            var camera = _camera;
            var newCamera = new Camera(camera.Location, camera.Heading, camera.Pitch + delta, camera.Roll);
            _camera = newCamera;
        }

        public void RotateY(double delta)
        {
            var camera = _camera;
            _camera = new Camera(camera.Location, camera.Heading, camera.Pitch, camera.Roll + delta);
        }

        public void RotateZ(double delta)
        {
            var camera = _camera;
            var newCamera = new Camera(camera.Location, camera.Heading + delta, camera.Pitch, camera.Roll);
            _camera = newCamera;
        }

        public void RotateRoundByCenter(double delta)
        {
            var camera = _camera;
            Point center = new(_sceneView.ActualWidth / 2.0, _sceneView.ActualHeight / 2.0);
            var lookAtPoint = _sceneView.ScreenToBaseSurface(center);
            while (null == lookAtPoint && (_sceneView.ActualHeight - center.Y) > 10.0)
            {
                double iPntY = center.Y + (_sceneView.ActualHeight - center.Y) / 2;
                center = new Point(center.X, iPntY);
                lookAtPoint = _sceneView.ScreenToBaseSurface(center);
            }

            if (lookAtPoint != null)
            {
                //use RotateAround interface instead
                var newCamera = camera.RotateAround(lookAtPoint, -delta, 0, 0);
                _camera = newCamera;
            }
        }

        public void Update()
        {
            try
            {
                _sceneView.SetViewpointCamera(_camera);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}
