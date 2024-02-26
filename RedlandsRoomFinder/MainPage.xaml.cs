using CommunityToolkit.Mvvm.ComponentModel;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System.ComponentModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RedlandsRoomFinder
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            MapPoint mapCenterPoint = new MapPoint(34.063367, -117.164185, SpatialReferences.Wgs84);
            MainMapView.SetViewpoint(new Viewpoint(mapCenterPoint, 100000));
        }


        async private void OnButtonClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Event", "Button Clicked", "Ok");
        }


        // These methods are here because commands were not working for my buttons ¯\_(ツ)_/¯
        private void OnIncrementFloorButtonClicked(object sender, EventArgs e)
        {
            MainMapViewModel.Floor++;
        }
        private void OnDecrementFloorButtonClicked(object sender, EventArgs e)
        {
            MainMapViewModel.Floor--;
        }
    }
}
