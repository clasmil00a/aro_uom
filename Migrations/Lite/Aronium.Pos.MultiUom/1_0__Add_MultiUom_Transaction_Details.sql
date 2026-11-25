CREATE TABLE IF NOT EXISTS PosOrderItemMultiUom (
    PosOrderItemId INTEGER PRIMARY KEY
        REFERENCES PosOrderItem (Id) ON DELETE CASCADE,
    ProductId INTEGER NOT NULL,
    ProductUnitId INTEGER NOT NULL,
    UnitId INTEGER NOT NULL,
    ConversionFactor NUMERIC NOT NULL,
    SelectedQuantity NUMERIC NOT NULL,
    SelectedUnitPrice NUMERIC NOT NULL,
    BaseQuantity NUMERIC NOT NULL,
    BaseUnitPrice NUMERIC NOT NULL,
    AppliedTierId INTEGER,
    AppliedTierMinQuantity NUMERIC,
    AppliedTierPrice NUMERIC,
    DateCreated DATETIME NOT NULL DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS DocumentItemMultiUom (
    DocumentItemId INTEGER PRIMARY KEY
        REFERENCES DocumentItem (Id) ON DELETE CASCADE,
    ProductId INTEGER NOT NULL,
    ProductUnitId INTEGER NOT NULL,
    UnitId INTEGER NOT NULL,
    ConversionFactor NUMERIC NOT NULL,
    SelectedQuantity NUMERIC NOT NULL,
    SelectedUnitPrice NUMERIC NOT NULL,
    BaseQuantity NUMERIC NOT NULL,
    BaseUnitPrice NUMERIC NOT NULL,
    AppliedTierId INTEGER,
    AppliedTierMinQuantity NUMERIC,
    AppliedTierPrice NUMERIC,
    DateCreated DATETIME NOT NULL DEFAULT (datetime('now','localtime'))
);
