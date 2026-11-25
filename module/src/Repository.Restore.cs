using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Aronium.Pos.Core.Models;

namespace Aronium.Pos.MultiUom
{
    internal sealed partial class MultiUomRepository
    {
        public void RestoreOrderMetadata(Order order)
        {
            if (order == null) return;
            foreach (OrderItem item in order)
            {
                if (item == null || item.Id == null || item.Product == null) continue;
                MultiUomSelection selection = GetSelection("PosOrderItemMultiUom", "PosOrderItemId", ToLong(item.Id));
                if (selection != null)
                {
                    item.Product.Package = selection.ToPackage();
                    item.VoidReason = "__MULTIUOM_" + selection.ProductUnitId;
                }
            }
        }

        public MultiUomSelection GetOrderSelection(long orderItemId)
        {
            return GetSelection("PosOrderItemMultiUom", "PosOrderItemId", orderItemId);
        }

        public MultiUomSelection GetDocumentSelection(long documentItemId)
        {
            return GetSelection("DocumentItemMultiUom", "DocumentItemId", documentItemId);
        }

        private MultiUomSelection GetSelection(string table, string idColumn, long lineId)
        {
            string sql = "SELECT ProductUnitId,UnitId,ConversionFactor,SelectedQuantity,SelectedUnitPrice," +
                "BaseQuantity,BaseUnitPrice,AppliedTierId,AppliedTierMinQuantity,AppliedTierPrice FROM " +
                table + " WHERE " + idColumn + "=" + lineId.ToString(CultureInfo.InvariantCulture);
            return db.ExecuteReader<MultiUomSelection>(sql, null, delegate(IDataReader r)
            {
                if (!r.Read()) return null;
                int tierId = r.IsDBNull(7) ? 0 : Convert.ToInt32(r.GetValue(7), CultureInfo.InvariantCulture);
                return new MultiUomSelection
                {
                    ProductUnitId = Convert.ToInt32(r.GetValue(0), CultureInfo.InvariantCulture),
                    UnitId = Convert.ToInt32(r.GetValue(1), CultureInfo.InvariantCulture),
                    ConversionFactor = Convert.ToDecimal(r.GetValue(2), CultureInfo.InvariantCulture),
                    SelectedQuantity = Convert.ToDecimal(r.GetValue(3), CultureInfo.InvariantCulture),
                    SelectedUnitPrice = Convert.ToDecimal(r.GetValue(4), CultureInfo.InvariantCulture),
                    BaseQuantity = Convert.ToDecimal(r.GetValue(5), CultureInfo.InvariantCulture),
                    BaseUnitPrice = Convert.ToDecimal(r.GetValue(6), CultureInfo.InvariantCulture),
                    AppliedTierId = tierId == 0 ? (int?)null : tierId,
                    AppliedTierMinQuantity = r.IsDBNull(8) ? (decimal?)null : Convert.ToDecimal(r.GetValue(8), CultureInfo.InvariantCulture),
                    AppliedTierPrice = r.IsDBNull(9) ? (decimal?)null : Convert.ToDecimal(r.GetValue(9), CultureInfo.InvariantCulture)
                };
            });
        }
    }
}
