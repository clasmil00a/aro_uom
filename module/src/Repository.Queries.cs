using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace Aronium.Pos.MultiUom
{
    internal sealed partial class MultiUomRepository
    {
        public List<ProductUnitOption> GetProductUnits(int productId)
        {
            string sql = "SELECT PU.Id,PU.ProductId,PU.UnitId,U.Name,U.Symbol,PU.ConversionFactor,PU.Price,PU.IsBaseUnit,PU.IsDefault,PU.IsEnabled " +
                "FROM ProductUnit PU INNER JOIN Unit U ON U.Id=PU.UnitId " +
                "WHERE PU.ProductId=" + productId + " AND PU.IsEnabled=1 AND U.IsEnabled=1 " +
                "ORDER BY PU.IsDefault DESC,PU.IsBaseUnit DESC,U.Name";
            return db.ExecuteReader<List<ProductUnitOption>>(sql, null, delegate(IDataReader r)
            {
                var list = new List<ProductUnitOption>();
                while (r.Read())
                {
                    list.Add(new ProductUnitOption
                    {
                        ProductUnitId = Convert.ToInt32(r.GetValue(0), CultureInfo.InvariantCulture),
                        ProductId = Convert.ToInt32(r.GetValue(1), CultureInfo.InvariantCulture),
                        UnitId = Convert.ToInt32(r.GetValue(2), CultureInfo.InvariantCulture),
                        UnitName = Convert.ToString(r.GetValue(3), CultureInfo.InvariantCulture),
                        OriginalUnitName = Convert.ToString(r.GetValue(3), CultureInfo.InvariantCulture),
                        UnitSymbol = Convert.ToString(r.GetValue(4), CultureInfo.InvariantCulture),
                        ConversionFactor = Convert.ToDecimal(r.GetValue(5), CultureInfo.InvariantCulture),
                        Price = Convert.ToDecimal(r.GetValue(6), CultureInfo.InvariantCulture),
                        IsBaseUnit = Convert.ToInt32(r.GetValue(7), CultureInfo.InvariantCulture) != 0,
                        IsDefault = Convert.ToInt32(r.GetValue(8), CultureInfo.InvariantCulture) != 0,
                        IsEnabled = Convert.ToInt32(r.GetValue(9), CultureInfo.InvariantCulture) != 0
                    });
                }
                return list;
            });
        }

        public List<PriceTierOption> GetTiers(int productId, int productUnitId)
        {
            string sql = "SELECT t.Id,t.ProductUnitId,t.MinQuantity,t.Price,t.CurrencyId,COALESCE(u.Name,'(semua satuan)'),COALESCE(u.Symbol,'') " +
                "FROM ProductPriceTier t LEFT JOIN ProductUnit pu ON pu.Id=t.ProductUnitId LEFT JOIN Unit u ON u.Id=pu.UnitId " +
                "WHERE t.ProductId=" + productId + " AND (t.ProductUnitId IS NULL OR t.ProductUnitId=" + productUnitId + ")";
            return db.ExecuteReader<List<PriceTierOption>>(sql, null, delegate(IDataReader r)
            {
                var list = new List<PriceTierOption>();
                while (r.Read())
                {
                    list.Add(new PriceTierOption
                    {
                        Id = Convert.ToInt32(r.GetValue(0), CultureInfo.InvariantCulture),
                        ProductUnitId = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1), CultureInfo.InvariantCulture),
                        MinQuantity = Convert.ToDecimal(r.GetValue(2), CultureInfo.InvariantCulture),
                        Price = Convert.ToDecimal(r.GetValue(3), CultureInfo.InvariantCulture),
                        CurrencyId = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4), CultureInfo.InvariantCulture),
                        UnitName = Convert.ToString(r.GetValue(5), CultureInfo.InvariantCulture),
                        UnitSymbol = Convert.ToString(r.GetValue(6), CultureInfo.InvariantCulture)
                    });
                }
                return list;
            });
        }
    }
}
