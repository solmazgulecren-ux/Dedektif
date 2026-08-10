using Dapper;
using DedektiflikRPG.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;

namespace DedektiflikRPG.Data;

/// <summary>
/// Dapper ile SQLite & SQL Server veritabanı işlemlerini yöneten repository sınıfı.
/// NPC, ipucu ve diyalog kayıtları üzerinde CRUD ve otopsi/oturum işlemleri gerçekleştirir.
/// </summary>
public class DatabaseRepository
{
    private readonly string _connectionString;
    private readonly bool _isSqlite;

    public DatabaseRepository(string connectionString)
    {
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? "Data Source=Data/dedektiflik.db"
            : connectionString;

        _isSqlite = _connectionString.Contains("Data Source") || _connectionString.Contains(".db");
        
        if (_isSqlite)
        {
            EnsureDatabaseCreated();
        }
    }

    public IDbConnection CreateConnection()
    {
        if (_isSqlite)
            return new SqliteConnection(_connectionString);
        return new SqlConnection(_connectionString);
    }

    private void EnsureDatabaseCreated()
    {
        try
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "dedektiflik.db");
            var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "schema.sql");
            
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(dbPath) && File.Exists(schemaPath))
            {
                var schemaSql = File.ReadAllText(schemaPath);
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = schemaSql;
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️ SQLite veritabanı başlatma uyarısı: {ex.Message}");
        }
    }

    // =============================================
    // NPC İşlemleri
    // =============================================

    public async Task<IEnumerable<NPC>> GetAllNPCsAsync()
    {
        using var db = CreateConnection();
        return await db.QueryAsync<NPC>("SELECT * FROM NPCs ORDER BY NPCId");
    }

    public async Task<NPC?> GetNPCByIdAsync(int npcId)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<NPC>(
            "SELECT * FROM NPCs WHERE NPCId = @NPCId",
            new { NPCId = npcId });
    }

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

    public async Task<IEnumerable<Clue>> GetAllCluesAsync()
    {
        using var db = CreateConnection();
        try
        {
            return await db.QueryAsync<Clue>("SELECT * FROM Clues ORDER BY ClueId");
        }
        catch
        {
            // Clues yoksa SceneObjects'ten dönüştür
            var objs = await db.QueryAsync<SceneObject>("SELECT * FROM SceneObjects");
            return objs.Select(o => new Clue
            {
                ClueId = o.ObjectId,
                Title = o.ObjectName,
                Description = o.Description,
                RelatedNPCId = o.NPCId,
                Status = "Pending"
            });
        }
    }

    public async Task<IEnumerable<Clue>> GetCluesInBagAsync()
    {
        using var db = CreateConnection();
        try
        {
            return await db.QueryAsync<Clue>("SELECT * FROM Clues WHERE Status = 'KeptInBag' ORDER BY ClueId");
        }
        catch
        {
            return Enumerable.Empty<Clue>();
        }
    }

    public async Task UpdateClueStatusAsync(int clueId, string status)
    {
        using var db = CreateConnection();
        try
        {
            await db.ExecuteAsync("UPDATE Clues SET Status = @Status WHERE ClueId = @ClueId", new { ClueId = clueId, Status = status });
        }
        catch { }
    }

    public async Task<IEnumerable<Clue>> GetCluesByNPCIdAsync(int npcId)
    {
        using var db = CreateConnection();
        try
        {
            return await db.QueryAsync<Clue>(
                "SELECT * FROM Clues WHERE RelatedNPCId = @NPCId ORDER BY ClueId",
                new { NPCId = npcId });
        }
        catch
        {
            return Enumerable.Empty<Clue>();
        }
    }

    // =============================================
    // Diyalog Kayıt İşlemleri
    // =============================================

    public async Task AddDialogLogAsync(DialogLog log)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(@"
            INSERT INTO DialogLogs (NPCId, PlayerQuestion, NPCResponse, DetectedEmotion, TrustChange, CreatedAt)
            VALUES (@NPCId, @PlayerQuestion, @NPCResponse, @DetectedEmotion, @TrustChange, @CreatedAt)",
            log);
    }

    public async Task<IEnumerable<DialogLog>> GetDialogLogsByNPCIdAsync(int npcId)
    {
        using var db = CreateConnection();
        return await db.QueryAsync<DialogLog>(
            "SELECT * FROM DialogLogs WHERE NPCId = @NPCId ORDER BY CreatedAt DESC",
            new { NPCId = npcId });
    }

    public async Task<IEnumerable<DialogLog>> GetRecentDialogLogsAsync(int npcId, int count = 10)
    {
        using var db = CreateConnection();
        if (_isSqlite)
        {
            return await db.QueryAsync<DialogLog>(
                "SELECT * FROM DialogLogs WHERE NPCId = @NPCId ORDER BY CreatedAt DESC LIMIT @Count",
                new { NPCId = npcId, Count = count });
        }
        else
        {
            return await db.QueryAsync<DialogLog>(
                "SELECT TOP (@Count) * FROM DialogLogs WHERE NPCId = @NPCId ORDER BY CreatedAt DESC",
                new { NPCId = npcId, Count = count });
        }
    }

    public async Task<IEnumerable<NPCDialogue>> GetLocalAIPoolAsync(int npcId)
    {
        using var db = CreateConnection();
        return await db.QueryAsync<NPCDialogue>(
            "SELECT * FROM NPCDialogues WHERE NPCId = @NPCId AND Category LIKE 'local_ai_%'",
            new { NPCId = npcId });
    }

    // =============================================
    // Seed Data — Varsayılan Veri Yükleme
    // =============================================

    public async Task SeedDataAsync()
    {
        using var db = CreateConnection();
        try
        {
            var npcCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM NPCs");

            // schema.sql varsa çalıştır (Eğer tablo yoksa vs.)
            var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "schema.sql");
            if (File.Exists(schemaPath) && npcCount < 5)
            {
                var sql = File.ReadAllText(schemaPath);
                await db.ExecuteAsync(sql);
            }

            // Gelişmiş yapay zeka hafıza verilerini yükle (1000+ Cümle)
            var aiSeeder = new AISeeder(this);
            await aiSeeder.Seed1000PlusDialoguesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️ SeedDataAsync uyarısı: {ex.Message}");
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var db = CreateConnection();
            db.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TablesExistAsync()
    {
        try
        {
            using var db = CreateConnection();
            var count = await db.ExecuteScalarAsync<int>(
                _isSqlite 
                    ? "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('NPCs', 'SceneObjects', 'DialogLogs')"
                    : "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('NPCs', 'Clues', 'DialogLogs')");
            return count >= 2;
        }
        catch
        {
            return false;
        }
    }

    // =============================================
    // Oyun Oturumu İşlemleri (GameSessions)
    // =============================================

    public async Task<int> CreateGameSessionAsync(int guiltyNpcId)
    {
        using var db = CreateConnection();
        if (_isSqlite)
        {
            await db.ExecuteAsync("INSERT INTO GameSessions (GuiltyNPCId, StartedAt) VALUES (@GuiltyNPCId, datetime('now'))", new { GuiltyNPCId = guiltyNpcId });
            return await db.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");
        }
        else
        {
            return await db.ExecuteScalarAsync<int>(@"
                INSERT INTO GameSessions (GuiltyNPCId, StartedAt)
                VALUES (@GuiltyNPCId, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { GuiltyNPCId = guiltyNpcId });
        }
    }

    public async Task EndGameSessionAsync(int sessionId, string result, int? accusedNpcId, int totalQuestions, int cluesCollected)
    {
        using var db = CreateConnection();
        string dateFunc = _isSqlite ? "datetime('now')" : "GETDATE()";
        await db.ExecuteAsync($@"
            UPDATE GameSessions 
            SET EndedAt = {dateFunc}, Result = @Result, AccusedNPCId = @AccusedNPCId,
                TotalQuestions = @TotalQuestions, CluesCollected = @CluesCollected
            WHERE SessionId = @SessionId",
            new { SessionId = sessionId, Result = result, AccusedNPCId = accusedNpcId, TotalQuestions = totalQuestions, CluesCollected = cluesCollected });
    }

    public async Task LogPlayerActionAsync(int sessionId, string actionType, int? targetId, string? details)
    {
        using var db = CreateConnection();
        string dateFunc = _isSqlite ? "datetime('now')" : "GETDATE()";
        await db.ExecuteAsync($@"
            INSERT INTO PlayerActions (SessionId, ActionType, TargetId, Details, CreatedAt)
            VALUES (@SessionId, @ActionType, @TargetId, @Details, {dateFunc})",
            new { SessionId = sessionId, ActionType = actionType, TargetId = targetId, Details = details });
    }

    public async Task SaveGameStateAsync(int sessionId, string stateDataJson)
    {
        using var db = CreateConnection();
        string dateFunc = _isSqlite ? "datetime('now')" : "GETDATE()";
        var exists = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM GameStates WHERE SessionId = @SessionId",
            new { SessionId = sessionId });
        
        if (exists > 0)
        {
            await db.ExecuteAsync($@"
                UPDATE GameStates SET StateData = @StateData, SavedAt = {dateFunc}
                WHERE SessionId = @SessionId",
                new { SessionId = sessionId, StateData = stateDataJson });
        }
        else
        {
            await db.ExecuteAsync($@"
                INSERT INTO GameStates (SessionId, StateData, SavedAt)
                VALUES (@SessionId, @StateData, {dateFunc})",
                new { SessionId = sessionId, StateData = stateDataJson });
        }
    }

    public async Task<string?> LoadGameStateAsync(int sessionId)
    {
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<string?>(
            _isSqlite 
                ? "SELECT StateData FROM GameStates WHERE SessionId = @SessionId ORDER BY SavedAt DESC LIMIT 1"
                : "SELECT TOP 1 StateData FROM GameStates WHERE SessionId = @SessionId ORDER BY SavedAt DESC",
            new { SessionId = sessionId });
    }

    public async Task<IEnumerable<HelperMessage>> GetHelperMessagesAsync(string context, string? buildingName = null)
    {
        using var db = CreateConnection();
        try
        {
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
        catch
        {
            return Enumerable.Empty<HelperMessage>();
        }
    }

    public async Task<string> AnalyzeCluesForHelperAsync(List<int> clueIds, int guiltyNpcId)
    {
        if (clueIds == null || clueIds.Count == 0)
            return "Amirims, çantanızda henüz delil yok! Binalara girip delilleri toplamalısınız.";

        using var db = CreateConnection();
        try
        {
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
        catch
        {
            return $"Amirims, çantanızdaki {clueIds.Count} delili dikkatle inceleyin. Katil bu delillerin arasında gizli!";
        }
    }

    public async Task LogDialogWithCategoryAsync(int npcId, string playerQuestion, string npcResponse, int difficulty, string category)
    {
        using var db = CreateConnection();
        string dateFunc = _isSqlite ? "datetime('now')" : "GETDATE()";
        await db.ExecuteAsync($@"
            INSERT INTO DialogLogs (NPCId, PlayerQuestion, NPCResponse, Difficulty, Category, CreatedAt)
            VALUES (@NPCId, @PlayerQuestion, @NPCResponse, @Difficulty, @Category, {dateFunc})",
            new { NPCId = npcId, PlayerQuestion = playerQuestion, NPCResponse = npcResponse, Difficulty = difficulty, Category = category });
    }

    public async Task EnsureHelperTablesAsync()
    {
        using var db = CreateConnection();
        if (_isSqlite)
        {
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS GameSessions (
                    SessionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    GuiltyNPCId INTEGER NOT NULL,
                    StartedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    EndedAt TEXT NULL,
                    Result TEXT NULL,
                    AccusedNPCId INTEGER NULL,
                    TotalQuestions INTEGER DEFAULT 0,
                    CluesCollected INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS PlayerActions (
                    ActionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId INTEGER NOT NULL,
                    ActionType TEXT NOT NULL,
                    TargetId INTEGER NULL,
                    Details TEXT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS GameStates (
                    StateId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId INTEGER NOT NULL,
                    StateData TEXT NOT NULL,
                    SavedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS HelperMessages (
                    MessageId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Context TEXT NOT NULL,
                    BuildingName TEXT NULL,
                    Message TEXT NOT NULL,
                    Priority INTEGER DEFAULT 1,
                    IsOneTime INTEGER DEFAULT 1
                );
            ");
        }
        else
        {
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
    }

    public async Task SeedHelperMessagesAsync()
    {
        using var db = CreateConnection();
        try
        {
            var count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM HelperMessages");
            if (count > 0) return;

            await db.ExecuteAsync(@"
                INSERT INTO HelperMessages (Context, BuildingName, Message, Priority, IsOneTime) VALUES
                ('splash', NULL, 'Hoş geldin Amirims! Ben Yardımcı Dedektif Çetin. Bu karanlık davada sana yardımcı olacağım. Hazır olduğunda dosyayı aç ve soruşturmaya başlayalım!', 1, 1),
                ('story_end', NULL, 'Soruşturmaya başlamadan önce şunu bil Amirims: Kasabada 5 bina ve 5 şüpheli var. Her binada 3 delil bulabilirsin. Delilleri dikkatle incele ve çantana al. Ama dikkat et, çantanda yalnızca 5 delil bulunabilir!', 1, 1),
                ('map_enter', NULL, 'İşte kasaba haritası Amirims! Haritadaki binalara tıklayarak soruşturmana başlayabilirsin. Her binada deliller ve şüpheliler seni bekliyor. Dikkatli ol!', 1, 1),
                ('building_enter', NULL, 'Olay yerindeki delilleri inceleyebilir, çantana atabilirsin. Ama dikkat et, çantanda yalnızca 5 delil bulunabilir!', 2, 0),
                ('building_enter', 'Kasap', 'Burası Kasap Hasan''ın dükkânı Amirims. Tezgahtaki satıra, deftere ve yırtık önlüğe dikkat et. Hasan sert bir adam...', 3, 1),
                ('building_enter', 'Eczane', 'Eczacı Selma''nın dükkânındayız Amirims. Tezgah altına, reçete defterine ve ilaç şişelerine dikkat et...', 3, 1),
                ('building_enter', 'Muhtarlık', 'Muhtar Kemal''in ofisindeyiz Amirims. Çekmecesindeki mektuplara, kırık gözlüğe ve kasasına dikkat et...', 3, 1),
                ('building_enter', 'Karakol', 'Komiser Güneş''in karakolundayız Amirims. Polis rozetine, gizli dosyaya ve kayıp düğmeye dikkat et...', 3, 1),
                ('building_enter', 'Terzi', 'Terzi Yahya''nın atölyesindeyiz Amirims. İplik makarasına, yırtık kumaşa ve gizli cebe dikkat et...', 3, 1),
                ('bag_open', NULL, 'Çantandaki delilleri İncele butonuyla detaylı inceleyebilirsin Amirims. Suçluyu bulmak için ipuçlarını birleştir!', 1, 1),
                ('clue_inspect', NULL, 'Bu delili dikkatle incele Amirims. Suçluya ait olabilecek izler görebilirsin.', 1, 0),
                ('npc_talk', NULL, 'Dikkatli soru sor Amirims, sadece 5 soru hakkın var! Sorularını iyi seç.', 1, 0),
                ('autopsy_ready', NULL, 'Amirims! Adli Tıp Merkezi''nden otopsi raporu geldi! Harita ekranındaki rapora tıklayarak hemen inceleyin!', 1, 1),
                ('accuse', NULL, 'Son kararını vermeden önce tüm delilleri gözden geçir Amirims. Yanlış suçlama kasaba için felaket olur!', 1, 1);
            ");
        }
        catch { }
    }
}
