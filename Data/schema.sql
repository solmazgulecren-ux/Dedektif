-- =============================================
-- AI Destekli Dedektiflik RPG - Veritabanı Şeması
-- SQL Server için CREATE TABLE sorguları
-- =============================================

-- Varsa tabloları sırayla sil (bağımlılık sırasına dikkat)
IF OBJECT_ID('dbo.DialogLogs', 'U') IS NOT NULL DROP TABLE dbo.DialogLogs;
IF OBJECT_ID('dbo.Clues', 'U') IS NOT NULL DROP TABLE dbo.Clues;
IF OBJECT_ID('dbo.NPCs', 'U') IS NOT NULL DROP TABLE dbo.NPCs;
GO

-- =============================================
-- NPCs Tablosu
-- Kasabadaki şüpheli karakterler
-- =============================================
CREATE TABLE dbo.NPCs (
    NPCId       INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100)   NOT NULL,
    Role        NVARCHAR(200)   NOT NULL,
    TrustLevel  INT             NOT NULL DEFAULT 50,    -- 0-100 arası
    FearLevel   INT             NOT NULL DEFAULT 30,    -- 0-100 arası
    IsGuilty    BIT             NOT NULL DEFAULT 0,
    SecretInfo  NVARCHAR(500)   NOT NULL DEFAULT ''
);
GO

-- =============================================
-- Clues Tablosu
-- Oyuncu tarafından keşfedilen ipuçları
-- =============================================
CREATE TABLE dbo.Clues (
    ClueId          INT IDENTITY(1,1) PRIMARY KEY,
    Title           NVARCHAR(200)   NOT NULL,
    Description     NVARCHAR(1000)  NOT NULL,
    RelatedNPCId    INT             NULL,
    Status          NVARCHAR(50)    NOT NULL DEFAULT 'Pending',     -- 'Pending', 'KeptInBag', 'IgnoredAtScene'
    Location        NVARCHAR(200)   NOT NULL DEFAULT N'Olay Yeri',
    CONSTRAINT FK_Clues_NPCs FOREIGN KEY (RelatedNPCId)
        REFERENCES dbo.NPCs(NPCId)
        ON DELETE SET NULL
);
GO

-- =============================================
-- DialogLogs Tablosu
-- Oyuncu-NPC diyalog kayıtları
-- =============================================
CREATE TABLE dbo.DialogLogs (
    LogId           INT IDENTITY(1,1) PRIMARY KEY,
    NPCId           INT             NOT NULL,
    PlayerQuestion  NVARCHAR(1000)  NOT NULL,
    NPCResponse     NVARCHAR(2000)  NOT NULL,
    DetectedEmotion NVARCHAR(50)    NOT NULL DEFAULT '',
    TrustChange     INT             NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_DialogLogs_NPCs FOREIGN KEY (NPCId)
        REFERENCES dbo.NPCs(NPCId)
        ON DELETE CASCADE
);
GO

-- =============================================
-- Varsayılan Veriler (Seed Data)
-- 3 şüpheli NPC ve ilgili ipuçları
-- =============================================
INSERT INTO dbo.NPCs (Name, Role, TrustLevel, FearLevel, IsGuilty, SecretInfo)
VALUES
    (N'Kasap Hasan',   N'Kasabadaki eski kasap, herkesin tanıdığı bir figür.',            50, 20, 0, N'Cinayet gecesi dükkânında gizlice birine et sattı.'),
    (N'Eczacı Selma',  N'Eczane sahibi, ilaç ve zehir konusunda uzman.',                  50, 45, 0, N'Kurbanın kullandığı ilacın yan etkilerini biliyordu ama gizledi.'),
    (N'Muhtar Kemal',  N'Kasabanın muhtarı, herkesin sırrını bilen bir politikacı.',       50, 60, 1, N'Kurbanla arazi anlaşmazlığı vardı ve onu tehdit etmişti.');

INSERT INTO dbo.Clues (Title, Description, RelatedNPCId)
VALUES
    (N'Kanlı Bıçak',           N'Olay yerinde bulunan paslanmış bir kasap bıçağı.',                          1),
    (N'Boş İlaç Şişesi',       N'Kurbanın evinde bulunan etiketsiz ilaç şişesi.',                            2),
    (N'Tehdit Mektubu',        N'Kurbanın çekmecesinden çıkan, muhtarın el yazısına benzeyen mektup.',        3),
    (N'Tanık İfadesi',         N'Bir komşu, cinayet gecesi muhtarın evinden bağırışlar duyduğunu söyledi.',   3),
    (N'Güvenlik Kamerası',     N'Eczanenin önündeki kamera, gece yarısı şüpheli bir silüet yakalamış.',       2);
GO
