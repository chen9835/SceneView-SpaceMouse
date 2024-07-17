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
using System.Windows.Input;

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace SceneViewTdx
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const double EARTH_RADIUS = 6378137.0;

        private readonly string _elevationServiceUrl = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";

        public MainWindow()
        {
            try
            {
                AGELogger.GetInst().Debug("-----Start up test app-------");
                InitializeComponent();
                AGELogger.GetInst().Debug("-----Success Initialize Component-------");
                InitializeSceneView();
                AGELogger.GetInst().Debug("-----Success Initialize SceneView logic-------");

                this.Loaded += (s, e) =>
                {
                    TDxInputManager.GetInstance().Initialize(MySceneView);
                };
            }
            catch (Exception ex)
            {
                AGELogger.GetInst().Error("-----Fail to Initialize:-------\n" + ex.Message);
            }
        }

        private void InitializeSceneView()
        {
            string WORLD_IMAGERY_URL2 = "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer";
            var baseMap = new Basemap(new ArcGISTiledLayer(new Uri(WORLD_IMAGERY_URL2)) { Name = "World Imagery" }) { Name = "Imagery" };
            MySceneView.Scene = new Scene(baseMap);

            // Add the base surface for elevation data.
            Surface elevationSurface = new Surface();
            ArcGISTiledElevationSource elevationSource = new ArcGISTiledElevationSource(new Uri(_elevationServiceUrl));
            elevationSurface.ElevationSources.Add(elevationSource);

            // Add the surface to the scene.
            MySceneView.Scene.BaseSurface = elevationSurface;

            // Set the initial camera.
            MapPoint initialLocation = new MapPoint(-119.9489, 46.7592, 0, SpatialReferences.Wgs84);
            Camera initialCamera = new Camera(initialLocation, 15000, 40, 60, 0);
            MySceneView.SetViewpointCamera(initialCamera);

            MySceneView.GeoViewTapped += MySceneView_GeoViewTapped;
            MySceneView.GeoViewDoubleTapped += MySceneView_GeoViewDoubleTapped;
        }

        private void MySceneView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e)
        {
            AGELogger.GetInst().Debug("-----GeoViewDoubleTapped-------");
            System.Diagnostics.Debug.WriteLine("-----GeoViewDoubleTapped-------");

            MapPoint mapPoint = MySceneView.ScreenToBaseSurface(Mouse.GetPosition(MySceneView));
            if (mapPoint != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("-----location:{0},{1},{2}-------", mapPoint.X, mapPoint.Y, mapPoint.Z));
            }
        }

        private void MySceneView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            AGELogger.GetInst().Debug("-----GeoViewTapped-------");
            System.Diagnostics.Debug.WriteLine("-----GeoViewTapped-------");

            MapPoint mapPoint = MySceneView.ScreenToBaseSurface(Mouse.GetPosition(MySceneView));
            if (mapPoint != null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("-----location:{0},{1},{2}-------", mapPoint.X, mapPoint.Y, mapPoint.Z));
            }
        }
    }
}
