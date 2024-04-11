using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Diagnostics;

using Color = System.Drawing.Color;

namespace RedlandsRoomFinder
{
    internal class RouteManager
    {
        private RouteTask? _router;

        private MapPoint? _startStop;
        private MapPoint? _destStop;
        private List<RouteLine> _routeLines; //lines from splitting a route by floor

        private int _floorHeight;


        public RouteManager(TransportationNetworkDataset networkDataset, int floorHeight)
        {
            _floorHeight = floorHeight;

            _ = Initialize(networkDataset);
        }

        public event EventHandler? RouteGeometryChanged;
        public event EventHandler RouteNotFound;

        public List<RouteLine> RouteLines
        {
            get { return _routeLines; }
            private set
            {
                _routeLines = value;
            }
        }

        public MapPoint? StartStop
        {
            get { return _startStop; }
            set
            {
                _startStop = value;
                _ = TryMakeRoute();
            }
        }

        public MapPoint? DestStop
        {
            get { return _destStop; }
            set
            {
                _destStop = value;
                _ = TryMakeRoute();
            }
        }

        private async Task Initialize(TransportationNetworkDataset networkDataset)
        {
            _routeLines = new List<RouteLine>();
            _router = await RouteTask.CreateAsync(networkDataset);
            if ( _router == null )
            {
                throw new Exception("RouterTask could not be made");
            }
        }

        private async Task<String> moveResourceToAppData(string fileName)
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

        private async Task TryMakeRoute()
        {
            if (_startStop == null || _destStop == null || _router == null)
            {
                processGeometryChange();
                return;
            }

            var routeParams = await _router.CreateDefaultParametersAsync();

            var stops = new[]
            {
                new Stop(_startStop),
                new Stop(_destStop)
            };

            routeParams.SetStops(stops);

            try
            {
                var routeResult = await _router.SolveRouteAsync(routeParams);

                if (routeResult?.Routes?.FirstOrDefault() is Route route)
                {
                    SimpleLineSymbol routeSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.LightBlue, 2.0);
                    processGeometryChange(route.RouteGeometry);
                }
            } catch (ArcGISRuntimeException e)
            {
                Debug.WriteLine(e.Message);
                processGeometryChange();
                RouteNotFound.Invoke(this, new EventArgs());
            }
        }

        private void processGeometryChange()
        {
            RouteGeometryChanged?.Invoke(this, new PropertyChangedEventArgs("RouteGeometry"));
        }

        private void processGeometryChange(Polyline routeLine)
        {
            var splitLines = new List<RouteLine>();

            //split routeLine by vertex elevation
            var currLinePoints = new List<MapPoint>();
            var currFloor = 1;
            foreach(MapPoint point in routeLine.Parts[0].Points)
            {
                currLinePoints.Add(point);
                if ((int)Math.Round(point.Z) % _floorHeight!=0)
                {
                    splitLines.Add( new RouteLine( new Polyline(currLinePoints, routeLine.SpatialReference), currFloor) );
                    currLinePoints = new List<MapPoint> { point };
                }
                else
                {
                    currFloor = 1 + (int)Math.Round(point.Z) / _floorHeight;
                }
            }

            splitLines.Add(new RouteLine( new Polyline(currLinePoints, routeLine.SpatialReference), currFloor ));

            RouteLines = splitLines;
            RouteGeometryChanged?.Invoke(this, new PropertyChangedEventArgs("RouteGeometry"));
        }

        public struct RouteLine
        {
            public Polyline line;
            public int floor;

            public RouteLine( Polyline line, int floor )
            {
                this.line = line;
                this.floor = floor;
            }
        }
    }
}
