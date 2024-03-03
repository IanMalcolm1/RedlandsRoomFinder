using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Floor;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI;
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
        private const int FloorLayerID = 1;
        private const int TotalFloors = 2;

        private Map? _map;
        private FeatureLayer? _roomsLayer;
        private int _floor;

        private RouteManager _routeManager;

        public MapViewModel()
        {
            _ = Initialize();

            //Commands must be set without async as otherwise they will be null when bound to calling views
            DecrementFloorCommand = new Command(
                execute: () => { Floor--; },
                canExecute: () => { return true; });
            IncrementFloorCommand = new Command(
                execute: () => { Floor++; },
                canExecute: () => { return true; });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Map? Map
        {
            get { return _map; }
            set { _map = value; OnPropertyChanged(); }
        }

        public Graphic? RouteGraphic
        {
            get => _routeManager?.RouteGraphic;
        }

        public Command DecrementFloorCommand { get; private set; }
        public Command IncrementFloorCommand { get; private set; }

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

            _routeManager = new RouteManager(_map.TransportationNetworks[0]);
            _routeManager.PropertyChanged += HandleRouteChangedEvent;
        }

        private async Task SetUpMap()
        {
            /* Bundled assets are read-only. This copies the package to the AppDataDirectory
             * so that it can be modified if necessary *shrugs* */
            string packageName = "testnetworkpackage.mmpk";
            string newPath = await moveToAppDataDirectory(packageName);

            /* Map stuff */
            MobileMapPackage mapPackage = new MobileMapPackage(newPath);
            await mapPackage.LoadAsync();
            this.Map = mapPackage.Maps.FirstOrDefault();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void HandleRouteChangedEvent(object? sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RouteGraphic)));
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

            if (_routeManager.StartStop == null)
            {
                _routeManager.StartStop = loc;
            }
            else
            {
                _routeManager.DestStop = loc;
            }

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
