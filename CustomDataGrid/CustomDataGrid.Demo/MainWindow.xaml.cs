using System.Collections.ObjectModel;
using System.Windows;
using Eneca.CustomDataGrid.Models;

namespace Eneca.CustomDataGrid.Demo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadSampleData();
            CheckboxColumnCheck.IsChecked = true;
            OnOptionChanged(null, null);
        }

        private void LoadSampleData()
        {
            DataGrid.Columns.Clear();
            DataGrid.Columns.Add(new DataGridColumnModel { Header = "ID", Width = 80 });
            DataGrid.Columns.Add(new DataGridColumnModel { Header = "Наименование", Width = 220 });
            DataGrid.Columns.Add(new DataGridColumnModel { Header = "Категория", Width = 160 });
            DataGrid.Columns.Add(new DataGridColumnModel { Header = "Статус", Width = 120 });
            DataGrid.Columns.Add(new DataGridColumnModel { Header = "Дата", Width = 140 });
            DataGrid.Columns.Add(new DataGridColumnModel { Header = "Ответственный", Width = 180 });

            DataGrid.FilterIconClick += (_, args) =>
                MessageBox.Show($"Фильтр: {args.Column.Header}", "FilterIconClick");

            DataGrid.HeaderCellClick += (_, args) =>
                MessageBox.Show($"Колонка: {args.Column.Header}, Clicked={args.Column.IsHeaderClicked}", "HeaderCellClick");

            DataGrid.Rows.Clear();
            AddRow("1", "Насос циркуляционный", "Оборудование", "В работе", "12.05.2026", "Иванов А.");
            AddRow("2", "Клапан запорный DN50", "Арматура", "На складе", "10.05.2026", "Петров С.");
            AddRow("3", "Датчик давления", "КИП", "Заказан", "08.05.2026", "Сидорова М.");
            AddRow("4", "Теплообменник пластинчатый", "Оборудование", "В работе", "05.05.2026", "Козлов Д.");
            AddRow("5", "Фильтр сетчатый", "Арматура", "На складе", "01.05.2026", "Иванов А.");
            AddRow("6", "Блок управления BMS", "Автоматика", "Монтаж", "28.04.2026", "Петров С.");
            AddRow("7", "Труба стальная Ø108", "Материалы", "На складе", "25.04.2026", "Сидорова М.");
            AddRow("8", "Изоляция минвата", "Материалы", "Заказан", "20.04.2026", "Козлов Д.");
        }

        private void AddRow(params string[] cells)
        {
            var row = new DataGridRowModel();
            foreach (var cell in cells)
                row.Cells.Add(cell);
            DataGrid.Rows.Add(row);
        }

        private void OnOptionChanged(object sender, RoutedEventArgs e)
        {
            DataGrid.FreezeFirstColumn = FreezeFirstColumnCheck.IsChecked == true;
            DataGrid.FirstColumnIsCheckBoxColumn = CheckboxColumnCheck.IsChecked == true;
        }
    }
}
