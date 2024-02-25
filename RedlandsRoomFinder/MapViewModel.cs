using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Floor;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace RedlandsRoomFinder
{
    /* TODO
     * isChoosingLocation: field for when user is clicking a point to choose their location
     * UseGPSLocationCommand: implement using gps location
     * Floor: Bind with floor picker and slider views
     * FloorLayerID: maybe search through feature layers to find the correct id on loading the map
     */
    internal class MapViewModel : INotifyPropertyChanged
    {
        private const int FloorLayerID = 2;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MapViewModel() {
            _ = Initialize();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Map? _map;
        public Map? Map
        {
            get { return _map; }
            set { _map = value; OnPropertyChanged(); }
        }


        private int _floor;
        public int Floor
        {
            set
            {
                _floor = value;
                FilterFloors(); 
                OnPropertyChanged();
            }
            get { return _floor; }
        }


        public Command FilterFloorCommand { private set; get; }

        //TODO: implement this:
        //public Command UseGPSLocationCommand { private set; get; }

        private async Task Initialize()
        {
            await SetUpMap();
            SetUpCommands();
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


        private void SetUpCommands()
        {
            FilterFloorCommand = new Command(
                execute: FilterFloors,
                canExecute: () => { return true; }
                );
        }

        private void FilterFloors()
        {
            var floorLayer = _map?.OperationalLayers[FloorLayerID] as FeatureLayer;
            if ( floorLayer == null ) { return; }
            floorLayer.DefinitionExpression = $"Floor = {_floor}";
        }
    }
}
