
using System;
using System.Windows;

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

namespace RuntimeSDKTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const double EARTH_RADIUS = 6378137.0;

        private readonly string _elevationServiceUrl = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";

        private TDxSpaceMouseNavigationController _navigationController = null;

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
                    _navigationController = new TDxSpaceMouseNavigationController(MySceneView);
                    _navigationController.Profile = Application.Current.MainWindow.Title;
                    _navigationController.Enable = true;
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
        }
    }
}
