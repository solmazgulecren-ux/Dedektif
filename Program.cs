using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DedektiflikRPG.Controllers;
using DedektiflikRPG.Core.Interfaces;
using DedektiflikRPG.Data;
using DedektiflikRPG.Models;
using DedektiflikRPG.Services;
using DedektiflikRPG.Services.AI;
using DedektiflikRPG.Services.Forensic;
using DedektiflikRPG.UI;
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
            try
            {
                await repository.EnsureHelperTablesAsync();
                await repository.SeedHelperMessagesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠️ Yardımcı tablo oluşturma hatası (ihmal edildi): {ex.Message}");
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

        // Web Modu (Katmanlı Mimari ve DI Servisleri)
        var builder = WebApplication.CreateBuilder(args);

        // Dependency Injection Servis Kayıtları
        builder.Services.AddSingleton<IGameRepository>(repository);
        builder.Services.AddSingleton<IAIService, LocalAiEngine>();
        builder.Services.AddSingleton<IForensicService, ForensicService>();

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

        // Katmanlı API Endpoints Haritalaması
        app.MapGameEndpoints();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n  🌍 Akıllı Dedektiflik RPG Web Sunucusu Başlatıldı! (Katmanlı Mimari v4.0)");
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
}

public class ClueActionRequest
{
    public string Status { get; set; } = string.Empty;
}

public class InterrogationRequest
{
    public int NpcId { get; set; }
    public string Question { get; set; } = string.Empty;
    public int? GuiltyNpcId { get; set; }
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

public class SessionStartRequest
{
    public int GuiltyNpcId { get; set; }
}

public class SessionEndRequest
{
    public int SessionId { get; set; }
    public string Result { get; set; } = string.Empty;
    public int? AccusedNpcId { get; set; }
    public int TotalQuestions { get; set; }
    public int CluesCollected { get; set; }
}

public class ActionLogRequest
{
    public int SessionId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int? TargetId { get; set; }
    public string? Details { get; set; }
}

public class AnalyzeCluesRequest
{
    public List<int>? ClueIds { get; set; }
}

public class GameStateSaveRequest
{
    public int SessionId { get; set; }
    public string StateData { get; set; } = string.Empty;
}

public class DialogLogRequest
{
    public int NpcId { get; set; }
    public string PlayerQuestion { get; set; } = string.Empty;
    public string NpcResponse { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ForensicSubmitRequest
{
    public int ClueId { get; set; }
    public string ClueName { get; set; } = string.Empty;
    public string FindingText { get; set; } = string.Empty;
}
