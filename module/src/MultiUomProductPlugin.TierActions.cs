using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Aronium.Pos.MultiUom
{
    public sealed partial class MultiUomProductPlugin
    {
        private void OnAddTierRow(object sender, RoutedEventArgs e)
        {
            if (product == null || product.Id == null) { Show("Produk belum siap disimpan."); return; }
            var row = new PriceTierOption
            {
                ProductId = Convert.ToInt32(product.Id, CultureInfo.InvariantCulture),
                UnitName = "", MinQuantity = 0m, Price = 0m
            };
            tierRows.Add(row);
            tierGrid.SelectedIndex = tierRows.Count - 1;
            tierGrid.CurrentCell = new DataGridCellInfo(row, tierGrid.Columns[0]);
            tierGrid.BeginEdit();
        }

        private void OnDeleteTierRow(object sender, RoutedEventArgs e)
        {
            var row = tierGrid.SelectedItem as PriceTierOption;
            if (row == null) { Show("Pilih tier yang akan dihapus."); return; }
            if (!Confirm("Hapus tier harga ini?")) return;
            if (row.Id > 0) removedTierIds.Add(row.Id);
            tierRows.Remove(row);
        }
    }
}
