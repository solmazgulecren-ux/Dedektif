using Dapper;
using DedektiflikRPG.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DedektiflikRPG.Data;

/// <summary>
/// Dapper ile SQL Server veritabanı işlemlerini yöneten repository sınıfı.
/// NPC, ipucu ve diyalog kayıtları üzerinde CRUD işlemleri gerçekleştirir.
/// </summary>
public class DatabaseRepository
{
    private readonly string _connectionString;

    public DatabaseRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // =============================================
    // NPC İşlemleri
    // =============================================

    /// <summary>
    /// Tüm NPC'leri getirir.
    /// </summary>
    public async Task<IEnumerable<NPC>> GetAllNPCsAsync()
    {
        using var db = CreateConnection();
        return await db.QueryAsync<NPC>("SELECT * FROM NPCs ORDER BY NPCId");
    }

    /// <summary>
    /// Belirli bir NPC'yi ID'sine göre getirir.
    /// </summary>
    public async Task<NPC?> GetNPCByIdAsync(int npcId)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<NPC>(
            "SELECT * FROM NPCs WHERE NPCId = @NPCId",
            new { NPCId = npcId });
    }

    /// <summary>
    /// NPC'nin güven seviyesini günceller (0-100 arasında kalmasını sağlar).
    /// </summary>
    public async Task UpdateNPCTrustAsync(int npcId, int trustChange)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(@"
            UPDATE NPCs 
            SET TrustLevel = 
                CASE 
                    WHEN TrustLevel + @TrustChange > 100 THEN 100
                    WHEN TrustLevel + @TrustChange < 0 THEN 0
                    ELSE TrustLevel + @TrustChange
                END
            WHERE NPCId = @NPCId",
            new { NPCId = npcId, TrustChange = trustChange });
    }

    // =============================================
    // İpucu İşlemleri
    // =============================================

    /// <summary>
    /// Tüm ipuçlarını getirir.
    /// </summary>
    public async Task<IEnumerable<Clue>> GetAllCluesAsync()
    {
        using var db = CreateConnection();
        return await db.QueryAsync<Clue>("SELECT * FROM Clues ORDER BY ClueId");
    }

    /// <summary>
    /// Sadece oyuncunun çantasına sakladığı ipuçlarını getirir.
    /// </summary>
    public async Task<IEnumerable<Clue>> GetCluesInBagAsync()
    {
        using var db = CreateConnection();
        return await db.QueryAsync<Clue>("SELECT * FROM Clues WHERE Status = 'KeptInBag' ORDER BY ClueId");
    }

    /// <summary>
    /// İpucunun durumunu günceller ("KeptInBag" veya "IgnoredAtScene").
    /// </summary>
    public async Task UpdateClueStatusAsync(int clueId, string status)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync("UPDATE Clues SET Status = @Status WHERE ClueId = @ClueId", new { ClueId = clueId, Status = status });
    }

    /// <summary>
    /// Belirli bir NPC'ye bağlı ipuçlarını getirir.
    /// </summary>
    public async Task<IEnumerable<Clue>> GetCluesByNPCIdAsync(int npcId)
    {
        using var db = CreateConnection();
        return await db.QueryAsync<Clue>(
            "SELECT * FROM Clues WHERE RelatedNPCId = @NPCId ORDER BY ClueId",
            new { NPCId = npcId });
    }

    // =============================================
    // Diyalog Kayıt İşlemleri
    // =============================================

    /// <summary>
    /// Yeni bir diyalog kaydı ekler.
    /// </summary>
    public async Task AddDialogLogAsync(DialogLog log)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(@"
            INSERT INTO DialogLogs (NPCId, PlayerQuestion, NPCResponse, DetectedEmotion, TrustChange, CreatedAt)
            VALUES (@NPCId, @PlayerQuestion, @NPCResponse, @DetectedEmotion, @TrustChange, @CreatedAt)",
            log);
    }

    /// <summary>
    /// Belirli bir NPC ile yapılan tüm diyalog kayıtlarını getirir.
    /// </summary>
    public async Task<IEnumerable<DialogLog>> GetDialogLogsByNPCIdAsync(int npcId)
    {
        using var db = CreateConnection();
        return await db.QueryAsync<DialogLog>(
            "SELECT * FROM DialogLogs WHERE NPCId = @NPCId ORDER BY CreatedAt DESC",
            new { NPCId = npcId });
    }

    // =============================================
    // Seed Data — Varsayılan Veri Yükleme
    // =============================================

    /// <summary>
    /// Veritabanında veri yoksa 3 varsayılan NPC ve ipuçlarını ekler.
    /// </summary>
    public async Task SeedDataAsync()
    {
        using var db = CreateConnection();

        var npcCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM NPCs");
        if (npcCount >= 5)
            return; // Veri zaten var, seed yapma

        // Tabloları temizle ve ID'leri sıfırla
        await db.ExecuteAsync(@"
            DELETE FROM DialogLogs; 
            DELETE FROM Clues; 
            DELETE FROM NPCs;
            DBCC CHECKIDENT ('NPCs', RESEED, 0);
            DBCC CHECKIDENT ('Clues', RESEED, 0);");

        // NPC'leri ekle
        await db.ExecuteAsync(@"
            INSERT INTO NPCs (Name, Role, TrustLevel, FearLevel, IsGuilty, SecretInfo)
            VALUES
                (N'Kasap Hasan',   N'Kasabadaki eski kasap, herkesin tanıdığı sert bir figür.',            50, 20, 0, N'Cinayet gecesi dükkânında gizlice birine et sattı.'),
                (N'Eczacı Selma',  N'Eczane sahibi, ilaç ve zehir konusunda uzman bir kadın.',             50, 45, 0, N'Kurbanın kullandığı ilacın yan etkilerini biliyordu ama gizledi.'),
                (N'Muhtar Kemal',  N'Kasabanın muhtarı, herkesin sırrını bilen bir politikacı.',           50, 60, 0, N'Kurbanla arazi anlaşmazlığı vardı ve onu tehdit etmişti.'),
                (N'Komiser Güneş', N'Kasabanın kadın polis komiseri, adaletin savunucusu.',                50, 30, 0, N'Olay gecesi bazı delilleri yanlışlıkla yok ettiğini gizliyor.'),
                (N'Terzi Yahya',   N'Kasabanın yaşlı terzisi, kurbana son kıyafeti diken kişi.',           50, 35, 0, N'Kurbanın ceketine gizli bir cep dikmişti, içinde ne olduğunu kimseye söylemedi.')");

        // İpuçlarını ekle
        await db.ExecuteAsync(@"
            INSERT INTO Clues (Title, Description, RelatedNPCId)
            VALUES
                (N'Kanlı Bıçak',           N'Olay yerinde bulunan paslanmış bir kasap bıçağı.',                          1),
                (N'Boş İlaç Şişesi',       N'Kurbanın evinde bulunan etiketsiz ilaç şişesi.',                            2),
                (N'Tehdit Mektubu',        N'Kurbanın çekmecesinden çıkan, muhtarın el yazısına benzeyen mektup.',        3),
                (N'Kopan Düğme',           N'Olay yerinde bulunan, polis üniformasına ait kopmuş pirinç düğme.',          4),
                (N'Kopuk İplik',           N'Kurbanın boğazında bulunan son derece sağlam bir terzi ipliği.',             5)");
    }

    // =============================================
    // Veritabanı Bağlantı Kontrolü
    // =============================================

    /// <summary>
    /// Veritabanı bağlantısını test eder.
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var db = CreateConnection();
            await ((SqlConnection)db).OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tabloların mevcut olup olmadığını kontrol eder.
    /// </summary>
    public async Task<bool> TablesExistAsync()
    {
        try
        {
            using var db = CreateConnection();
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('NPCs', 'Clues', 'DialogLogs')");
            return count == 3;
        }
        catch
        {
            return false;
        }
    }

    // =============================================
    // Oyun Oturumu İşlemleri (GameSessions)
    // =============================================

    /// <summary>
    /// Yeni bir oyun oturumu oluşturur ve ID'sini döner.
    /// </summary>
    public async Task<int> CreateGameSessionAsync(int guiltyNpcId)
    {
        using var db = CreateConnection();
        var sessionId = await db.ExecuteScalarAsync<int>(@"
            INSERT INTO GameSessions (GuiltyNPCId, StartedAt)
            VALUES (@GuiltyNPCId, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new { GuiltyNPCId = guiltyNpcId });
        return sessionId;
    }

    /// <summary>
    /// Oyun oturumunu sonlandırır.
    /// </summary>
    public async Task EndGameSessionAsync(int sessionId, string result, int? accusedNpcId, int totalQuestions, int cluesCollected)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(@"
            UPDATE GameSessions 
            SET EndedAt = GETDATE(), Result = @Result, AccusedNPCId = @AccusedNPCId,
                TotalQuestions = @TotalQuestions, CluesCollected = @CluesCollected
            WHERE SessionId = @SessionId",
            new { SessionId = sessionId, Result = result, AccusedNPCId = accusedNpcId, TotalQuestions = totalQuestions, CluesCollected = cluesCollected });
    }

    // =============================================
    // Oyuncu Aksiyon Kayıt İşlemleri (PlayerActions)
    // =============================================

    /// <summary>
    /// Oyuncunun bir aksiyonunu kaydeder.
    /// </summary>
    public async Task LogPlayerActionAsync(int sessionId, string actionType, int? targetId, string? details)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(@"
            INSERT INTO PlayerActions (SessionId, ActionType, TargetId, Details, CreatedAt)
            VALUES (@SessionId, @ActionType, @TargetId, @Details, GETDATE())",
            new { SessionId = sessionId, ActionType = actionType, TargetId = targetId, Details = details });
    }

    // =============================================
    // Oyun Durumu Kayıt/Yükleme (GameStates)
    // =============================================

    /// <summary>
    /// Oyun durumunu JSON olarak kaydeder.
    /// </summary>
    public async Task SaveGameStateAsync(int sessionId, string stateDataJson)
    {
        using var db = CreateConnection();
        // Mevcut state varsa güncelle, yoksa ekle
        var exists = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM GameStates WHERE SessionId = @SessionId",
            new { SessionId = sessionId });
        
        if (exists > 0)
        {
            await db.ExecuteAsync(@"
                UPDATE GameStates SET StateData = @StateData, SavedAt = GETDATE()
                WHERE SessionId = @SessionId",
                new { SessionId = sessionId, StateData = stateDataJson });
        }
        else
        {
            await db.ExecuteAsync(@"
                INSERT INTO GameStates (SessionId, StateData, SavedAt)
                VALUES (@SessionId, @StateData, GETDATE())",
                new { SessionId = sessionId, StateData = stateDataJson });
        }
    }

    /// <summary>
    /// Kayıtlı oyun durumunu yükler.
    /// </summary>
    public async Task<string?> LoadGameStateAsync(int sessionId)
    {
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<string?>(
            "SELECT TOP 1 StateData FROM GameStates WHERE SessionId = @SessionId ORDER BY SavedAt DESC",
            new { SessionId = sessionId });
    }

    // =============================================
    // Yardımcı Dedektif Mesaj İşlemleri (HelperMessages)
    // =============================================

    /// <summary>
    /// Belirli bağlama göre Çetin'in mesajlarını getirir.
    /// </summary>
    public async Task<IEnumerable<HelperMessage>> GetHelperMessagesAsync(string context, string? buildingName = null)
    {
        using var db = CreateConnection();
        if (!string.IsNullOrEmpty(buildingName))
        {
            return await db.QueryAsync<HelperMessage>(@"
                SELECT * FROM HelperMessages 
                WHERE Context = @Context AND (BuildingName = @BuildingName OR BuildingName IS NULL)
                ORDER BY Priority DESC",
                new { Context = context, BuildingName = buildingName });
        }
        return await db.QueryAsync<HelperMessage>(@"
            SELECT * FROM HelperMessages WHERE Context = @Context ORDER BY Priority DESC",
            new { Context = context });
    }

    /// <summary>
    /// Çetin'in delil analiz mesajını döner (çantadaki delil ID'lerine göre).
    /// </summary>
    public async Task<string> AnalyzeCluesForHelperAsync(List<int> clueIds, int guiltyNpcId)
    {
        if (clueIds == null || clueIds.Count == 0)
            return "Amirims, çantanızda henüz delil yok! Binalara girip delilleri toplamalısınız.";

        using var db = CreateConnection();
        var clues = await db.QueryAsync<Clue>(
            "SELECT * FROM Clues WHERE ClueId IN @Ids",
            new { Ids = clueIds });

        var clueList = clues.ToList();
        var relatedToGuilty = clueList.Where(c => c.RelatedNPCId == guiltyNpcId).ToList();

        if (relatedToGuilty.Count >= 2)
            return $"Amirims, çantanızdaki {clueList.Count} delilden bazıları aynı şüpheliyi işaret ediyor gibi görünüyor. Bu izleri dikkatle takip edin!";
        else if (clueList.Count >= 3)
            return $"Amirims, {clueList.Count} delil toplamışsınız. Farklı binalardaki delilleri karşılaştırın, bazıları birbiriyle bağlantılı olabilir!";
        else
            return $"Amirims, henüz {clueList.Count} delil topladınız. Daha fazla bina incelemenizi öneririm. Her binada 3 delil var!";
    }

    // =============================================
    // Gelişmiş Diyalog Kayıt İşlemleri
    // =============================================

    /// <summary>
    /// NPC konuşma kaydını veritabanına yazar (difficulty ve category ile).
    /// </summary>
    public async Task LogDialogWithCategoryAsync(int npcId, string playerQuestion, string npcResponse, int difficulty, string category)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(@"
            INSERT INTO DialogLogs (NPCId, PlayerQuestion, NPCResponse, Difficulty, Category, CreatedAt)
            VALUES (@NPCId, @PlayerQuestion, @NPCResponse, @Difficulty, @Category, GETDATE())",
            new { NPCId = npcId, PlayerQuestion = playerQuestion, NPCResponse = npcResponse, Difficulty = difficulty, Category = category });
    }

    // =============================================
    // Yardımcı Tablo Oluşturma (Migrate)
    // =============================================

    /// <summary>
    /// Eksik tabloları oluşturur (GameSessions, PlayerActions, GameStates, HelperMessages).
    /// </summary>
    public async Task EnsureHelperTablesAsync()
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GameSessions')
            CREATE TABLE GameSessions (
                SessionId INT IDENTITY(1,1) PRIMARY KEY,
                GuiltyNPCId INT NOT NULL,
                StartedAt DATETIME NOT NULL DEFAULT GETDATE(),
                EndedAt DATETIME NULL,
                Result NVARCHAR(50) NULL,
                AccusedNPCId INT NULL,
                TotalQuestions INT DEFAULT 0,
                CluesCollected INT DEFAULT 0
            );

            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PlayerActions')
            CREATE TABLE PlayerActions (
                ActionId INT IDENTITY(1,1) PRIMARY KEY,
                SessionId INT NOT NULL,
                ActionType NVARCHAR(100) NOT NULL,
                TargetId INT NULL,
                Details NVARCHAR(MAX) NULL,
                CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
            );

            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GameStates')
            CREATE TABLE GameStates (
                StateId INT IDENTITY(1,1) PRIMARY KEY,
                SessionId INT NOT NULL,
                StateData NVARCHAR(MAX) NOT NULL,
                SavedAt DATETIME NOT NULL DEFAULT GETDATE()
            );

            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HelperMessages')
            CREATE TABLE HelperMessages (
                MessageId INT IDENTITY(1,1) PRIMARY KEY,
                Context NVARCHAR(100) NOT NULL,
                BuildingName NVARCHAR(100) NULL,
                Message NVARCHAR(MAX) NOT NULL,
                Priority INT DEFAULT 1,
                IsOneTime BIT DEFAULT 1
            );
        ");
    }

    /// <summary>
    /// HelperMessages tablosuna varsayılan mesajları ekler.
    /// </summary>
    public async Task SeedHelperMessagesAsync()
    {
        using var db = CreateConnection();
        var count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM HelperMessages");
        if (count > 0) return;

        await db.ExecuteAsync(@"
            INSERT INTO HelperMessages (Context, BuildingName, Message, Priority, IsOneTime) VALUES
            -- Giriş Ekranı
            (N'splash', NULL, N'Hoş geldin Amirims! Ben Yardımcı Dedektif Çetin. Bu karanlık davada sana yardımcı olacağım. Hazır olduğunda dosyayı aç ve soruşturmaya başlayalım!', 1, 1),
            -- Hikaye Sonrası
            (N'story_end', NULL, N'Soruşturmaya başlamadan önce şunu bil Amirims: Kasabada 5 bina ve 5 şüpheli var. Her binada 3 delil bulabilirsin. Delilleri dikkatle incele ve çantana al. Ama dikkat et, çantanda yalnızca 5 delil bulunabilir!', 1, 1),
            -- Kasaba Haritasına İlk Giriş
            (N'map_enter', NULL, N'İşte kasaba haritası Amirims! Haritadaki binalara tıklayarak soruşturmana başlayabilirsin. Her binada deliller ve şüpheliler seni bekliyor. Dikkatli ol, her binaya sadece bir kez girebilirsin!', 1, 1),
            -- Binaya Girişler (Genel)
            (N'building_enter', NULL, N'Olay yerindeki delilleri inceleyebilir, çantana atabilirsin. Ama dikkat et, çantanda yalnızca 5 delil bulunabilir! Ayrıca buradaki şüpheliyle konuşmayı unutma.', 2, 0),
            -- Binaya Girişler (Bina-Bazlı)
            (N'building_enter', N'Kasap', N'Burası Kasap Hasan''ın dükkânı Amirims. Tezgahtaki satıra, deftere ve yırtık önlüğe dikkat et. Hasan sert bir adam, ama gözlerinde korku var...', 3, 1),
            (N'building_enter', N'Eczane', N'Eczacı Selma''nın dükkânındayız Amirims. Tezgah altına, reçete defterine ve ilaç şişelerine dikkat et. Bu kadın zehirler konusunda uzman...', 3, 1),
            (N'building_enter', N'Muhtarlık', N'Muhtar Kemal''in ofisindeyiz Amirims. Çekmecesindeki mektuplara, kırık gözlüğe ve kasasına dikkat et. Bu adam her şeyi kontrol etmek istiyor...', 3, 1),
            (N'building_enter', N'Karakol', N'Komiser Güneş''in karakolundayız Amirims. Polis rozetine, gizli dosyaya ve kayıp düğmeye dikkat et. Bir polis neden delilleri saklasın ki?', 3, 1),
            (N'building_enter', N'Terzi', N'Terzi Yahya''nın atölyesindeyiz Amirims. İplik makarasına, yırtık kumaşa ve gizli cebe dikkat et. Bu yaşlı adam bildiklerinden fazlasını saklıyor...', 3, 1),
            -- Çanta Açıldığında
            (N'bag_open', NULL, N'Çantandaki delilleri İncele butonuyla detaylı inceleyebilirsin Amirims. Suçluyu bulmak için ipuçlarını birleştir! Unutma, incelediğin deliller çantadan çıkarılamaz.', 1, 1),
            -- Delil İnceleme
            (N'clue_inspect', NULL, N'Bu delili dikkatle incele Amirims. Suçluya ait olabilecek izler görebilirsin. Her detay önemli!', 1, 0),
            -- NPC Konuşma Başlangıcı
            (N'npc_talk', NULL, N'Dikkatli soru sor Amirims, sadece 5 soru hakkın var! Sorularını iyi seç ve NPC''nin tepkilerini iyi gözlemle.', 1, 0),
            -- Otopsi Raporu
            (N'autopsy_ready', NULL, N'Amirims! Adli Tıp Merkezi''nden otopsi raporu geldi! Harita ekranındaki rapora tıklayarak hemen inceleyin, çok önemli bulgular var!', 1, 1),
            -- Suçlama Ekranı
            (N'accuse', NULL, N'Son kararını vermeden önce tüm delilleri gözden geçir Amirims. Yanlış suçlama kasaba için felaket olur! Emin olmadan düğmeye basma!', 1, 1);
        ");
    }
}
