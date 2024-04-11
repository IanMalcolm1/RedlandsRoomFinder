using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace RedlandsRoomFinder
{
    /* TODO
     * FloorLayerID: maybe search through feature layer names to find the correct id on loading the map
     */
    internal class MapViewModel : INotifyPropertyChanged
    {
        private const String MAP_PACKAGE_NAME = "RedlandsRoomMapv0-5.mmpk";
        private const String ROOMS_LAYER_NAME = "Indoor Spaces";
        private const String FLOOR_ATTRIBUTE_NAME = "FloorNum";
        private const int TOTAL_FLOORS = 6;
        private const int FLOOR_HEIGHT = 100;

        private int RoomsLayerIndex;

        private Map? _map;
        private FeatureLayer? _roomsLayer;
        private int _floor;

        private RouteManager _routeManager;
        private List<Graphic> _routeGraphics;
        private SimpleMarkerSymbol _startStopSymbol;
        private SimpleMarkerSymbol _destStopSymbol;
        private SimpleLineSymbol _routeLineSymbol;

        private WaitingStates _waitingState;

        public MapViewModel()
        {
            _ = Initialize();

            _startStopSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 7);
            _destStopSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Gold, 7);
            _routeLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.LightBlue, 2);

            _waitingState = WaitingStates.NotWaiting;

            //Commands must be set without async as otherwise they will be null when bound to calling views
            ChangeFloorCommand = new Command<Char>(
                execute:
                (Char c) => {
                    if (c == '+')
                        Floor++;
                    if (c == '-')
                        Floor--;
                });
            ChooseLocationCommand = new Command<Int32>(
                execute:
                (Int32 c) =>
                {
                    WaitingState = (WaitingStates) c;
                });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? RouteNotFound;

        public Map? Map
        {
            get { return _map; }
            set { _map = value; OnPropertyChanged(); }
        }

        public List<Graphic> RouteGraphics
        {
            get => _routeGraphics;
            private set
            {
                _routeGraphics = value;
                OnPropertyChanged(nameof(RouteGraphics));
            }
        }

        public WaitingStates WaitingState
        {
            get { return _waitingState; }
            private set {
                _waitingState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWaitingForStopInput)));
            }
        }

        public bool IsWaitingForStopInput
        {
            get { return (_waitingState != WaitingStates.NotWaiting); }
        }

        public Command<Char> ChangeFloorCommand { get; private set; }
        public Command<Int32> ChooseLocationCommand { get; private set; }

        public int Floor
        {
            set
            {
                if (value>=0 && value<TOTAL_FLOORS)
                {
                    _floor = value;
                    FilterFloors();
                    updateRouteGraphics();
                    OnPropertyChanged();
                }
            }
            get { return _floor; }
        }

        private async Task Initialize()
        {
            await SetUpMap();
            RoomsLayerIndex = getRoomsLayerIndex();
            _roomsLayer = (FeatureLayer?)_map?.OperationalLayers[RoomsLayerIndex];
            Floor = 1;

            _routeManager = new RouteManager(_map.TransportationNetworks[0], FLOOR_HEIGHT);
            _routeManager.RouteGeometryChanged += HandleRouteChangedEvent;
            _routeManager.RouteNotFound += HandleRouteNotFoundEvent;
        }

        private async Task SetUpMap()
        {
            /* Bundled assets are read-only. This copies the package to the AppDataDirectory
             * so that it can be modified if necessary *shrugs* */
            string newPath = await moveToAppDataDirectory(MAP_PACKAGE_NAME);

            /* Map stuff */
            MobileMapPackage mapPackage = new MobileMapPackage(newPath);
            await mapPackage.LoadAsync();
            this.Map = mapPackage.Maps.FirstOrDefault();
        }

        private int getRoomsLayerIndex()
        {
            for (int i=0; i<_map?.OperationalLayers.Count; i++)
            {
                if (_map.OperationalLayers[i].Name.Equals(ROOMS_LAYER_NAME))
                    return i;
            }
            return -1;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void HandleRouteChangedEvent(object? sender, EventArgs e)
        {
            updateRouteGraphics();
        }

        private void HandleRouteNotFoundEvent(object? sender, EventArgs e)
        {
            RouteNotFound?.Invoke(this, e);
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
            _roomsLayer.DefinitionExpression = $"{FLOOR_ATTRIBUTE_NAME} = {_floor}";
        }

        public async Task SelectLocation(MapPoint loc)
        {
            if (loc==null) { return; } //TODO: raise exception?
            if (_roomsLayer == null) { return; } //TODO: raise exception?

            _roomsLayer.ClearSelection();

            var queryParams = new QueryParameters
            {
                Geometry = loc,
                SpatialRelationship=SpatialRelationship.Intersects
            };

            var queryResult = await _roomsLayer.FeatureTable.QueryFeaturesAsync(queryParams);
            foreach (Feature feature in queryResult)
            {
                var featureFloor = (short) feature.Attributes[FLOOR_ATTRIBUTE_NAME];
                if (featureFloor == _floor)
                {
                    _roomsLayer.SelectFeature(feature);
                }
            }

            var loc_z = new MapPoint(loc.X, loc.Y, (_floor - 1) * FLOOR_HEIGHT, loc.SpatialReference);

            switch (_waitingState)
            {
                case WaitingStates.NotWaiting:
                    return;
                case WaitingStates.Start:
                    _routeManager.StartStop = loc_z;
                    break;
                case WaitingStates.Dest:
                    _routeManager.DestStop = loc_z;
                    break;
            }
        }

        private void updateRouteGraphics()
        {
            if (_routeManager == null)
            {
                return;
            }

            List<Graphic> graphics = new List<Graphic>()
            {
                new Graphic(_routeManager.StartStop, _startStopSymbol),
                new Graphic(_routeManager.DestStop, _destStopSymbol)
            };

            foreach(RouteManager.RouteLine line in _routeManager.RouteLines)
            {
                if (line.floor == Floor)
                {
                    graphics.Add(new Graphic(line.line, _routeLineSymbol));
                }
            }

            RouteGraphics = graphics;
        }
        public enum WaitingStates
        {
            Start=0,
            Dest=1,
            NotWaiting=2
        }
    }
}
