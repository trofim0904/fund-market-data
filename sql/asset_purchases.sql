CREATE TABLE "AssetPurchases" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AssetPurchases" PRIMARY KEY,
    "Ticker" TEXT NULL,
    "Date" TEXT NOT NULL,
    "Price" TEXT NOT NULL,
    "Qty" TEXT NOT NULL
);