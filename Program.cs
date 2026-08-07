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
            // Yardımcı tabloları oluştur ve seed et
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

        // 4. Sorgulama Yap (Veritabanı Sabit Cevap)
        app.MapPost("/api/game/interrogate", async (InterrogationRequest request) =>
        {
            try
            {
                var npc = await repository.GetNPCByIdAsync(request.NpcId);
                if (npc == null) return Results.NotFound("Şüpheli bulunamadı.");

                // Hardcoded mock for now to bypass AI
                return Results.Ok(new
                {
                    dialogue = "Bu konuda konuşmak istemiyorum. Zaten bildiğim her şeyi anlattım.",
                    emotion = "Sinirli",
                    trustChange = -10,
                    revealedSecret = "",
                    updatedNpc = npc
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

                // Gerçek suçluyu bul
                var npcs = await repository.GetAllNPCsAsync();
                var guiltyNpc = npcs.FirstOrDefault(n => n.IsGuilty);
                var guiltyName = guiltyNpc?.Name ?? "Bilinmiyor";
                var guiltyId = guiltyNpc?.NPCId ?? 0;

                if (npc.IsGuilty)
                {
                    return Results.Ok(new { success = true, message = $"Tebrikler! Suçlunun {npc.Name} olduğunu doğru tahmin ettiniz.", accusedName = npc.Name, guiltyNpcName = guiltyName, guiltyNpcId = guiltyId });
                }
                else
                {
                    return Results.Ok(new { success = false, message = $"{npc.Name} masum çıktı! Gerçek katil {guiltyName} idi.", accusedName = npc.Name, guiltyNpcName = guiltyName, guiltyNpcId = guiltyId });
                }
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // =============================================
        // YENİ EKLENEN C# BACKEND SİSTEMLERİ (OTOPSİ & DETAYLAR)
        // =============================================

        // OTOPSİ RAPORU ENDPOINT
        app.MapGet("/api/game/autopsy", async () =>
        {
            try
            {
                var npcs = await repository.GetAllNPCsAsync();
                var guiltyNpc = npcs.FirstOrDefault(n => n.IsGuilty);
                
                if (guiltyNpc == null) return Results.NotFound("Suçlu atanmamış.");

                string report = "=== OTOPSİ RAPORU ===\n";
                report += "Kurban: Osman Bey\n";
                report += "Ölüm Saati: Cinayet gecesi 23:45 - 00:30 arası.\n\n";
                report += "BULGULAR:\n";

                switch (guiltyNpc.NPCId)
                {
                    case 1: // Kasap
                        report += "- Boyun bölgesinde ağır ve nispeten kör bir kesici aletle oluşturulmuş geniş ve derin bir yara mevcuttur.\n";
                        report += "- Yaranın yapısı, darbenin oldukça yüksek bir kuvvetle indirildiğini göstermektedir.\n";
                        report += "- Kurbanın sağ el tırnak altı dokularında, mücadele esnasında koptuğu anlaşılan kalın, koyu renkli ve muhtemelen deri veya benzeri dayanıklı bir kumaş türüne ait mikroskobik lifler ve parçacıklar izole edilmiştir.";
                        break;
                    case 2: // Eczacı
                        report += "- Kurbanın vücudunda boğuşmaya veya herhangi bir fiziksel travmaya bağlı harici bir ize rastlanmamıştır.\n";
                        report += "- Kan örneği üzerinde yapılan detaylı toksikolojik tarama sonucunda, kardiyovasküler sistemi doğrudan hedef alan nadir bitkisel bir toksin türünün varlığı saptanmıştır.\n";
                        report += "- Toksinin kurbanın sistemine, muhtemelen düzenli kullandığı farmakolojik bir madde üzerinden alındığı, bu maddenin yavaşça etki gösterecek dozajda hazırlandığı tespit edilmiştir.";
                        break;
                    case 3: // Muhtar
                        report += "- Kurbanın kollarında ve göğsünde belirgin savunma yaraları ve ciddi bir arbedenin izleri mevcuttur.\n";
                        report += "- Kafatasının sağ paryetal bölgesinde, düz yüzeyli ağır ve sert bir cisimle oluşturulmuş künt travmaya bağlı kırık ve subdural kanama saptanmıştır.\n";
                        report += "- Kurbanın kıyafetleri üzerinde yapılan kimyasal analizde, yakın zamanda yoğun bir basınca maruz kalmış mürekkep lekeleri ve sert kâğıt kalıntıları gözlemlenmiştir.";
                        break;
                    case 4: // Komiser
                        report += "- Gövde ve kollar üzerinde uzun, silindirik, esnek fakat ağır bir nesnenin tekrar tekrar vurulması sonucu oluşan hematomlar izlenmiştir.\n";
                        report += "- Kurbanın boyun çevresinde, travmaya ek olarak asfiksiye (havasız kalma) işaret eden kanamalar ve sıkışma bulguları saptanmıştır.\n";
                        report += "- Cesedin üzerindeki bazı epitel dokuların özel bir solüsyonla silindiği, olay sonrasında delilleri yok etmeye yönelik sistemli bir çaba gösterildiği saptanmıştır.";
                        break;
                    case 5: // Terzi
                        report += "- Kurbanın boyun çevresinde çepeçevre uzanan, çok ince, pürüzsüz ancak aşırı yüksek gerilime dayanıklı bir bağ (özel büküm materyal) ile oluşturulmuş ligatür izi (boğulma) görülmektedir.\n";
                        report += "- Tırnak altlarında savunma izine rastlanmamış olması, eylemin kurbanın arkasından, aniden ve kurbanın failin yaklaşmasına müsaade ettiği bir mesafeden gerçekleştirildiğini göstermektedir.\n";
                        report += "- Kurbanın dış giysisinde (ceket) sıradışı bir ağırlık dengesizliği ve iç astarına sonradan eklenmiş, oldukça nizami gizli bir bölme tespit edilmiştir.";
                        break;
                    default:
                        report += "- Kesin ölüm sebebi belirlenemedi. Adli tıp incelemesi sürüyor.";
                        break;
                }

                return Results.Ok(new { success = true, report = report });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // DİNAMİK İPUCU DETAYI ENDPOINT
        app.MapGet("/api/game/clue-detail/{clueId}", async (int clueId) =>
        {
            try
            {
                var npcs = await repository.GetAllNPCsAsync();
                var guiltyNpc = npcs.FirstOrDefault(n => n.IsGuilty);
                int guiltyId = guiltyNpc?.NPCId ?? 0;

                string text = "Bu nesne karanlık sırlar barındırıyor...";

                switch(clueId) 
                {
                    case 1: text = "Üzerindeki kan lekeleri Osman Bey'e ait gibi görünüyor. " + (guiltyId == 1 ? "Sapındaki el izi net bir şekilde Kasap Hasan'ı işaret ediyor." : "Ancak satırın sapında kasaba ait olmayan, eldivenle tutulmuş gibi garip izler var."); break;
                    case 2: text = "Sayfalarda Osman Bey'in adı kırmızıyla çizilmiş. Yanında bir not: " + (guiltyId == 1 ? "'Borcunu ödemedi, cezasını çekecek.'" : "'Bu borç sadece başlangıç.'"); break;
                    case 3: text = "Kavga izleri taşıyan önlük... Osman Bey'in ceketinin düğmesi önlüğün cebinde bulunuyor. " + (guiltyId == 1 ? "Hasan o gece kurbanla boğuşmuş olmalı." : ""); break;
                    case 4: text = "Zehirli bir ilacın boş şişesi. Reçetede Osman Bey'in adı var. " + (guiltyId == 2 ? "Etiketin arkasında Selma'nın el yazısıyla 'Son Doz' yazıyor." : "Şişe aceleyle alınmış gibi, kapağı zorlanmış."); break;
                    case 5: text = "Osman Bey'e verilen ilaçların listesi. Son sayfa yırtık. " + (guiltyId == 2 ? "Yırtık sayfanın izinde 'Zehir' kelimesi okunabiliyor." : "Birisi kanıtları yok etmek için defteri zorla yırtmış."); break;
                    case 6: text = "Bu bitkinin özü, Osman Bey'in kanında bulunan zehirle aynı. " + (guiltyId == 2 ? "Selma bunu kasten hazırlamış." : "Birisi Selma'nın dükkanından bu otu gizlice almış olabilir."); break;
                    case 7: text = "Mektupta 'Osman, o araziler benim, sonun yaklaşıyor' yazıyor. " + (guiltyId == 3 ? "Muhtar Kemal açıkça kurbanı tehdit etmiş ve bunu gerçekleştirmiş." : "Ancak mektup asla postalanmamış, sadece bir sinir anında yazılmış."); break;
                    case 8: text = "Osman Bey'in kırık gözlüğü... " + (guiltyId == 3 ? "Muhtarın odasında şiddetli bir kavga yaşanmış." : "Gözlük bir başka yerde kırılıp buraya bırakılmış olabilir."); break;
                    case 9: text = "Kasada Osman Bey'in arazilerine ait sahte tapular var. " + (guiltyId == 3 ? "Kemal her şeyi planlamış, cinayet sebebi bu tapular." : "Bu tapular sadece muhtarın açgözlülüğünü gösteriyor, cinayeti değil."); break;
                    case 10: text = "Rozetin numarası kazınmış. Osman Bey'in cesedinin hemen yanında bulundu. " + (guiltyId == 4 ? "Güneş, kurbanla olay yerinde boğuşurken rozetini düşürmüş." : "Rozet oraya özellikle bir polisi suçlamak için bırakılmış."); break;
                    case 11: text = "Dosyada Osman Bey'in gizli geçmişi var. " + (guiltyId == 4 ? "Komiser Güneş, bu geçmişi kullanarak kurbanı şantaj yapıyordu." : "Dosya sadece prosedür gereği tutulmuş."); break;
                    case 12: text = "Pahalı bir palto düğmesi. Osman Bey'in cebinden çıktı. " + (guiltyId == 4 ? "Güneş'in paltosundan kopmuş, arbede sırasında Osman Bey onu tutmuş." : "Bu düğme terzinin bir müşterisine de ait olabilir."); break;
                    case 13: text = "İplik, Osman Bey'in ceketinin dikişleriyle aynı. Üzerindeki kan... " + (guiltyId == 5 ? "Kurbanın kanı. Yahya kurbanı öldürürken makara elindeydi." : "Terzinin dikiş yaparken kendi elini kestiği bir kaza olabilir."); break;
                    case 14: text = "Osman Bey'in ceketinden kopan kumaş. " + (guiltyId == 5 ? "Yahya kurbanla boğuşurken kumaş yırtıldı." : "Kumaş sadece bir terzi artığı olabilir."); break;
                    case 15: text = "Cepteki notta 'Osman, bu gece gel konuşalım' yazıyor. " + (guiltyId == 5 ? "Yahya onu çağırdı ve tuzağa düşürdü." : "Yahya çağırdı ama gittiğinde onu ölü buldu."); break;
                }

                return Results.Ok(new { success = true, text = text });
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
                // Rastgele suçlu seç (1-5 arası)
                var random = new Random();
                int guiltyId = random.Next(1, 6);
                
                try
                {
                    // Veritabanı varsa güncelle
                    using var db = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                    await db.OpenAsync();
                    using var cmd = db.CreateCommand();
                    cmd.CommandText = @"
                        IF OBJECT_ID('Clues', 'U') IS NOT NULL UPDATE Clues SET Status = 'Pending';
                        UPDATE NPCs SET TrustLevel = 50, FearLevel = 30;
                        DELETE FROM DialogLogs;
                        UPDATE NPCs SET IsGuilty = 0;
                        UPDATE NPCs SET IsGuilty = 1 WHERE NPCId = @GuiltyId;
                    ";
                    cmd.Parameters.AddWithValue("@GuiltyId", guiltyId);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // DB bağlantısı yoksa sadece client-side suçlu belirle
                }

                return Results.Ok(new { success = true, message = "Oyun durumu sıfırlandı ve yeni suçlu belirlendi.", guiltyNpcId = guiltyId });
            }
            catch (Exception ex)
            {
                // Fallback: veritabanı olmasa bile suçlu belirle
                var fallbackGuilty = new Random().Next(1, 6);
                return Results.Ok(new { success = true, message = "Oyun sıfırlandı (offline mod).", guiltyNpcId = fallbackGuilty });
            }
        });

        // DİNAMİK DİYALOG ENDPOINT
        app.MapGet("/api/game/dialogues", (int npcId, string category) =>
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "dialogues.json");
                if (!File.Exists(filePath)) return Results.NotFound("Diyalog dosyası bulunamadı.");

                var jsonStr = File.ReadAllText(filePath);
                var allDialogues = JsonSerializer.Deserialize<Dictionary<string, List<DialogueNode>>>(jsonStr);

                if (allDialogues != null && allDialogues.TryGetValue(npcId.ToString(), out var npcDialogues))
                {
                    var contextualPool = npcDialogues.Where(d => d.category == category).ToList();
                    var questionsToShow = contextualPool.Count > 0 ? contextualPool : npcDialogues;
                    
                    // Rastgele en fazla 4 soru seç
                    var rnd = new Random();
                    var count = Math.Min(4, questionsToShow.Count);
                    var selected = questionsToShow.OrderBy(x => rnd.Next()).Take(count).ToList();

                    return Results.Ok(new { success = true, dialogues = selected });
                }

                return Results.NotFound("NPC diyalogları bulunamadı.");
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // =============================================
        // YENİ: OTURUM YÖNETİMİ
        // =============================================

        // Yeni oyun oturumu başlat
        app.MapPost("/api/game/session/start", async (SessionStartRequest request) =>
        {
            try
            {
                var sessionId = await repository.CreateGameSessionAsync(request.GuiltyNpcId);
                return Results.Ok(new { success = true, sessionId = sessionId });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { success = false, sessionId = 0, error = ex.Message });
            }
        });

        // Oturumu sonlandır
        app.MapPost("/api/game/session/end", async (SessionEndRequest request) =>
        {
            try
            {
                await repository.EndGameSessionAsync(request.SessionId, request.Result, request.AccusedNpcId, request.TotalQuestions, request.CluesCollected);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { success = false, error = ex.Message });
            }
        });

        // =============================================
        // YENİ: OYUNCU AKSİYON KAYDI
        // =============================================

        app.MapPost("/api/game/action/log", async (ActionLogRequest request) =>
        {
            try
            {
                await repository.LogPlayerActionAsync(request.SessionId, request.ActionType, request.TargetId, request.Details);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { success = false, error = ex.Message });
            }
        });

        // =============================================
        // YENİ: YARDIMCI DEDEKTİF ÇETİN SİSTEMİ
        // =============================================

        // Bağlama göre Çetin mesajı getir
        app.MapGet("/api/game/helper/tip", async (string context, string? building) =>
        {
            try
            {
                var messages = await repository.GetHelperMessagesAsync(context, building);
                var topMessage = messages.FirstOrDefault();
                if (topMessage != null)
                {
                    return Results.Ok(new { success = true, message = topMessage.Message, context = topMessage.Context, priority = topMessage.Priority });
                }
                return Results.Ok(new { success = false, message = "", context = context, priority = 0 });
            }
            catch (Exception ex)
            {
                // Fallback: Veritabanı yoksa da çalışsın
                var fallbackMessages = new Dictionary<string, string>
                {
                    ["splash"] = "Hoş geldin Amirims! Ben Yardımcı Dedektif Çetin. Bu karanlık davada sana yardımcı olacağım!",
                    ["story_end"] = "Kasabada 5 bina ve 5 şüpheli var. Delilleri dikkatle incele ve çantana al. Ama dikkat et, çantanda yalnızca 5 delil bulunabilir!",
                    ["map_enter"] = "Haritadaki binalara tıklayarak soruşturmana başlayabilirsin. Her binaya sadece bir kez girebilirsin!",
                    ["building_enter"] = "Olay yerindeki delilleri inceleyebilir, çantana atabilirsin. Ama dikkat et, çantanda yalnızca 5 delil bulunabilir!",
                    ["bag_open"] = "Çantandaki delilleri 'İncele' butonuyla detaylı inceleyebilirsin. Suçluyu bulmak için ipuçlarını birleştir!",
                    ["npc_talk"] = "Dikkatli soru sor Amirims, sadece 5 soru hakkın var!",
                    ["accuse"] = "Son kararını vermeden önce tüm delilleri gözden geçir Amirims. Yanlış suçlama kasaba için felaket olur!"
                };
                var msg = fallbackMessages.GetValueOrDefault(context, "Amirims, soruşturmaya devam edin!");
                return Results.Ok(new { success = true, message = msg, context = context, priority = 1 });
            }
        });

        // Çetin'in delil analizi
        app.MapPost("/api/game/helper/analyze-clues", async (AnalyzeCluesRequest request) =>
        {
            try
            {
                var npcs = await repository.GetAllNPCsAsync();
                var guiltyNpc = npcs.FirstOrDefault(n => n.IsGuilty);
                int guiltyId = guiltyNpc?.NPCId ?? 0;
                
                var analysis = await repository.AnalyzeCluesForHelperAsync(request.ClueIds ?? new List<int>(), guiltyId);
                return Results.Ok(new { success = true, analysis = analysis });
            }
            catch (Exception ex)
            {
                string fallback = request.ClueIds?.Count > 0 
                    ? $"Amirims, {request.ClueIds.Count} delil toplamışsınız. Delilleri dikkatlice inceleyin!"
                    : "Amirims, çantanızda henüz delil yok! Binalara girip delilleri toplamalısınız.";
                return Results.Ok(new { success = true, analysis = fallback });
            }
        });

        // =============================================
        // YENİ: OYUN DURUMU KAYIT/YÜKLEME
        // =============================================

        app.MapPost("/api/game/state/save", async (GameStateSaveRequest request) =>
        {
            try
            {
                await repository.SaveGameStateAsync(request.SessionId, request.StateData);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { success = false, error = ex.Message });
            }
        });

        app.MapGet("/api/game/state/load", async (int sessionId) =>
        {
            try
            {
                var stateData = await repository.LoadGameStateAsync(sessionId);
                return Results.Ok(new { success = stateData != null, stateData = stateData ?? "" });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { success = false, stateData = "", error = ex.Message });
            }
        });

        // =============================================
        // YENİ: DİYALOG KAYDI (KATEGORİ İLE)
        // =============================================

        app.MapPost("/api/game/dialog/log", async (DialogLogRequest request) =>
        {
            try
            {
                await repository.LogDialogWithCategoryAsync(request.NpcId, request.PlayerQuestion, request.NpcResponse, request.Difficulty, request.Category);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { success = false, error = ex.Message });
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

