using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Eneca.CustomDataGrid.Models
{
    public class DataGridColumnModel : INotifyPropertyChanged
    {
        private string _header = string.Empty;
        private double _width = Theme.DataGridTheme.DefaultColumnWidth;
        private bool _showFilterIcon;
        private bool _isHeaderDisabled;
        private bool _isHeaderActive;
        private bool _isHeaderClicked;

        public string Header
        {
            get => _header;
            set { _header = value; OnPropertyChanged(); }
        }

        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        /// <summary>Shows filter icon in the column header (right side, 8px from edge).</summary>
        public bool ShowFilterIcon
        {
            get => _showFilterIcon;
            set { _showFilterIcon = value; OnPropertyChanged(); }
        }

        /// <summary>Persistent disabled state for the header cell (requires EnableHeaderSelection).</summary>
        public bool IsHeaderDisabled
        {
            get => _isHeaderDisabled;
            set { _isHeaderDisabled = value; OnPropertyChanged(); }
        }

        /// <summary>Persistent active state — highlighted header (requires EnableHeaderSelection).</summary>
        public bool IsHeaderActive
        {
            get => _isHeaderActive;
            set { _isHeaderActive = value; OnPropertyChanged(); }
        }

        /// <summary>Persistent clicked/selected state — highlighted header (requires EnableHeaderSelection).</summary>
        public bool IsHeaderClicked
        {
            get => _isHeaderClicked;
            set { _isHeaderClicked = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
