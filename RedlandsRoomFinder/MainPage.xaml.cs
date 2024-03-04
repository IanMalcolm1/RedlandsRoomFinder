using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System.ComponentModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RedlandsRoomFinder
{
    public partial class MainPage : ContentPage
    {
        private GraphicsOverlay _mapRouteOverlay;

        public MainPage()
        {
            InitializeComponent();
            InitializeMapView();
        }


        private void InitializeMapView()
        {
            MapPoint mapCenterPoint = new MapPoint(34.063367, -117.164185, SpatialReferences.Wgs84);
            MainMapView.SetViewpoint(new Viewpoint(mapCenterPoint, 100000));

            _mapRouteOverlay = new GraphicsOverlay { Id = "PopOverlay" };
            MainMapView.GraphicsOverlays?.Add(_mapRouteOverlay);
            MainMapViewModel.PropertyChanged += HandleMainMapViewPropertyChanged;
        }

        private async void OnButtonClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Event", "Button Clicked", "Ok");
        }


        private async void OnMapViewTapped(object sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            await MainMapViewModel.SelectLocation(e.Location);
        }

        private void HandleMainMapViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName=="RouteGraphic")
            {
                UpdateRouteGraphic();
            }
        }

        private void UpdateRouteGraphic()
        {
            _mapRouteOverlay.Graphics.Clear();
            if (MainMapViewModel.RouteGraphic != null)
                _mapRouteOverlay.Graphics.Add(MainMapViewModel.RouteGraphic);
        }
    }
}
