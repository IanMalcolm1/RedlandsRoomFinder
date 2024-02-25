using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Esri.ArcGISRuntime.Mapping;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace RedlandsRoomFinder
{
    internal class MapViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public MapViewModel() {
            _ =  SetUpMap();
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
    }
}
