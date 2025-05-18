CREATE TABLE "AssetSales" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AssetSales" PRIMARY KEY,
    "Ticker" TEXT NULL,
    "Date" TEXT NOT NULL,
    "Price" TEXT NOT NULL,
    "Qty" TEXT NOT NULL
);