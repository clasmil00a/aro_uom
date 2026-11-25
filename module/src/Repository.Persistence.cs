using System;
using System.Collections.Generic;
using System.Globalization;
using Aronium.Pos.Core.Models;

namespace Aronium.Pos.MultiUom
{
    internal sealed partial class MultiUomRepository
    {
        public void SaveOrderMetadata(Order order)
        {
            if (order == null) return;
            foreach (OrderItem item in order)
            {
                if (item == null || item.Id == null || item.Product == null) continue;
                MultiUomSelection selection = MultiUomSelection.FromPackage(item.Product.Package);
                if (selection == null) continue;
                Upsert("PosOrderItemMultiUom", "PosOrderItemId", ToLong(item.Id), item.Product.Id, selection);
            }
        }

        public void SaveDocumentMetadata(Document document)
        {
            if (document == null || document.Items == null) return;
            foreach (DocumentItem item in document.Items)
            {
                if (item == null || item.Id == null || item.Product == null) continue;
                MultiUomSelection selection = MultiUomSelection.FromPackage(item.Product.Package);
                if (selection == null) continue;
                Upsert("DocumentItemMultiUom", "DocumentItemId", ToLong(item.Id), item.Product.Id, selection);
            }
        }

        private void Upsert(string table, string idColumn, long lineId, object productId, MultiUomSelection s)
        {
            string sql = "INSERT OR REPLACE INTO " + table + " (" +
                idColumn + ",ProductId,ProductUnitId,UnitId,ConversionFactor,SelectedQuantity," +
                "SelectedUnitPrice,BaseQuantity,BaseUnitPrice,AppliedTierId,AppliedTierMinQuantity,AppliedTierPrice) VALUES (" +
                lineId.ToString(CultureInfo.InvariantCulture) + "," + ToLong(productId) + "," +
                s.ProductUnitId + "," + s.UnitId + "," + N(s.ConversionFactor) + "," +
                N(s.SelectedQuantity) + "," + N(s.SelectedUnitPrice) + "," + N(s.BaseQuantity) + "," +
                N(s.BaseUnitPrice) + "," + Nullable(s.AppliedTierId) + "," +
                Nullable(s.AppliedTierMinQuantity) + "," + Nullable(s.AppliedTierPrice) + ")";
            Execute(sql);
        }

        private static long ToLong(object value)
        {
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        private static string N(decimal value)
        {
            return value.ToString("G29", CultureInfo.InvariantCulture);
        }

        private static string Nullable(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
        }

        private static string Nullable(decimal? value)
        {
            return value.HasValue ? N(value.Value) : "NULL";
        }
    }
}
