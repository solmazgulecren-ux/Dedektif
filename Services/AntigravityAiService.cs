using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DedektiflikRPG.Models;

namespace DedektiflikRPG.Services;

/// <summary>
/// Gemini AI API ile iletişim kuran servis sınıfı.
/// NPC'nin ruh hali ve oyuncu verilerinden dinamik prompt oluşturur,
/// API'den gelen yanıtı parse eder.
/// </summary>
public class AntigravityAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;

    public AntigravityAiService(string apiKey, string modelName = "gemini-2.0-flash")
    {
        _apiKey = apiKey;
        _modelName = modelName;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// NPC bağlamı ve oyuncunun sorusuna göre AI'dan cevap üretir.
    /// </summary>
    public async Task<AIInteractionResponse> GetNPCResponseAsync(
        NPC npc,
        string playerQuestion,
        IEnumerable<Clue> playerClues)
    {
        var systemPrompt = BuildSystemPrompt(npc, playerClues);
        var response = await CallGeminiApiAsync(systemPrompt, playerQuestion);
        return response;
    }

    /// <summary>
    /// NPC bilgilerinden dinamik bir system prompt oluşturur.
    /// </summary>
    private string BuildSystemPrompt(NPC npc, IEnumerable<Clue> playerClues)
    {
        var clueList = string.Join("\n", playerClues.Select(c => $"  - {c.Title}: {c.Description}"));
        if (string.IsNullOrEmpty(clueList))
            clueList = "  (Henüz ipucu yok)";

        var guiltStatus = npc.IsGuilty ? "SUÇLU (bunu kesinlikle gizlemeye çalış)" : "MASUM (ama yine de şüpheli davranabilir)";

        return $@"Sen bir dedektiflik RPG oyunundaki NPC karakterisin. Aşağıdaki kurallara uymalısın:

KARAKTERİN:
- İsim: {npc.Name}
- Rol: {npc.Role}
- Güven Seviyesi: {npc.TrustLevel}/100 (düşükse temkinli ve kısa cevaplar ver, yüksekse daha açık ol)
- Korku Seviyesi: {npc.FearLevel}/100 (yüksekse tedirgin, gergin ve bazen tutarsız davran)
- Suçluluk Durumu: {guiltStatus}
- Sakladığın Sır: {npc.SecretInfo}

OYUNCUNUN ELİNDEKİ İPUÇLARI:
{clueList}

DAVRANIŞKURALLARI:
1. Güven seviyesi 70'in üstündeyse daha samimi ve detaylı cevaplar ver, sırra yaklaşan ipuçları verebilirsin.
2. Güven seviyesi 30'un altındaysa çok temkinli ol, kısa ve kaçamak cevaplar ver.
3. Korku seviyesi yüksekse gergin, tedirgin ve bazen çelişkili davran.
4. Eğer suçluysan, yalan söyleyebilirsin ama baskı altında tutarsızlıklar göster.
5. Oyuncu seni sıkıştırırsa duygu durumun değişsin (sinirli, korkmuş, pişman vb.).
6. Karakter rolüne uygun konuş (kasap halk ağzıyla, eczacı bilimsel, muhtar politik).

YANITINI MUTLAKA SADECE AŞAĞIDAKI JSON FORMATINDA VER, BAŞKA HİÇBİR ŞEY YAZMA:
{{
  ""dialogue"": ""NPC'nin oyuncuya verdiği cevap"",
  ""emotion"": ""şu anki duygu durumu (sinirli/korkmuş/sakin/tedirgin/samimi/pişman/saldırgan)"",
  ""trustChange"": 0,
  ""revealedSecret"": null
}}

trustChange değerleri: -10 ile +10 arasında bir tam sayı olmalı.
revealedSecret: Eğer güven yüksekse ve oyuncu doğru soruları soruyorsa sırrın bir kısmını açığa çıkarabilirsin, yoksa null yaz.";
    }

    /// <summary>
    /// Gemini API'ye HTTP POST isteği atar ve yanıtı parse eder.
    /// </summary>
    private async Task<AIInteractionResponse> CallGeminiApiAsync(string systemPrompt, string userMessage)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                }
            },
            generationConfig = new
            {
                temperature = 0.9,
                maxOutputTokens = 500,
                responseMimeType = "application/json"
            }
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new AIInteractionResponse
                {
                    Dialogue = $"[AI Bağlantı Hatası: {response.StatusCode}] NPC şu an konuşamıyor...",
                    Emotion = "sessiz",
                    TrustChange = 0,
                    RevealedSecret = null
                };
            }

            return ParseGeminiResponse(responseBody);
        }
        catch (TaskCanceledException)
        {
            return new AIInteractionResponse
            {
                Dialogue = "[Zaman Aşımı] NPC düşüncelerini toparlıyor... Tekrar deneyin.",
                Emotion = "dalgın",
                TrustChange = 0,
                RevealedSecret = null
            };
        }
        catch (Exception ex)
        {
            return new AIInteractionResponse
            {
                Dialogue = $"[Hata] Bir sorun oluştu: {ex.Message}",
                Emotion = "sessiz",
                TrustChange = 0,
                RevealedSecret = null
            };
        }
    }

    /// <summary>
    /// Gemini API'nin JSON yanıtını AIInteractionResponse'a dönüştürür.
    /// </summary>
    private AIInteractionResponse ParseGeminiResponse(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Gemini yanıt formatı: candidates[0].content.parts[0].text
            var text = root
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrEmpty(text))
            {
                return FallbackResponse("AI boş yanıt döndü.");
            }

            // JSON olarak parse et
            var aiResponse = JsonSerializer.Deserialize<AIInteractionResponse>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return aiResponse ?? FallbackResponse("AI yanıtı parse edilemedi.");
        }
        catch (Exception ex)
        {
            return FallbackResponse($"Yanıt parse hatası: {ex.Message}");
        }
    }

    private AIInteractionResponse FallbackResponse(string reason)
    {
        return new AIInteractionResponse
        {
            Dialogue = $"*{reason}* NPC sessizce sizi süzüyor...",
            Emotion = "belirsiz",
            TrustChange = 0,
            RevealedSecret = null
        };
    }
}
