using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Aronium.Pos.MultiUom
{
    public sealed partial class MultiUomProductPlugin
    {
        private List<UnitDefinition> unitDefinitions = new List<UnitDefinition>();

        private void RefreshData()
        {
            unitRows.Clear();
            tierRows.Clear();
            removedUnitIds.Clear();
            removedTierIds.Clear();

            MultiUomRepository repository = MultiUomRuntime.Repository;
            if (repository == null || product == null || product.Id == null)
            {
                unitDefinitions = new List<UnitDefinition>();
                unitGrid.ItemsSource = unitRows;
                tierGrid.ItemsSource = tierRows;
                return;
            }

            int productId = Convert.ToInt32(product.Id, CultureInfo.InvariantCulture);
            foreach (ProductUnitOption row in repository.GetProductUnits(productId))
                unitRows.Add(row);
            foreach (PriceTierOption row in repository.GetAllTiers(productId))
                tierRows.Add(row);
            unitDefinitions = repository.GetUnits();
            unitGrid.ItemsSource = unitRows;
            tierGrid.ItemsSource = tierRows;
        }

        private bool CommitTableEdits()
        {
            try
            {
                unitGrid.CommitEdit();
                tierGrid.CommitEdit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Ready(MultiUomRepository repository)
        {
            if (repository == null || product == null || product.Id == null)
            {
                Show("Produk belum siap disimpan.");
                return false;
            }
            return true;
        }

        private void Show(string message)
        {
            Window owner = Application.Current == null ? null : Application.Current.MainWindow;
            if (owner == null) MessageBox.Show(message, "Multi-UOM");
            else MessageBox.Show(owner, message, "Multi-UOM");
        }

        private bool Confirm(string message)
        {
            Window owner = Application.Current == null ? null : Application.Current.MainWindow;
            if (owner == null)
                return MessageBox.Show(message, "Multi-UOM", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            return MessageBox.Show(owner, message, "Multi-UOM", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}
