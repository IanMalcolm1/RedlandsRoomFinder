using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Floor;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace RedlandsRoomFinder
{
    /* TODO
     * isChoosingLocation: field for when user is clicking a point to choose their location
     * FloorLayerID: maybe search through feature layer names to find the correct id on loading the map
     */
    internal class MapViewModel : INotifyPropertyChanged
    {
        private const int FloorLayerID = 2;
        private const int TotalFloors = 4;

        private Map? _map;
        private FeatureLayer? _roomsLayer;
        private int _floor;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MapViewModel()
        {
            _ = Initialize();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Map? Map
        {
            get { return _map; }
            set { _map = value; OnPropertyChanged(); }
        }

        public int Floor
        {
            set
            {
                if (value>=0 && value<TotalFloors)
                {
                    _floor = value;
                    FilterFloors();
                    OnPropertyChanged();
                }
            }
            get { return _floor; }
        }

        private async Task Initialize()
        {
            await SetUpMap();
            _roomsLayer = (FeatureLayer?)_map?.OperationalLayers[FloorLayerID];
            Floor = 0;
        }

        private async Task SetUpMap()
        {
            /* Bundled assets are read-only. This copies the package to the AppDataDirectory
             * so that it can be modified if necessary *shrugs* */
            string packageName = "mobiletestpackage.mmpk";
            string newPath = await moveToAppDataDirectory(packageName);

            /* Map stuff */
            MobileMapPackage mapPackage = new MobileMapPackage(newPath);
            await mapPackage.LoadAsync();
            this.Map = mapPackage.Maps.FirstOrDefault();
        }

        private async Task<string> moveToAppDataDirectory(string fileName)
        {
            string directoryPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            if (File.Exists(directoryPath))
            {
                return directoryPath;
            }
            using Stream inStream = await FileSystem.Current.OpenAppPackageFileAsync(fileName);
            using FileStream outStream = File.Create(directoryPath);
            await inStream.CopyToAsync(outStream);

            return directoryPath;
        }

        private void FilterFloors()
        {
            if ( _roomsLayer== null ) { return; } //TODO: raise exception

            _roomsLayer.DefinitionExpression = $"Floor = {_floor}";
        }

        public async Task SelectLocation(MapPoint loc)
        {
            if (loc==null) { return; } //TODO: make exception
            if (_roomsLayer == null) { return; } //TODO: make exception

            _roomsLayer.ClearSelection();

            var queryParams = new QueryParameters
            {
                Geometry = loc,
                SpatialRelationship=SpatialRelationship.Intersects
            };

            var queryResult = await _roomsLayer.FeatureTable.QueryFeaturesAsync(queryParams);
            foreach (Feature feature in queryResult)
            {
                var featureFloor = (short) feature.Attributes["Floor"];
                if (featureFloor == _floor)
                {
                    _roomsLayer.SelectFeature(feature);
                }
            }
        }
    }
}
