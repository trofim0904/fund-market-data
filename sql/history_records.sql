CREATE TABLE "HistoryRecords" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_HistoryRecords" PRIMARY KEY,
    "Ticker" TEXT NULL,
    "Date" TEXT NOT NULL,
    "CurrentPrice" TEXT NOT NULL,
    "FuturePrice" TEXT NOT NULL,
    "MarketCap" TEXT NOT NULL,
    "Change" TEXT NOT NULL
);