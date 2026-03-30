using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Enjaz.Models
{
    public class ReceptionSample : INotifyPropertyChanged
    {
        private int _id;
        private int _receptionId;
        private string _sampleNumber = string.Empty;
        private string _description = string.Empty;
        private string _root = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int Id 
        { 
            get => _id; 
            set { _id = value; OnPropertyChanged(); } 
        }

        public int ReceptionId 
        { 
            get => _receptionId; 
            set { _receptionId = value; OnPropertyChanged(); } 
        }

        public string SampleNumber 
        { 
            get => _sampleNumber; 
            set { _sampleNumber = value; OnPropertyChanged(); } 
        }

        public string Description 
        { 
            get => _description; 
            set { _description = value; OnPropertyChanged(); } 
        }

        public string Root 
        { 
            get => _root; 
            set { _root = value; OnPropertyChanged(); } 
        }
    }
}
