using System;
using System.Collections.Generic;
using System.Linq;

namespace Aronium.Pos.MultiUom
{
    internal static class MultiUomPricing
    {
        public static MultiUomSelection Resolve(ProductUnitOption unit, decimal quantity, decimal productPrice, List<PriceTierOption> tiers)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            PriceTierOption tier = FindTier(tiers, unit.ProductUnitId, quantity);
            decimal unitPrice = unit.Price;
            if (tier != null)
                unitPrice = tier.Price;
            else if (unitPrice <= 0)
                unitPrice = productPrice;

            decimal factor = unit.ConversionFactor;
            if (factor <= 0) factor = 1m;
            return new MultiUomSelection
            {
                ProductUnitId = unit.ProductUnitId,
                UnitId = unit.UnitId,
                ConversionFactor = factor,
                SelectedQuantity = quantity,
                SelectedUnitPrice = unitPrice,
                BaseQuantity = quantity * factor,
                BaseUnitPrice = unitPrice / factor,
                AppliedTierId = tier == null ? (int?)null : tier.Id,
                AppliedTierMinQuantity = tier == null ? (decimal?)null : tier.MinQuantity,
                AppliedTierPrice = tier == null ? (decimal?)null : tier.Price
            };
        }

        private static PriceTierOption FindTier(List<PriceTierOption> tiers, int productUnitId, decimal quantity)
        {
            if (tiers == null) return null;
            PriceTierOption specific = tiers
                .Where(x => x.ProductUnitId == productUnitId && x.MinQuantity <= quantity)
                .OrderByDescending(x => x.MinQuantity)
                .FirstOrDefault();
            if (specific != null) return specific;
            return tiers
                .Where(x => x.ProductUnitId == 0 && x.MinQuantity <= quantity)
                .OrderByDescending(x => x.MinQuantity)
                .FirstOrDefault();
        }
    }
}
