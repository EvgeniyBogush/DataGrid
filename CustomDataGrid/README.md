# Eneca Custom DataGrid

WPF-библиотека кастомного DataGrid с настраиваемыми опциями и отдельным демо-проектом для просмотра и отладки.

## Структура решения

| Проект | Назначение |
|--------|------------|
| `CustomDataGrid` → сборка **Eneca.CustomDataGrid** | Библиотека контрола |
| `CustomDataGrid.Demo` | Запускаемое приложение для проверки и настройки |

```
CustomDataGrid/
├── Controls/EnecaDataGrid.xaml    — основной контрол
├── Models/                        — колонки и строки
└── Theme/DataGridTheme.cs         — цвета, размеры, шрифты

CustomDataGrid.Demo/
└── MainWindow.xaml                — чекбоксы опций + пример данных
```

## Запуск демо

1. Откройте `CustomDataGrid.sln` в Visual Studio 2022.
2. Стартовый проект — **Eneca.CustomDataGrid.Demo** (не библиотека `Eneca.CustomDataGrid`).
   - Если при F5 появляется ошибка «библиотеку классов нельзя запустить»: в обозревателе решений ПКМ по **Eneca.CustomDataGrid.Demo** → **Назначить запускаемым проектом**.
   - Либо в выпадающем списке запуска на панели инструментов выберите профиль **Demo** (файл `CustomDataGrid.slnLaunch`).
3. Соберите и запустите (F5).

## Опции контрола

| Свойство | Тип | Описание |
|----------|-----|----------|
| `FreezeFirstColumn` | `bool` | Первый столбец (чекбокс или данные) закреплён слева, остальные прокручиваются |
| `FirstColumnIsCheckBoxColumn` | `bool` | Первый столбец — чекбоксы выбора строк |
| `Columns` | `ObservableCollection<DataGridColumnModel>` | Заголовки и ширины колонок данных |
| `Rows` | `ObservableCollection<DataGridRowModel>` | Строки (`Cells` + `IsChecked`) |

Пример в XAML:

```xml
xmlns:grid="clr-namespace:Eneca.CustomDataGrid.Controls;assembly=Eneca.CustomDataGrid"

<grid:EnecaDataGrid FreezeFirstColumn="True"
                    FirstColumnIsCheckBoxColumn="True"
                    x:Name="MyGrid" />
```

## Интеграция в свою библиотеку

1. Добавьте ссылку на проект `CustomDataGrid` или на DLL `Eneca.CustomDataGrid`.
2. Подключите namespace `Eneca.CustomDataGrid.Controls`.
3. При необходимости скопируйте шрифты Montserrat в ресурсы приложения (сейчас используется fallback `Segoe UI`).

## Шрифт Montserrat

Установите Montserrat в Windows или добавьте `.ttf` в демо/хост-приложение и укажите pack URI в `DataGridTheme.Montserrat`.
