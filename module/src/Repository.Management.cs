using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace Aronium.Pos.MultiUom
{
    internal sealed partial class MultiUomRepository
    {
        public List<UnitDefinition> GetUnits()
        {
            return db.ExecuteReader<List<UnitDefinition>>(
                "SELECT Id,Name,Symbol,IsEnabled FROM Unit ORDER BY Name", null,
                delegate(IDataReader r)
                {
                    var list = new List<UnitDefinition>();
                    while (r.Read())
                        list.Add(new UnitDefinition
                        {
                            Id = Convert.ToInt32(r.GetValue(0), CultureInfo.InvariantCulture),
                            Name = Convert.ToString(r.GetValue(1), CultureInfo.InvariantCulture),
                            Symbol = Convert.ToString(r.GetValue(2), CultureInfo.InvariantCulture),
                            IsEnabled = Convert.ToInt32(r.GetValue(3), CultureInfo.InvariantCulture) != 0
                        });
                    return list;
                });
        }

        public int AddUnit(string name, string symbol)
        {
            Execute("INSERT INTO Unit(Name,Symbol,IsEnabled) VALUES('" + Text(name) + "','" + Text(symbol) + "',1)");
            var units = GetUnits();
            foreach (UnitDefinition u in units)
                if (u.Name == name && u.Symbol == symbol) return u.Id;
            return 0;
        }

        public void SaveProductUnit(ProductUnitOption value)
        {
            int productId = Convert.ToInt32(value.ProductId, CultureInfo.InvariantCulture);
            if (value.UnitId <= 0) throw new InvalidOperationException("Satuan produk belum memiliki satuan yang valid.");
            int existingId = FindProductUnitId(productId, value.UnitId);
            if (value.ProductUnitId > 0 && existingId > 0 && existingId != value.ProductUnitId)
                throw new InvalidOperationException("Satuan tersebut sudah digunakan oleh produk ini.");

            if (value.IsBaseUnit)
                Execute("UPDATE ProductUnit SET IsBaseUnit=0 WHERE ProductId=" + productId);
            if (value.IsDefault)
                Execute("UPDATE ProductUnit SET IsDefault=0 WHERE ProductId=" + productId);

            int targetId = existingId > 0 ? existingId : value.ProductUnitId;
            if (targetId > 0)
            {
                int affected = Execute("UPDATE ProductUnit SET UnitId=" + value.UnitId + ",ConversionFactor=" + N(value.ConversionFactor) +
                    ",Price=" + N(value.Price) + ",IsBaseUnit=" + Bit(value.IsBaseUnit) + ",IsDefault=" + Bit(value.IsDefault) +
                    ",IsEnabled=" + Bit(value.IsEnabled) + " WHERE Id=" + targetId + " AND ProductId=" + productId);
                if (affected > 0)
                {
                    value.ProductUnitId = targetId;
                    return;
                }
            }

            Execute("INSERT INTO ProductUnit(ProductId,UnitId,ConversionFactor,Price,IsBaseUnit,IsDefault,IsEnabled) VALUES(" +
                productId + "," + value.UnitId + "," + N(value.ConversionFactor) + "," + N(value.Price) + "," +
                Bit(value.IsBaseUnit) + "," + Bit(value.IsDefault) + "," + Bit(value.IsEnabled) + ")");
            value.ProductUnitId = FindProductUnitId(productId, value.UnitId);
        }

        private int FindProductUnitId(int productId, int unitId)
        {
            return db.ExecuteScalar<int>("SELECT Id FROM ProductUnit WHERE ProductId=" + productId +
                " AND UnitId=" + unitId + " LIMIT 1", null);
        }

        public void DeleteProductUnit(int productId, int productUnitId)
        {
            Execute("DELETE FROM ProductUnit WHERE Id=" + productUnitId + " AND ProductId=" + productId);
        }

        public void SavePriceTier(int productId, int productUnitId, decimal minQuantity, decimal price)
        {
            Execute("INSERT INTO ProductPriceTier(ProductId,ProductUnitId,MinQuantity,Price) VALUES(" +
                productId + "," + productUnitId + "," + N(minQuantity) + "," + N(price) + ")");
        }

        public void SavePriceTier(PriceTierOption tier)
        {
            if (tier.Id > 0)
            {
                Execute("UPDATE ProductPriceTier SET ProductUnitId=" + tier.ProductUnitId +
                    ",MinQuantity=" + N(tier.MinQuantity) + ",Price=" + N(tier.Price) +
                    " WHERE Id=" + tier.Id + " AND ProductId=" + tier.ProductId);
                return;
            }
            Execute("INSERT INTO ProductPriceTier(ProductId,ProductUnitId,MinQuantity,Price) VALUES(" +
                tier.ProductId + "," + tier.ProductUnitId + "," + N(tier.MinQuantity) + "," + N(tier.Price) + ")");
        }

        public void DeletePriceTier(int productId, int tierId)
        {
            Execute("DELETE FROM ProductPriceTier WHERE Id=" + tierId + " AND ProductId=" + productId);
        }

        public List<PriceTierOption> GetAllTiers(int productId)
        {
            return db.ExecuteReader<List<PriceTierOption>>(
                "SELECT t.Id,t.ProductUnitId,t.MinQuantity,t.Price,t.CurrencyId,COALESCE(u.Name,'(semua satuan)'),COALESCE(u.Symbol,'') " +
                "FROM ProductPriceTier t LEFT JOIN ProductUnit pu ON pu.Id=t.ProductUnitId LEFT JOIN Unit u ON u.Id=pu.UnitId " +
                "WHERE t.ProductId=" + productId + " ORDER BY t.ProductUnitId,t.MinQuantity", null,
                delegate(IDataReader r)
                {
                    var list = new List<PriceTierOption>();
                    while (r.Read())
                        list.Add(new PriceTierOption
                        {
                            Id = Convert.ToInt32(r.GetValue(0), CultureInfo.InvariantCulture),
                            ProductId = productId,
                            ProductUnitId = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1), CultureInfo.InvariantCulture),
                            MinQuantity = Convert.ToDecimal(r.GetValue(2), CultureInfo.InvariantCulture),
                            Price = Convert.ToDecimal(r.GetValue(3), CultureInfo.InvariantCulture),
                            CurrencyId = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4), CultureInfo.InvariantCulture),
                            UnitName = Convert.ToString(r.GetValue(5), CultureInfo.InvariantCulture),
                            UnitSymbol = Convert.ToString(r.GetValue(6), CultureInfo.InvariantCulture)
                        });
                    return list;
                });
        }

        private static int Bit(bool value) { return value ? 1 : 0; }
        private static string Text(string value) { return (value ?? "").Replace("'", "''"); }
    }
}
