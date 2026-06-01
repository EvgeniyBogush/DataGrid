using Eneca.CustomDataGrid.Models;

namespace Eneca.CustomDataGrid.Controls
{
    public class DataGridColumnEventArgs : System.EventArgs
    {
        public DataGridColumnEventArgs(int columnIndex, DataGridColumnModel column)
        {
            ColumnIndex = columnIndex;
            Column = column;
        }

        public int ColumnIndex { get; }
        public DataGridColumnModel Column { get; }
    }
}
