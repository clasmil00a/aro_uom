using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Aronium.Modularity;
using Aronium.Pos.Core;
using Aronium.Pos.Core.Models;

namespace Aronium.Pos.MultiUom
{
    public sealed partial class MultiUomProductPlugin : UserControl, IProductPlugin, IVisualPlugin
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(MultiUomProductPlugin));
        private static readonly MoneyConverter Money = new MoneyConverter();
        private readonly DataGrid unitGrid;
        private readonly DataGrid tierGrid;
        private ScrollViewer pageScroller;
        private readonly ObservableCollection<ProductUnitOption> unitRows = new ObservableCollection<ProductUnitOption>();
        private readonly ObservableCollection<PriceTierOption> tierRows = new ObservableCollection<PriceTierOption>();
        private readonly HashSet<int> removedUnitIds = new HashSet<int>();
        private readonly HashSet<int> removedTierIds = new HashSet<int>();
        private Product product;

        public MultiUomProductPlugin()
        {
            unitGrid = new DataGrid
            {
                Height = 220, AutoGenerateColumns = false, IsReadOnly = false,
                RowHeight = 36, FontSize = 13.5,
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                SelectionMode = DataGridSelectionMode.Single, CanUserAddRows = false, CanUserDeleteRows = false
            };
            unitGrid.Columns.Add(EditColumn("Satuan", "UnitName", 155));
            unitGrid.Columns.Add(EditColumn("Faktor", "ConversionFactor", 80));
            unitGrid.Columns.Add(MoneyColumn("Harga", "Price", 100));
            unitGrid.Columns.Add(CheckTemplateColumn("Default", "IsDefault", 85));
            unitGrid.Columns.Add(CheckTemplateColumn("Aktif", "IsEnabled", 70));

            tierGrid = new DataGrid
            {
                Height = 190, AutoGenerateColumns = false, IsReadOnly = false,
                RowHeight = 36, FontSize = 13.5,
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                SelectionMode = DataGridSelectionMode.Single, CanUserAddRows = false, CanUserDeleteRows = false
            };
            tierGrid.Columns.Add(EditColumn("Satuan", "UnitName", 135));
            tierGrid.Columns.Add(EditColumn("Min. Qty", "MinQuantity", 100));
            tierGrid.Columns.Add(MoneyColumn("Harga / unit", "Price", 145));

            BuildLayout();
            ApplyGridStyle(unitGrid);
            ApplyGridStyle(tierGrid);
            unitGrid.PreviewMouseWheel += OnGridPreviewMouseWheel;
            tierGrid.PreviewMouseWheel += OnGridPreviewMouseWheel;
        }

        public Product Product
        {
            get { return product; }
            set { product = value; RefreshData(); }
        }

        public string DisplayName { get { return "Satuan & Harga"; } }
        public int DisplayIndex { get { return 3; } }
        public void Initialize() { RefreshData(); }
        public bool Save() { return SaveAll(false); }

        private static DataGridTextColumn EditColumn(string header, string path, double width)
        {
            return new DataGridTextColumn
            {
                Header = header, Width = width,
                Binding = new Binding(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus }
            };
        }

        private static DataGridTextColumn MoneyColumn(string header, string path, double width)
        {
            return new DataGridTextColumn
            {
                Header = header, Width = width,
                Binding = new Binding(path)
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                    Converter = Money
                }
            };
        }

        private static DataGridCheckBoxColumn CheckColumn(string header, string path, double width)
        {
            return new DataGridCheckBoxColumn
            {
                Header = header, Width = width,
                Binding = new Binding(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
            };
        }
    }
}
