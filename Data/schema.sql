-- =============================================
-- AI Destekli Dedektiflik RPG - Veritabanı Şeması (Point & Click + SQLite)
-- =============================================

-- Varsa tabloları sırayla sil
DROP TABLE IF EXISTS DialogLogs;
DROP TABLE IF EXISTS Clues;
DROP TABLE IF EXISTS Dialogues;
DROP TABLE IF EXISTS SceneObjects;
DROP TABLE IF EXISTS NPCs;

-- =============================================
-- NPCs Tablosu (Binalarla Eşleşir)
-- =============================================
CREATE TABLE NPCs (
    NPCId       INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    BuildingName TEXT NOT NULL,
    Role        TEXT NOT NULL,
    TrustLevel  INTEGER NOT NULL DEFAULT 50,
    FearLevel   INTEGER NOT NULL DEFAULT 30,
    IsGuilty    INTEGER NOT NULL DEFAULT 0,
    SecretInfo  TEXT NOT NULL DEFAULT '',
    IsActive    INTEGER NOT NULL DEFAULT 1 -- 0 for Karakol, Terzi
);

-- =============================================
-- SceneObjects Tablosu (Point & Click Nesneleri)
-- =============================================
CREATE TABLE SceneObjects (
    ObjectId    INTEGER PRIMARY KEY AUTOINCREMENT,
    NPCId       INTEGER NOT NULL,
    ObjectName  TEXT NOT NULL,
    Description TEXT NOT NULL,
    IsDiscovered INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY(NPCId) REFERENCES NPCs(NPCId) ON DELETE CASCADE
);

-- =============================================
-- Dialogues Tablosu (Hardcoded Soru-Cevap Ağacı)
-- =============================================
CREATE TABLE Dialogues (
    DialogueId  INTEGER PRIMARY KEY AUTOINCREMENT,
    NPCId       INTEGER NOT NULL,
    ParentId    INTEGER NULL, -- Hangi soruya bağlı? (NULL ise ana soru)
    PlayerText  TEXT NOT NULL,
    NPCText     TEXT NOT NULL,
    TrustChange INTEGER NOT NULL DEFAULT 0,
    FearChange  INTEGER NOT NULL DEFAULT 0,
    RequiredObjectId INTEGER NULL, -- Sadece çantamızda bu nesne varsa görünsün
    FOREIGN KEY(NPCId) REFERENCES NPCs(NPCId) ON DELETE CASCADE
);

-- =============================================
-- Varsayılan Veriler (Seed Data)
-- =============================================
INSERT INTO NPCs (Name, BuildingName, Role, TrustLevel, FearLevel, IsGuilty, SecretInfo, IsActive)
VALUES
    ('Kasap Hasan', 'Kasap', 'Kasabadaki eski kasap.', 50, 20, 0, 'Cinayet gecesi dükkânında gizlice birine et sattı.', 1),
    ('Eczacı Selma', 'Eczane', 'Eczane sahibi.', 50, 45, 0, 'Kurbanın kullandığı ilacın yan etkilerini biliyordu.', 1),
    ('Muhtar Kemal', 'Muhtarlık', 'Kasabanın muhtarı.', 50, 60, 1, 'Kurbanla arazi anlaşmazlığı vardı.', 1),
    ('Karakol', 'Karakol', 'Kapalı', 0, 0, 0, '', 0),
    ('Terzi', 'Terzi', 'Kapalı', 0, 0, 0, '', 0);

-- Eczane Nesneleri
INSERT INTO SceneObjects (NPCId, ObjectName, Description) VALUES
    (2, 'Boş İlaç Şişesi', 'Üzerinde kurbanın adının silindiği eski bir ilaç şişesi.'),
    (2, 'Reçete Defteri', 'Kurbanın adının karalandığı son sayfa dikkat çekiyor.'),
    (2, 'Zehirli Sarmaşık', 'Arka odada yetiştirilen, felce sebep olabilecek nadir bir bitki.');

-- Kasap Nesneleri
INSERT INTO SceneObjects (NPCId, ObjectName, Description) VALUES
    (1, 'Kanlı Satır', 'Tezgaha sertçe saplanmış, üzerinde taze lekeler olan paslı bir satır.'),
    (1, 'Kara Kaplı Defter', 'Veresiye listesinde kurbanın isminin üzeri kırmızı kalemle çizilmiş.'),
    (1, 'Yırtık Önlük', 'Kavga izleri taşıyan, yakası kopmuş bir kasap önlüğü.');

-- Muhtar Nesneleri
INSERT INTO SceneObjects (NPCId, ObjectName, Description) VALUES
    (3, 'Tehdit Mektubu', 'Çekmecede gizlenmiş, kurbana yazılmış yarım bir tehdit mektubu taslağı.'),
    (3, 'Kırık Gözlük', 'Kurbana ait olduğu bilinen ama muhtarın odasında bulunan camı kırık gözlük.'),
    (3, 'Gizli Kasa', 'Şifresi kurbanın ölüm tarihiyle aynı olan yarı açık bir çelik kasa.');

-- Diyalog Ağacı Örnekleri (Eczacı Selma)
-- Ana Sorular
INSERT INTO Dialogues (DialogueId, NPCId, ParentId, PlayerText, NPCText, TrustChange, FearChange, RequiredObjectId) VALUES
    (1, 2, NULL, 'Cinayet gecesi eczaneniz açık mıydı?', 'Gece yarısına kadar açıktı, sonra eve gittim. Neden soruyorsunuz?', -5, 10, NULL),
    (2, 2, NULL, 'Kurbanla ilişkiniz nasıldı?', 'Sadece müşterimdi, düzenli ilaç alırdı.', 5, 0, NULL),
    (3, 2, NULL, '(Boş İlaç Şişesi) Bu şişe sizin eczanenize ait. Neden kurbanın evindeydi?', 'Ben.. Ben ona sadece ağrı kesici verdim!', -15, 25, 1);

-- Alt Sorular (1. soruya bağlı)
INSERT INTO Dialogues (DialogueId, NPCId, ParentId, PlayerText, NPCText, TrustChange, FearChange, RequiredObjectId) VALUES
    (4, 2, 1, 'Gece yarısına kadar kimi bekliyordunuz?', 'Kimseyi beklemiyordum, envanter sayımı yapıyordum.', -10, 15, NULL);

