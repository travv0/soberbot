CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

CREATE TABLE "Sobrieties" (
    "ID" INTEGER NOT NULL CONSTRAINT "PK_Sobrieties" PRIMARY KEY AUTOINCREMENT,
    "UserID" INTEGER NOT NULL,
    "UserName" TEXT NULL,
    "ServerID" INTEGER NOT NULL,
    "SobrietyDate" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20190928140448_ActiveDate', '2.2.6-servicing-10079');

