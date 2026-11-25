using System;
using System.Collections.Generic;
using Aronium.Pos.Facade.Services;

namespace Aronium.Pos.MultiUom
{
    internal sealed partial class MultiUomRepository
    {
        private readonly IGenericDatabaseService db;

        public MultiUomRepository(IGenericDatabaseService db)
        {
            if (db == null) throw new ArgumentNullException("db");
            this.db = db;
        }

        public void EnsureSchema()
        {
            Execute("CREATE TABLE IF NOT EXISTS Unit (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Symbol TEXT, IsEnabled INTEGER NOT NULL DEFAULT 1)");
            Execute("CREATE TABLE IF NOT EXISTS ProductUnit (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProductId INTEGER NOT NULL REFERENCES Product(Id) ON DELETE CASCADE, UnitId INTEGER NOT NULL REFERENCES Unit(Id) ON DELETE CASCADE, ConversionFactor NUMERIC NOT NULL DEFAULT 1, Price NUMERIC NOT NULL DEFAULT 0, IsBaseUnit INTEGER NOT NULL DEFAULT 0, IsDefault INTEGER NOT NULL DEFAULT 0, IsEnabled INTEGER NOT NULL DEFAULT 1, UNIQUE(ProductId,UnitId))");
            Execute("CREATE INDEX IF NOT EXISTS IX_ProductUnit_Product ON ProductUnit(ProductId)");
            Execute("CREATE TABLE IF NOT EXISTS ProductPriceTier (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProductId INTEGER NOT NULL REFERENCES Product(Id) ON DELETE CASCADE, ProductUnitId INTEGER REFERENCES ProductUnit(Id) ON DELETE CASCADE, MinQuantity NUMERIC NOT NULL DEFAULT 0, Price NUMERIC NOT NULL, CurrencyId INTEGER REFERENCES Currency(Id))");
            Execute("CREATE INDEX IF NOT EXISTS IX_ProductPriceTier_Product ON ProductPriceTier(ProductId)");
            Execute("CREATE VIEW IF NOT EXISTS ProductUnitPriceView AS SELECT P.Id AS ProductId,P.Name AS ProductName,PU.Id AS ProductUnitId,U.Name AS UnitName,U.Symbol AS UnitSymbol,PU.ConversionFactor AS ConversionFactor,CASE WHEN PU.Price>0 THEN PU.Price ELSE P.Price END AS Price,PU.IsBaseUnit AS IsBaseUnit,PU.IsDefault AS IsDefault FROM ProductUnit PU INNER JOIN Product P ON P.Id=PU.ProductId INNER JOIN Unit U ON U.Id=PU.UnitId");
            Execute(CreateDetailTable("PosOrderItemMultiUom", "PosOrderItemId", "PosOrderItem"));
            Execute(CreateDetailTable("DocumentItemMultiUom", "DocumentItemId", "DocumentItem"));
        }

        private static string CreateDetailTable(string table, string idColumn, string parentTable)
        {
            return "CREATE TABLE IF NOT EXISTS " + table + " (" +
                idColumn + " INTEGER PRIMARY KEY REFERENCES " + parentTable + "(Id) ON DELETE CASCADE," +
                "ProductId INTEGER NOT NULL, ProductUnitId INTEGER NOT NULL, UnitId INTEGER NOT NULL," +
                "ConversionFactor NUMERIC NOT NULL, SelectedQuantity NUMERIC NOT NULL," +
                "SelectedUnitPrice NUMERIC NOT NULL, BaseQuantity NUMERIC NOT NULL," +
                "BaseUnitPrice NUMERIC NOT NULL, AppliedTierId INTEGER," +
                "AppliedTierMinQuantity NUMERIC, AppliedTierPrice NUMERIC," +
                "DateCreated DATETIME NOT NULL DEFAULT (datetime('now','localtime')))";
        }

        private int Execute(string sql)
        {
            return db.Execute(sql);
        }
    }
}
