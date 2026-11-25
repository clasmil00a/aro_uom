using System;
using System.Collections.Generic;
using System.Windows;
using Aronium.Pos.Core;
using Aronium.Pos.Core.Models;

namespace Aronium.Pos.MultiUom
{
    public sealed class MultiUomOrderItemPreprocessor : IOrderItemPreprocessor
    {
        public void Initialize()
        {
        }

        public void Process(Order order, OrderItem item)
        {
            MultiUomRepository repository = MultiUomRuntime.Repository;
            if (repository == null || item == null || item.Product == null || item.Product.Id == null)
                return;

            List<ProductUnitOption> units = repository.GetProductUnits(Convert.ToInt32(item.Product.Id));
            if (units == null || units.Count == 0)
                return;

            ProductUnitOption selected = units[0];
            decimal quantity = item.Quantity > 0 ? item.Quantity : 1m;
            if (units.Count > 1)
            {
                var window = new UnitSelectionWindow(units, quantity);
                Window owner = Application.Current == null ? null : Application.Current.MainWindow;
                if (owner != null && owner != window) window.Owner = owner;
                bool? result = window.ShowDialog();
                selected = window.SelectedUnit ?? selected;
                quantity = window.Quantity > 0 ? window.Quantity : quantity;
            }

            List<PriceTierOption> tiers = repository.GetTiers(Convert.ToInt32(item.Product.Id), selected.ProductUnitId);
            MultiUomSelection selection = MultiUomPricing.Resolve(selected, quantity, item.Product.Price, tiers);
            item.Quantity = selection.BaseQuantity;
            item.Price = selection.BaseUnitPrice;
            item.Product.Package = selection.ToPackage();

            // Order.AddItem membandingkan VoidReason saat menggabungkan item yang
            // memiliki produk/harga sama. Penanda internal ini mencegah kg dan box
            // dengan harga dasar sama digabung menjadi satu baris.
            item.VoidReason = "__MULTIUOM_" + selection.ProductUnitId;
        }
    }
}
