using DedektiflikRPG.Data;
using DedektiflikRPG.Models;
using DedektiflikRPG.Services;
using DedektiflikRPG.UI;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DedektiflikRPG;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var config = LoadConfiguration();
        var connectionString = config.ConnectionString;
        var geminiApiKey = config.GeminiApiKey;
        var geminiModel = config.GeminiModel;

        if (string.IsNullOrEmpty(geminiApiKey))
            geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";

        // Veritabanı kontrolü ve Seed
        var repository = new DatabaseRepository(connectionString);
        bool dbConnected = await repository.TestConnectionAsync();

        if (dbConnected)
        {
            if (await repository.TablesExistAsync())
            {
                await repository.SeedDataAsync();
            }
        }

        // Eğer console modunda çalıştırılmak istenirse
        if (args.Contains("--console"))
        {
            if (!dbConnected)
            {
                Console.WriteLine("Hata: Veritabanı bağlantısı kurulamadı. Konsol modu başlatılamıyor.");
                return;
            }
            var aiService = new AntigravityAiService(geminiApiKey, geminiModel);
            var dialogManager = new DialogManager(repository, aiService);
            var consoleUI = new ConsoleUI(repository, dialogManager);
            await consoleUI.RunAsync();
            return;
        }

        // Web Modu (Default)
        var builder = WebApplication.CreateBuilder(args);

        // CORS Ekle
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Statik Dosyaları Sun
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseCors("AllowAll");

        // Servisleri oluştur
        var aiServiceInstance = new AntigravityAiService(geminiApiKey, geminiModel);
        var dialogManagerInstance = new DialogManager(repository, aiServiceInstance);

        // =============================================
        // API Uç Noktaları (Endpoints)
        // =============================================

        // 1. Şüpheli Listesi
        app.MapGet("/api/game/npcs", async () =>
        {
            try
            {
                var npcs = await repository.GetAllNPCsAsync();
                return Results.Ok(npcs);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // 2. İpuçları Listesi
        app.MapGet("/api/game/clues", async () =>
        {
            try
            {
                var clues = await repository.GetAllCluesAsync();
                return Results.Ok(clues);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // 3. İpucu Durumu Güncelle (Sakla / Gereksiz)
        app.MapPost("/api/game/clues/{id}/action", async (int id, ClueActionRequest request) =>
        {
            try
            {
                // status: "KeptInBag" veya "IgnoredAtScene"
                if (request.Status != "KeptInBag" && request.Status != "IgnoredAtScene" && request.Status != "Pending")
                {
                    return Results.BadRequest("Geçersiz durum değeri.");
                }

                await repository.UpdateClueStatusAsync(id, request.Status);
                return Results.Ok(new { success = true, clueId = id, status = request.Status });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // 4. Sorgulama Yap
        app.MapPost("/api/game/interrogate", async (InterrogationRequest request) =>
        {
            try
            {
                var result = await dialogManagerInstance.ProcessQuestionAsync(request.NpcId, request.Question);
                if (result == null)
                {
                    return Results.NotFound("Şüpheli bulunamadı.");
                }

                return Results.Ok(new
                {
                    dialogue = result.Value.Response.Dialogue,
                    emotion = result.Value.Response.Emotion,
                    trustChange = result.Value.Response.TrustChange,
                    revealedSecret = result.Value.Response.RevealedSecret,
                    updatedNpc = result.Value.UpdatedNPC
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // 5. Suçlamada Bulun
        app.MapPost("/api/game/accuse", async (AccuseRequest request) =>
        {
            try
            {
                var npc = await repository.GetNPCByIdAsync(request.NpcId);
                if (npc == null) return Results.NotFound("Şüpheli bulunamadı.");

                if (npc.IsGuilty)
                {
                    return Results.Ok(new { success = true, message = $"Tebrikler! Suçlunun {npc.Name} olduğunu doğru tahmin ettiniz.", secret = npc.SecretInfo });
                }
                else
                {
                    return Results.Ok(new { success = false, message = $"{npc.Name} masum çıktı! Soruşturma başarısız oldu." });
                }
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // 6. Oyunu Sıfırla
        app.MapPost("/api/game/reset", async () =>
        {
            try
            {
                // Tabloları temizle ve seed et
                using var db = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await db.OpenAsync();
                using var cmd = db.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Clues SET Status = 'Pending';
                    UPDATE NPCs SET TrustLevel = 50, FearLevel = 30;
                    DELETE FROM DialogLogs;
                ";
                await cmd.ExecuteNonQueryAsync();

                return Results.Ok(new { success = true, message = "Oyun durumu sıfırlandı." });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n  🌍 Akıllı Dedektiflik RPG Web Sunucusu Başlatıldı!");
        Console.WriteLine("  👉 Tarayıcıda Açın: http://localhost:5000 \n");
        Console.ResetColor();

        await app.RunAsync("http://localhost:5000");
    }

    private static AppConfig LoadConfiguration()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(configPath)) configPath = "appsettings.json";

        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return config ?? new AppConfig();
            }
            catch { }
        }
        return new AppConfig();
    }

    public static void SafeReadKey()
    {
        if (!Console.IsInputRedirected)
        {
            try { Console.ReadKey(true); } catch { }
        }
    }
}

public class ClueActionRequest
{
    public string Status { get; set; } = string.Empty;
}

public class InterrogationRequest
{
    public int NpcId { get; set; }
    public string Question { get; set; } = string.Empty;
}

public class AccuseRequest
{
    public int NpcId { get; set; }
}

public class AppConfig
{
    public string ConnectionString { get; set; } = "Server=(localdb)\\MSSQLLocalDB;Database=DedektiflikRPG;Trusted_Connection=true;TrustServerCertificate=true;";
    public string GeminiApiKey { get; set; } = "";
    public string GeminiModel { get; set; } = "gemini-2.0-flash";
}
