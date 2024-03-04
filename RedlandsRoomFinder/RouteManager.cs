using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI;
using Microsoft.Maui.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedlandsRoomFinder
{
    internal class RouteManager : INotifyPropertyChanged
    {
        private RouteTask? _router;
        private Graphic? _routeGraphic;

        private MapPoint? _startStop;
        private MapPoint? _destStop;


        public RouteManager(TransportationNetworkDataset networkDataset)
        {
            _ = Initialize(networkDataset);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Graphic? RouteGraphic
        {
            get { return _routeGraphic; }
            private set
            {
                _routeGraphic = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RouteGraphic)));
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
            _router = await RouteTask.CreateAsync(networkDataset);
            if ( _router == null )
            {
                throw new Exception();
            }
        }

        private async Task TryMakeRoute()
        {
            if (_startStop == null || _destStop == null || _router == null)
                return;

            var routeParams = await _router.CreateDefaultParametersAsync();

            var stops = new[]
            {
                new Stop(_startStop),
                new Stop(_destStop)
            };

            routeParams.SetStops(stops);

            var routeResult = await _router.SolveRouteAsync(routeParams);
            if (routeResult?.Routes?.FirstOrDefault() is Route route)
            {
                RouteGraphic = new Graphic(route.RouteGeometry);
            }
        }
    }
}
