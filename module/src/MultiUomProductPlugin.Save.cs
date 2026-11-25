using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Aronium.Pos.MultiUom
{
    public sealed partial class MultiUomProductPlugin
    {
        private void OnSaveAll(object sender, System.Windows.RoutedEventArgs e)
        {
            logger.Info("Multi-UOM native editor save button invoked.");
            bool result = SaveAll(true);
            logger.Info("Multi-UOM native editor save result: " + result);
        }

        private bool SaveAll(bool showSuccessMessage)
        {
            MultiUomRepository repository = MultiUomRuntime.Repository;
            if (!Ready(repository)) return false;
            if (!CommitTableEdits()) { Show("Selesaikan edit yang belum valid sebelum menyimpan."); return false; }

            int productId = Convert.ToInt32(product.Id, CultureInfo.InvariantCulture);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var unitIds = new HashSet<int>();
            int defaults = 0;
            int baseUnits = 0;
            logger.Info("Multi-UOM native editor rows: units=" + unitRows.Count + ", tiers=" + tierRows.Count);
            foreach (ProductUnitOption row in unitRows)
            {
                row.UnitName = (row.UnitName ?? "").Trim();
                if (row.UnitName.Length == 0) { Show("Nama satuan tidak boleh kosong."); return false; }
                if (!names.Add(row.UnitName)) { Show("Satuan '" + row.UnitName + "' duplikat."); return false; }
                if (row.ConversionFactor <= 0) { Show("Faktor satuan '" + row.UnitName + "' harus lebih besar dari nol."); return false; }
                if (row.Price < 0) { Show("Harga satuan '" + row.UnitName + "' tidak boleh negatif."); return false; }
                int effectiveUnitId = row.UnitId;
                if (row.ProductUnitId <= 0 || row.UnitName != row.OriginalUnitName)
                {
                    UnitDefinition existingDefinition = FindUnit(row.UnitName);
                    effectiveUnitId = existingDefinition == null ? 0 : existingDefinition.Id;
                }
                if (effectiveUnitId > 0 && !unitIds.Add(effectiveUnitId))
                {
                    Show("Dua baris menunjuk ke satuan yang sama: '" + row.UnitName + "'.");
                    return false;
                }
                if (row.IsDefault) defaults++;
                if (row.ConversionFactor == 1m) baseUnits++;
            }
            logger.Info("Multi-UOM native editor validation: defaults=" + defaults + ", baseUnits=" + baseUnits);
            if (defaults > 1) { Show("Hanya satu satuan yang dapat menjadi Default."); return false; }
            if (unitRows.Count > 0 && baseUnits != 1)
            {
                Show("Tepat satu satuan harus memiliki faktor 1 sebagai satuan dasar.");
                return false;
            }

            try
            {
                foreach (int id in removedTierIds) repository.DeletePriceTier(productId, id);
                foreach (int id in removedUnitIds) repository.DeleteProductUnit(productId, id);

                foreach (ProductUnitOption row in unitRows)
                {
                    row.ProductId = productId;
                    row.IsBaseUnit = row.ConversionFactor == 1m;
                    if (row.ProductUnitId <= 0 || row.UnitName != row.OriginalUnitName)
                    {
                        UnitDefinition definition = FindUnit(row.UnitName);
                        if (definition == null)
                        {
                            repository.AddUnit(row.UnitName, row.UnitName);
                            unitDefinitions = repository.GetUnits();
                            definition = FindUnit(row.UnitName);
                        }
                        if (definition == null) { Show("Satuan '" + row.UnitName + "' tidak dapat disimpan."); return false; }
                        row.UnitId = definition.Id;
                        row.UnitName = definition.Name;
                        row.UnitSymbol = definition.Symbol;
                    }
                    repository.SaveProductUnit(row);
                    row.OriginalUnitName = row.UnitName;
                }

                foreach (PriceTierOption row in tierRows)
                {
                    string name = (row.UnitName ?? "").Trim();
                    ProductUnitOption unit = unitRows.FirstOrDefault(x => string.Equals(x.UnitName, name, StringComparison.OrdinalIgnoreCase));
                    if (unit == null) { Show("Satuan tier '" + name + "' belum tersedia pada tabel satuan produk."); return false; }
                    if (row.MinQuantity < 0 || row.Price < 0) { Show("Min qty dan harga tier tidak boleh negatif."); return false; }
                    row.ProductId = productId;
                    row.ProductUnitId = unit.ProductUnitId;
                    row.UnitName = unit.UnitName;
                    repository.SavePriceTier(row);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Multi-UOM native editor save failed.", ex);
                Show("Gagal menyimpan: " + ex.Message);
                return false;
            }

            RefreshData();
            if (showSuccessMessage) Show("Perubahan Multi-UOM berhasil disimpan.");
            return true;
        }

        private UnitDefinition FindUnit(string name)
        {
            return unitDefinitions.FirstOrDefault(x =>
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Symbol, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
