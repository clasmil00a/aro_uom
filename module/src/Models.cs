using System;
using System.Globalization;
using Aronium.Pos.Core.Models;

namespace Aronium.Pos.MultiUom
{
    internal sealed class ProductUnitOption
    {
        public int ProductUnitId { get; set; }
        public int ProductId { get; set; }
        public int UnitId { get; set; }
        public string UnitName { get; set; }
        public string UnitSymbol { get; set; }
        public decimal ConversionFactor { get; set; }
        public decimal Price { get; set; }
        public bool IsBaseUnit { get; set; }
        public bool IsDefault { get; set; }
        public bool IsEnabled { get; set; }
        public string OriginalUnitName { get; set; }
    }

    internal sealed class UnitDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public bool IsEnabled { get; set; }
        public string DisplayName
        {
            get { return string.IsNullOrEmpty(Symbol) ? Name : Name + " (" + Symbol + ")"; }
        }
    }

    internal sealed class PriceTierOption
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public decimal MinQuantity { get; set; }
        public decimal Price { get; set; }
        public int CurrencyId { get; set; }
        public string UnitName { get; set; }
        public string UnitSymbol { get; set; }
    }

    internal sealed class MultiUomSelection
    {
        public int ProductUnitId { get; set; }
        public int UnitId { get; set; }
        public decimal ConversionFactor { get; set; }
        public decimal SelectedQuantity { get; set; }
        public decimal SelectedUnitPrice { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal BaseUnitPrice { get; set; }
        public int? AppliedTierId { get; set; }
        public decimal? AppliedTierMinQuantity { get; set; }
        public decimal? AppliedTierPrice { get; set; }

        public Package ToPackage()
        {
            return new Package
            {
                Id = ProductUnitId,
                Name = Encode(),
                Quantity = ConversionFactor
            };
        }

        public static MultiUomSelection FromPackage(Package package)
        {
            if (package == null || string.IsNullOrEmpty(package.Name))
                return null;
            return Decode(package.Name);
        }

        public string Encode()
        {
            return string.Join("|", new string[]
            {
                "MUOM1",
                ProductUnitId.ToString(CultureInfo.InvariantCulture),
                UnitId.ToString(CultureInfo.InvariantCulture),
                ConversionFactor.ToString("G29", CultureInfo.InvariantCulture),
                SelectedQuantity.ToString("G29", CultureInfo.InvariantCulture),
                SelectedUnitPrice.ToString("G29", CultureInfo.InvariantCulture),
                BaseQuantity.ToString("G29", CultureInfo.InvariantCulture),
                BaseUnitPrice.ToString("G29", CultureInfo.InvariantCulture),
                (AppliedTierId.HasValue ? AppliedTierId.Value : 0).ToString(CultureInfo.InvariantCulture),
                (AppliedTierMinQuantity.HasValue ? AppliedTierMinQuantity.Value : 0m).ToString("G29", CultureInfo.InvariantCulture),
                (AppliedTierPrice.HasValue ? AppliedTierPrice.Value : 0m).ToString("G29", CultureInfo.InvariantCulture)
            });
        }

        public static MultiUomSelection Decode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            string[] p = value.Split('|');
            if (p.Length != 11 || p[0] != "MUOM1")
                return null;
            try
            {
                int tierId = int.Parse(p[8], CultureInfo.InvariantCulture);
                decimal tierMin = decimal.Parse(p[9], CultureInfo.InvariantCulture);
                decimal tierPrice = decimal.Parse(p[10], CultureInfo.InvariantCulture);
                return new MultiUomSelection
                {
                    ProductUnitId = int.Parse(p[1], CultureInfo.InvariantCulture),
                    UnitId = int.Parse(p[2], CultureInfo.InvariantCulture),
                    ConversionFactor = decimal.Parse(p[3], CultureInfo.InvariantCulture),
                    SelectedQuantity = decimal.Parse(p[4], CultureInfo.InvariantCulture),
                    SelectedUnitPrice = decimal.Parse(p[5], CultureInfo.InvariantCulture),
                    BaseQuantity = decimal.Parse(p[6], CultureInfo.InvariantCulture),
                    BaseUnitPrice = decimal.Parse(p[7], CultureInfo.InvariantCulture),
                    AppliedTierId = tierId == 0 ? (int?)null : tierId,
                    AppliedTierMinQuantity = tierId == 0 ? (decimal?)null : tierMin,
                    AppliedTierPrice = tierId == 0 ? (decimal?)null : tierPrice
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
