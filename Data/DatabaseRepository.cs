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
        if (npcCount > 0)
            return; // Veri zaten var, seed yapma

        // NPC'leri ekle
        await db.ExecuteAsync(@"
            INSERT INTO NPCs (Name, Role, TrustLevel, FearLevel, IsGuilty, SecretInfo)
            VALUES
                (N'Kasap Hasan',   N'Kasabadaki eski kasap, herkesin tanıdığı bir figür.',            50, 20, 0, N'Cinayet gecesi dükkânında gizlice birine et sattı.'),
                (N'Eczacı Selma',  N'Eczane sahibi, ilaç ve zehir konusunda uzman.',                  50, 45, 0, N'Kurbanın kullandığı ilacın yan etkilerini biliyordu ama gizledi.'),
                (N'Muhtar Kemal',  N'Kasabanın muhtarı, herkesin sırrını bilen bir politikacı.',       50, 60, 1, N'Kurbanla arazi anlaşmazlığı vardı ve onu tehdit etmişti.')");

        // İpuçlarını ekle
        await db.ExecuteAsync(@"
            INSERT INTO Clues (Title, Description, RelatedNPCId)
            VALUES
                (N'Kanlı Bıçak',           N'Olay yerinde bulunan paslanmış bir kasap bıçağı.',                          1),
                (N'Boş İlaç Şişesi',       N'Kurbanın evinde bulunan etiketsiz ilaç şişesi.',                            2),
                (N'Tehdit Mektubu',        N'Kurbanın çekmecesinden çıkan, muhtarın el yazısına benzeyen mektup.',        3),
                (N'Tanık İfadesi',         N'Bir komşu, cinayet gecesi muhtarın evinden bağırışlar duyduğunu söyledi.',   3),
                (N'Güvenlik Kamerası',     N'Eczanenin önündeki kamera, gece yarısı şüpheli bir silüet yakalamış.',       2)");
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
}
