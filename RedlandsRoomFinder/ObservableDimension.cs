using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace RedlandsRoomFinder
{
    public class ObservableDimension : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableDimension()
        {
            dimension = 1;
        }

        private double dimension;

        public double Dimension
        {
            get { return Math.Max(dimension,1); }
            set
            {
                dimension = value;
                OnPropertyChanged();
            }
        }

        public void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dimension)));
        }
    }
}
