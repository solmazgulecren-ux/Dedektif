using DedektiflikRPG.Data;
using DedektiflikRPG.Services;
using DedektiflikRPG.UI;
using System.Text.Json;

namespace DedektiflikRPG;

/// <summary>
/// AI Destekli Dedektiflik RPG — Ana Giriş Noktası
/// Veritabanı bağlantısını kurar, servisleri başlatır ve oyun döngüsünü çalıştırır.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // =============================================
        // 1. Konfigürasyonu oku
        // =============================================
        var config = LoadConfiguration();

        var connectionString = config.ConnectionString;
        var geminiApiKey = config.GeminiApiKey;
        var geminiModel = config.GeminiModel;

        // Ortam değişkenlerinden de okunabilir
        if (string.IsNullOrEmpty(geminiApiKey))
            geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";

        // =============================================
        // 2. Başlangıç kontrolleri
        // =============================================
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n  🔧 AI Dedektiflik RPG başlatılıyor...\n");
        Console.ResetColor();

        // Veritabanı bağlantısı kontrolü
        var repository = new DatabaseRepository(connectionString);
        Console.Write("  📡 Veritabanı bağlantısı kontrol ediliyor... ");

        if (await repository.TestConnectionAsync())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Bağlantı başarılı!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Bağlantı başarısız!");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  ⚠️  SQL Server bağlantı hatası.");
            Console.WriteLine("  Lütfen aşağıdakileri kontrol edin:");
            Console.WriteLine("    1. SQL Server'ın çalışır durumda olduğunu");
            Console.WriteLine("    2. appsettings.json dosyasındaki ConnectionString değerini");
            Console.WriteLine("    3. Data/schema.sql dosyasını SQL Server'da çalıştırdığınızı");
            Console.ResetColor();
            Console.WriteLine("\n  Devam etmek için bir tuşa basın...");
            SafeReadKey();
            return;
        }

        // Tabloların varlığını kontrol et
        Console.Write("  📋 Tablolar kontrol ediliyor... ");
        if (await repository.TablesExistAsync())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Tablolar mevcut.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Tablolar bulunamadı!");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  ⚠️  Veritabanında gerekli tablolar mevcut değil.");
            Console.WriteLine("  Lütfen 'Data/schema.sql' dosyasını SQL Server'da çalıştırın.");
            Console.ResetColor();
            Console.WriteLine("\n  Devam etmek için bir tuşa basın...");
            SafeReadKey();
            return;
        }

        // Seed data yükle
        Console.Write("  🌱 Varsayılan veriler yükleniyor... ");
        await repository.SeedDataAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Veriler hazır.");
        Console.ResetColor();

        // API key kontrolü
        Console.Write("  🤖 Gemini API anahtarı kontrol ediliyor... ");
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  API anahtarı bulunamadı!");
            Console.ResetColor();
            Console.WriteLine("  appsettings.json veya GEMINI_API_KEY ortam değişkenini ayarlayın.");
            Console.WriteLine("  Oyun AI desteği olmadan başlatılıyor...");
            Thread.Sleep(2000);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Model: {geminiModel}");
            Console.ResetColor();
        }

        // =============================================
        // 3. Servisleri başlat
        // =============================================
        var aiService = new AntigravityAiService(geminiApiKey, geminiModel);
        var dialogManager = new DialogManager(repository, aiService);
        var consoleUI = new ConsoleUI(repository, dialogManager);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n  ✅ Tüm sistemler hazır! Oyun başlıyor...\n");
        Console.ResetColor();
        Thread.Sleep(1500);

        // =============================================
        // 4. Oyun döngüsünü başlat
        // =============================================
        await consoleUI.RunAsync();
    }

    /// <summary>
    /// appsettings.json dosyasından konfigürasyonu okur.
    /// </summary>
    private static AppConfig LoadConfiguration()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        // Eğer debug/geliştirme sırasında dosya proje kökünde ise onu dene
        if (!File.Exists(configPath))
            configPath = "appsettings.json";

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
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠️  appsettings.json okunamadı, varsayılan ayarlar kullanılıyor.");
                Console.ResetColor();
            }
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

/// <summary>
/// Uygulama konfigürasyon modeli
/// </summary>
public class AppConfig
{
    public string ConnectionString { get; set; } =
        "Server=(localdb)\\MSSQLLocalDB;Database=DedektiflikRPG;Trusted_Connection=true;TrustServerCertificate=true;";
    public string GeminiApiKey { get; set; } = "";
    public string GeminiModel { get; set; } = "gemini-2.0-flash";
}
