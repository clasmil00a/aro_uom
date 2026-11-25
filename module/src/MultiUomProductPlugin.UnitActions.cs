using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Aronium.Pos.MultiUom
{
    public sealed partial class MultiUomProductPlugin
    {
        private void OnAddUnitRow(object sender, RoutedEventArgs e)
        {
            if (product == null || product.Id == null) { Show("Produk belum siap disimpan."); return; }
            var row = new ProductUnitOption
            {
                ProductId = Convert.ToInt32(product.Id, CultureInfo.InvariantCulture),
                UnitName = "", ConversionFactor = 1m, Price = 0m,
                IsDefault = false, IsEnabled = true
            };
            unitRows.Add(row);
            unitGrid.SelectedIndex = unitRows.Count - 1;
            unitGrid.CurrentCell = new DataGridCellInfo(row, unitGrid.Columns[0]);
            unitGrid.BeginEdit();
        }

        private void OnDeleteUnitRow(object sender, RoutedEventArgs e)
        {
            var row = unitGrid.SelectedItem as ProductUnitOption;
            if (row == null) { Show("Pilih baris satuan yang akan dihapus."); return; }
            if (!Confirm("Hapus satuan '" + row.UnitName + "' beserta tiernya?")) return;
            if (row.ProductUnitId > 0) removedUnitIds.Add(row.ProductUnitId);
            foreach (PriceTierOption tier in tierRows.Where(x => x.ProductUnitId == row.ProductUnitId).ToList())
            {
                if (tier.Id > 0) removedTierIds.Add(tier.Id);
                tierRows.Remove(tier);
            }
            unitRows.Remove(row);
        }
    }
}
