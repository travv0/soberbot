﻿CREATE TABLE "Bans" (
    "ID" INTEGER NOT NULL CONSTRAINT "PK_Bans" PRIMARY KEY AUTOINCREMENT,
    "ServerID" INTEGER NOT NULL,
    "UserID" INTEGER NOT NULL,
    "Message" TEXT NULL
);
CREATE TABLE "Config" (
    "ID" INTEGER NOT NULL CONSTRAINT "PK_Config" PRIMARY KEY AUTOINCREMENT,
    "ServerID" INTEGER NOT NULL,
    "PruneDays" INTEGER NOT NULL
, "MilestoneChannelID" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE "Milestones" (
    "ID" INTEGER NOT NULL CONSTRAINT "PK_Milestones" PRIMARY KEY AUTOINCREMENT,
    "Days" INTEGER NOT NULL,
    "Name" TEXT NULL
);
CREATE TABLE "Sobrieties" (
    "ID" INTEGER NOT NULL CONSTRAINT "PK_Sobrieties" PRIMARY KEY AUTOINCREMENT,
    "UserID" INTEGER NOT NULL,
    "UserName" TEXT NULL,
    "ServerID" INTEGER NOT NULL,
    "SobrietyDate" TEXT NOT NULL,
    "ActiveDate" TEXT NOT NULL
, "LastMilestoneDays" INTEGER NOT NULL DEFAULT 0, "MilestonesEnabled" INTEGER NOT NULL DEFAULT 1);