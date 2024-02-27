using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using System.ComponentModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RedlandsRoomFinder
{
    public partial class MainPage : ContentPage
    {
        private GraphicsOverlay _mapPopUpOverlay;

        public MainPage()
        {
            InitializeComponent();
            InitializeMapView();
        }


        private void InitializeMapView()
        {
            MapPoint mapCenterPoint = new MapPoint(34.063367, -117.164185, SpatialReferences.Wgs84);
            MainMapView.SetViewpoint(new Viewpoint(mapCenterPoint, 100000));

            _mapPopUpOverlay = new GraphicsOverlay { Id = "PopOverlay" };
            MainMapView.GraphicsOverlays?.Add(_mapPopUpOverlay);
            //TODO: implement _mapPopUpOverlay properly
        }


        private async void OnButtonClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Event", "Button Clicked", "Ok");
        }


        private async void OnMapViewTapped(object sender, Esri.ArcGISRuntime.Maui.GeoViewInputEventArgs e)
        {
            MainMapView.DismissCallout();
            MainMapViewModel.FloorLayer?.ClearSelection();

            IReadOnlyList<IdentifyLayerResult> idResults = await MainMapView.IdentifyLayersAsync(e.Position, 10, false, 1);

            string? resultInfo = null;
            foreach (IdentifyLayerResult result in idResults)
            {
                if (result.LayerContent.Name == MainMapViewModel.FloorLayer?.Name)
                {
                    Feature room = (Feature)result.GeoElements[0];
                    MainMapViewModel.FloorLayer.SelectFeature(room);

                    var attributes = result.GeoElements[0].Attributes;
                    resultInfo = $"Building: {attributes["BuildingId"]}\nFloor: {attributes["Floor"]}";
                }
            }
            
            if (resultInfo != null)
            {
                MainMapView.ShowCalloutAt(e.Location, new CalloutDefinition(null, resultInfo));
            }
        }

        // These two methods are here because commands were not working for my buttons ¯\_(ツ)_/¯
        private void OnIncrementFloorButtonClicked(object sender, EventArgs e)
        {
            MainMapView.DismissCallout();
            MainMapViewModel.Floor++;
        }
        private void OnDecrementFloorButtonClicked(object sender, EventArgs e)
        {
            MainMapView.DismissCallout();
            MainMapViewModel.Floor--;
        }
    }
}
