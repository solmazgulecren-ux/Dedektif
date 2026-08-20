using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DedektiflikRPG.Core.Interfaces;
using DedektiflikRPG.Data;
using DedektiflikRPG.Models;

namespace DedektiflikRPG.Services.AI;

/// <summary>
/// %100 Türkçeye Duyarlı, Gelişmiş Senaryo ve Suçlu Psikolojisi Yöneten Türkçe Yapay Zeka Motoru v4.0.
/// 
/// KURALLAR:
/// 1. Hiçbir NPC net olarak "Ben suçluyum" veya "Cinayeti ben işledim" demez.
/// 2. Hiçbir masum NPC kendisini suçlu göstermez; suçlandığında haklı tepki verir ve masumiyetini korur.
/// 3. Suçlu NPC doğrudan suçlandığında kıvırır, delil ister, panikler veya başkasını hedef gösterir.
/// 4. Devrik cümleleri ("sen misin katil", "kim sence katil", "o saatte ne işin vardı orada") başarıyla çözümler.
/// </summary>
public class LocalAiEngine : IAIService
{
    private static readonly Random _random = new Random();
    private static readonly CultureInfo _cultureTr = new CultureInfo("tr-TR");
    private readonly DatabaseRepository? _repository;

    public LocalAiEngine(DatabaseRepository? repository = null)
    {
        _repository = repository;
    }

    public async Task<AIInteractionResponse> GenerateResponseAsync(
        NPC npc,
        int guiltyNpcId,
        string userQuestion,
        IEnumerable<Clue>? cluesInBag = null,
        IEnumerable<DialogLog>? recentDialogs = null)
    {
        if (string.IsNullOrWhiteSpace(userQuestion))
        {
            return new AIInteractionResponse
            {
                Dialogue = "*Sessizce yüzünüzü süzüyor.* Söyleyecek bir şeyiniz yoksa zamanımı çalmayın amirim.",
                Emotion = "Sakin",
                TrustChange = 0
            };
        }

        // Türkçe Karakter Temizleme & Çift Katmanlı Normalizasyon
        string rawTrLower = userQuestion.ToLower(_cultureTr).Trim();
        string normalizedAscii = TurkishTextEngine.NormalizeToAscii(rawTrLower);

        bool isGuilty = (npc.NPCId == guiltyNpcId);
        var clues = cluesInBag?.ToList() ?? new List<Clue>();
        int npcCluesInBagCount = clues.Count(c => c.RelatedNPCId == npc.NPCId);
        var history = recentDialogs?.ToList() ?? new List<DialogLog>();

        // 0. BAĞLAM HAFIZASI (Context Memory)
        bool isAffirmative = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii, "evet", "tabi", "isim", "soyle", "istiyorum", "ver", "kim");
        if (isAffirmative && history.Any())
        {
            var lastResponse = history.First().NPCResponse.ToLower(_cultureTr);
            if (lastResponse.Contains("isim mi duymak"))
            {
                return new AIInteractionResponse
                {
                    Dialogue = GetRandomSuspectOpinion(npc.NPCId, guiltyNpcId),
                    Emotion = "Ciddi",
                    TrustChange = 2
                };
            }
        }

        // 1. Türkçe Anlamsal / Niyet Analizi (Intent & Concept Detection)
        string processedSentence = TurkishTextEngine.PreprocessSentence(normalizedAscii);

        // Selamlama / Nezaket Kontrolü (Orijinal Türkçe, Normalize ASCII ve Kök İşlenmiş Cümle Kontrolü)
        bool isGreeting = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii,
            "merhaba", "selam", "selamlar", "slm", "sa", "merhabalar", "gunaydin", "iyi gunler", "iyi aksamlar", "kolay gelsin", "nasilsin", "nasilsiniz", "hos bulduk", "tesekkur", "saol", "sagol", "hey", "meraba")
            || TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence,
            "merhaba", "selam", "gunaydin", "iyi gunler", "iyi aksamlar", "kolay gelsin", "nasilsin", "nasilsiniz", "hos bulduk", "tesekkur", "saol", "sagol");

        if (isGreeting)
        {
            int greetingCount = history.Count(h => TurkishTextEngine.ContainsAnyConcept(h.PlayerQuestion.ToLower(_cultureTr), TurkishTextEngine.NormalizeToAscii(h.PlayerQuestion), "selam", "merhaba", "gunaydin", "iyi gunler", "iyi aksamlar", "kolay gelsin"));

            string greetingResponse = (npc.NPCId, greetingCount % 3) switch
            {
                (1, 0) => "Aleykümselam amirim, dükkânıma hoş geldiniz. Buyurun, cinayet soruşturmasında size nasıl yardımcı olabilirim?",
                (1, 1) => "Tekrar selamlar dedektif. Kasap dükkanım açık, buyurun sorunuzu dinliyorum.",
                (1, _) => "Selamınızı aldım amirim, etleri doğrarken kulaklarım sizde. Ne sormak istiyorsanız çekinmeden sorun.",

                (2, 0) => "Merhaba amirim, şifa dükkânıma hoş geldiniz. İnşallah bu acı olayı kısa sürede aydınlatırsınız. Dinliyorum amirim.",
                (2, 1) => "Merhaba dedektif bey. Eczanede her şey emrinizde, nasıl yardımcı olabilirim?",
                (2, _) => "Size de merhaba amirim. İlaç şişelerini düzenliyorum, buyurun sizi dinliyorum.",

                (3, 0) => "Selamlar amirim, muhtarlık makamımıza safalar getirdiniz. Kasabamızın huzuru için ne gerekiyorsa sormaktan çekinmeyin.",
                (3, 1) => "Aleykümselam dedektif. Kasabanın muhtarı olarak her türlü sorunuza açığım.",
                (3, _) => "Yine merhaba amirim. Kasaba meydanındaki sükuneti sağlamaya çalışıyoruz, buyurun.",

                (4, 0) => "Merhaba amirim, kolay gelsin. Karakolumuz ve tüm imkânlarımız emrinizdedir, buyurun.",
                (4, 1) => "Selamlar meslektaşım. Karakol arşivimiz ve tüm tutanaklar hazır, ne öğrenmek istiyorsunuz?",
                (4, _) => "Merhaba amirim. Nöbetçi polislerimiz teyakkuzda, buyurun dinliyorum.",

                (5, 0) => "Hoş geldiniz amirim, sefalar getirdiniz. Şöyle oturun, bir sıcak çayımı için... Sorularınızı dinliyorum.",
                (5, 1) => "Aleykümselam amirim. Terzi dükkanımda çayım her zaman sıcaktır, buyurun.",
                (5, _) => "Merhaba evladım. İğne ipliği bıraktım, sizi dinliyorum.",

                // Gölge Şehir NPC'leri (101 - 108)
                (101, 0) => "Ormandan gelen taze çam kokusu gibisi yoktur amirim... Ama bu gece orman bir garip sesler çıkarıyordu. Buyurun, ne sormak istiyorsanız sorun.",
                (101, 1) => "Selam dedektif! Odunları yarmayı bitirdim, Ekrem Bey cinayeti hakkında ne bilmek istiyorsunuz?",
                (101, _) => "Aleykümselam amirim. Baltam tezgâhta durur, orman hakkında istediğinizi sorun.",

                (102, 0) => "Hoş geldiniz amirim, taze meyvelerim gibi temiz bir kasabayız aslında! Ekrem Bey vakası hepimizi sarstı, buyurun ne öğrenmek istersiniz?",
                (102, 1) => "Merhaba dedektif bey! Tezgâhın başındayım, kasabadaki dedikoduları mı yoksa cinayet gecesini mi soracaksınız?",
                (102, _) => "Selamlar amirim. Buyurun çekinmeyin, Manav Ayşe'ye her şeyi sorabilirsiniz.",

                (103, 0) => "Kızgın demir döverken laf dinlemek zordur amirim... Sorunuzu çabuk sorun, ocağın ateşi sönmesin.",
                (103, 1) => "Selam. Demir tavında dövülür dedektif, sorunuz neyse söyleyin çabucak.",
                (103, _) => "Aleykümselam. Çekiç sesinden rahatsız olmazsanız buyurun dinliyorum.",

                (104, 0) => "Aaa amirim hoş geldiniz sefalar getirdiniz! Gölge Şehir'in tüm havadisleri bakkaldan geçer, Ekrem Bey meselesini de dinleyin benden!",
                (104, 1) => "Merhaba dedektifim! Bakkal defterini kapattım, buyurun ne sormak istiyorsanız sorun.",
                (104, _) => "Selamlar amirim! Bakkal dükkanım emrinize amadedir, buyurun.",

                (105, 0) => "Hekimlik yeminim sır saklamayı gerektirir amirim... Ama bu cinayet kasabanın dengesini bozdu. Tıbbi veya şahsi ne sormak istersiniz?",
                (105, 1) => "Merhaba dedektif. Şifalı bitkilerimi ayıklıyordum, Ekrem Bey'in zehirlenme şüphesi mi var?",
                (105, _) => "Selam amirim. Tıbbi açıdan merak ettiğiniz bir konu varsa yanıtlayabilirim.",

                (106, 0) => "Gölge Şehir sakin bir yerdir dedektif bey. Bu talihsiz olayı çabuk çözüp kasabanın adını lekelemeden kapatmalıyız. Sorun bakalım.",
                (106, 1) => "Selamlar amirim. Muhtarlık evraklarını inceliyordum, kasabayla ilgili ne öğrenmek istersiniz?",
                (106, _) => "Merhaba dedektif. Kasaba halkının güvenliği için buradayım, buyurun.",

                (107, 0) => "Penceremin önünde oturup gaz lambasında kitap okurdum evladım... O gece ayak sesleri tam penceremin altından geçti. Dinliyorum sizi.",
                (107, 1) => "Merhaba evladım... Emekli bir muallim olarak kasabanın geçmişini iyi bilirim. Ne sormak istersin?",
                (107, _) => "Aleykümselam dedektif bey. Gözlüğümü taktım, buyurun sizi dinliyorum.",

                (108, 0) => "Ayakkabı çamurundan insanın nereye gittiğini anlarım amirim... O gece gelen çizmelerdeki çamur göl kenarındandı! Ne öğrenmek istiyorsunuz?",
                (108, 1) => "Selam dedektif. Köseleleri dikiyorum, çamurlu ayakkabı izlerini mi soracaksınız?",
                (108, _) => "Aleykümselam amirim. Kundura atölyemde sorularınızı dinliyorum.",

                _ => "Merhaba amirim, hoş geldiniz. Dinliyorum sizi."
            };

            return new AIInteractionResponse
            {
                Dialogue = greetingResponse,
                Emotion = "Sakin",
                TrustChange = 1,
                StressIncrease = 0
            };
        }

        // DEVRİK CÜMLE VE SORGULAMA KATEGORİLERİ
        bool isOpinionQuery = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence,
            "suphe", "sence", "baska", "biri", "kusku", "fikir", "dusun", "kim sence", "sence kim");

        bool isDirectAccusation = !isOpinionQuery && TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence,
            "sen yap", "sen oldur", "katil sen", "itiraf et", "suclu sen", "sucu sen",
            "kurban sen", "sen kiy", "cinayet sen", "kaza degil", "sen misin katil", "katil sen misin");

        bool isAlibiQuery = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence,
            "nere", "gece", "saat", "evde", "dukkan", "ne yap", "gor", "zaman", "olay", "ne isin vardi");

        bool isWeaponQuery = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence,
            "satir", "bicak", "zehir", "sise", "mektup", "gozluk", "kasa", "rozet", "dugme", "iplik",
            "kumas", "usb", "cep", "defter", "delil", "kanit", "esya", "kanli", "kirik", "bos", "yirtik", "silah");

        bool isMotiveQuery = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence,
            "borc", "para", "tapu", "arazi", "rusvet", "tehdit", "santaj", "kavga", "tartisma",
            "neden", "niye", "sebep", "nicin", "hakkinda", "iliski", "dusman", "husumet");

        bool mentionsHasan = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "hasan", "kasap");
        bool mentionsSelma = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "selma", "eczaci");
        bool mentionsKemal = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "kemal", "muhtar");
        bool mentionsGunes = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "gunes", "komiser", "polis");
        bool mentionsYahya = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "yahya", "terzi");

        // Gölge Şehir Şüpheli Algılama
        bool mentionsTahsin = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "tahsin", "oduncu");
        bool mentionsAyse = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "ayse", "manav");
        bool mentionsKazim = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "kazim", "demirci");
        bool mentionsNaciye = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "naciye", "bakkal");
        bool mentionsSevgi = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "sevgi", "hekim", "doktor");
        bool mentionsCevdet = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "cevdet", "muhtar");
        bool mentionsFehmi = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "fehmi", "muallim", "ogretmen");
        bool mentionsRasim = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "rasim", "kunduraci", "ayakkabici");
        bool mentionsEkrem = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, "ekrem", "tuccar", "kurban", "maktul");

        bool isLieAccusation = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence,
            "yalan", "celiski", "demin", "az once", "baska sey", "farkli soyledin", "inkar", "dogru soyle", "gercegi anlat");

        string currentIntent = "none";
        if (isDirectAccusation) currentIntent = "local_ai_accusation";
        else if (isLieAccusation) currentIntent = "local_ai_lie";
        else if (isMotiveQuery) currentIntent = "local_ai_motive";
        else if (isWeaponQuery) currentIntent = "local_ai_weapon";
        else if (isAlibiQuery) currentIntent = "local_ai_alibi";

        int annoyanceLevel = 0;
        if (currentIntent != "none" && history.Any())
        {
            int repeated = 0;
            foreach (var log in history)
            {
                string logText = log.PlayerQuestion.ToLower(_cultureTr);
                string logAscii = TurkishTextEngine.NormalizeToAscii(logText);

                bool logAcc = TurkishTextEngine.ContainsAnyConcept(logText, logAscii, "sen yaptin", "katil sensin", "itiraf et", "sen öldürdün", "sen misin katil");
                bool logAlibi = TurkishTextEngine.ContainsAnyConcept(logText, logAscii, "neredeydin", "o gece", "evde miydin", "ne yapiyordun");
                bool logWeapon = TurkishTextEngine.ContainsAnyConcept(logText, logAscii, "satir", "bicak", "zehir", "sise", "gozluk", "rozet", "iplik", "balta", "cizme", "defter", "delil");
                bool logMotive = TurkishTextEngine.ContainsAnyConcept(logText, logAscii, "borc", "para", "tapu", "tehdit", "husumet", "sebep", "santaj");

                string logIntent = "none";
                if (logAcc) logIntent = "local_ai_accusation";
                else if (logAlibi) logIntent = "local_ai_alibi";
                else if (logWeapon) logIntent = "local_ai_weapon";
                else if (logMotive) logIntent = "local_ai_motive";

                if (logIntent == currentIntent) repeated++;
            }
            annoyanceLevel = Math.Min(3, repeated);
        }

        string responseText = "";
        string emotion = "Sakin";
        int trustChange = 0;
        int stressIncrease = 0;
        string? revealedSecret = null;

        // 1.5 ÇELİŞKİ / YALAN YAKALAMA TEPKİSİ
        if (isLieAccusation)
        {
            if (isGuilty)
            {
                stressIncrease = 30;
                emotion = "Panik";
                trustChange = -5;
                responseText = (npc.NPCId >= 101)
                    ? "*Yüzü bembeyaz kesilir ve elleri titrer* B-ben öyle demek istemedim amirim! Olay gecesinin şokuyla dilim sürçtü sadece... Beni köşeye sıkıştırmaya çalışmayın!"
                    : "*Gözlerini kaçırarak kekeler* Ş-şey... hafızam beni yanıltıyor olabilir amirim! O gece çok karanlıktı, kafam karışıktı diyorum size!";
            }
            else
            {
                stressIncrease = 5;
                emotion = "Ciddi";
                responseText = (npc.NPCId >= 101)
                    ? "Benim sözümde çelişki falan yok amirim. Ne gördüysem, ne yaşadıysam onu söyledim. İfademin arkasındayım."
                    : "Ben ne söylediysem dürüstçe söyledim amirim. Lafımı çarpıtmayın lütfen, ben doğruyu konuşuyorum.";
            }

            return new AIInteractionResponse
            {
                Dialogue = responseText,
                Emotion = emotion,
                TrustChange = trustChange,
                StressIncrease = stressIncrease
            };
        }

        // 2. DOĞRUDAN SUÇLAMA TEPKİSİ (STRICT RULES: NO CONFESSION EVER, INNOCENT MAINTAINS INNOCENCE)
        if (isDirectAccusation)
        {
            if (isGuilty)
            {
                stressIncrease = 25;
                if (clues.Count >= 2 && annoyanceLevel >= 2)
                {
                    emotion = "Gergin";
                    responseText = GetGuiltyEvadedResponse(npc.NPCId);
                }
                else
                {
                    emotion = "Savunmacı";
                    responseText = GetGuiltyDefensiveResponse(npc.NPCId);
                }
            }
            else
            {
                stressIncrease = 10;
                emotion = "Öfkeli";
                responseText = GetInnocentAccusationResponse(npc.NPCId);
            }

            return new AIInteractionResponse
            {
                Dialogue = responseText,
                Emotion = emotion,
                TrustChange = isGuilty ? -5 : -3,
                StressIncrease = stressIncrease
            };
        }

        // 3. VERİTABANI DİYALOG POOL VE SEMANTİK EŞLEŞTİRME
        var dbPool = _repository != null ? (await _repository.GetLocalAIPoolAsync(npc.NPCId)).ToList() : new List<NPCDialogue>();

        if (annoyanceLevel >= 3)
        {
            emotion = "Sinirli";
            trustChange = -10;
            responseText = isGuilty
                ? "*Terler ve bağırır* Yeter amirim! Sürekli aynı ithamları tekrarlayıp duruyorsunuz! Kanıtınız varsa konuşun!"
                : "*Bıkkınlıkla* Size bunu daha önce defalarca söyledim. Masum bir insanı darlamayı bırakın amirim!";

            if (isGuilty && npcCluesInBagCount > 0 && revealedSecret == null)
            {
                revealedSecret = $"Amirims, Çetin olarak söylüyorum: {npc.Name} öfkesinden kontrolünü kaybetti ve şu bilgiyi ağzından kaçırdı: '{npc.SecretInfo}'";
            }
        }
        else if (npc.NPCId >= 101 && (mentionsTahsin || mentionsAyse || mentionsKazim || mentionsNaciye || mentionsSevgi || mentionsCevdet || mentionsFehmi || mentionsRasim || mentionsEkrem))
        {
            responseText = GetGolgeNpcOpinion(npc.NPCId, mentionsTahsin, mentionsAyse, mentionsKazim, mentionsNaciye, mentionsSevgi, mentionsCevdet, mentionsFehmi, mentionsRasim, mentionsEkrem, guiltyNpcId);
            emotion = "Düşünceli";
        }
        else if (mentionsHasan || mentionsSelma || mentionsKemal || mentionsGunes || mentionsYahya)
        {
            responseText = GetOtherNpcOpinion(npc.NPCId, mentionsHasan, mentionsSelma, mentionsKemal, mentionsGunes, mentionsYahya, guiltyNpcId);
            emotion = "Düşünceli";
        }
        else if (isOpinionQuery)
        {
            responseText = (npc.NPCId >= 101) 
                ? GetRandomGolgeSuspectOpinion(npc.NPCId, guiltyNpcId)
                : GetOtherNpcOpinion(npc.NPCId, false, false, false, false, false, guiltyNpcId);
            emotion = "Düşünceli";
        }
        else if (isAlibiQuery)
        {
            responseText = isGuilty ? GetGuiltyAlibiResponse(npc.NPCId) : GetInnocentAlibiResponse(npc.NPCId);
            emotion = isGuilty ? "Tedirgin" : "Sakin";
        }
        else if (isWeaponQuery)
        {
            responseText = isGuilty ? GetGuiltyWeaponResponse(npc.NPCId, rawTrLower) : GetInnocentWeaponResponse(npc.NPCId, rawTrLower);
            emotion = isGuilty ? "Gergin" : "Düşünceli";
        }
        else if (isMotiveQuery)
        {
            responseText = isGuilty ? GetGuiltyMotiveResponse(npc.NPCId) : GetInnocentMotiveResponse(npc.NPCId);
            emotion = isGuilty ? "Savunmacı" : "Sakin";
        }
        else if (dbPool.Any())
        {
            var usedResponses = history.Select(h => h.NPCResponse).ToHashSet();
            NPCDialogue? bestMatch = null;
            double highestScore = -1;

            foreach (var dialogue in dbPool)
            {
                bool isUsed = usedResponses.Contains(dialogue.NPCResponse);
                double score = CalculateSemanticScore(normalizedAscii, dialogue.PlayerText, dialogue.Category, currentIntent, npc.NPCId);

                if (isUsed) score -= 50;

                if (score > highestScore)
                {
                    highestScore = score;
                    bestMatch = dialogue;
                }
            }

            if (highestScore > 10 && bestMatch != null)
            {
                if (isGuilty && !string.IsNullOrEmpty(bestMatch.GuiltyResponses))
                {
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(bestMatch.GuiltyResponses);
                        if (dict != null && dict.TryGetValue(npc.NPCId.ToString(), out var gResp))
                        {
                            responseText = gResp;
                            emotion = "Tedirgin";
                        }
                    }
                    catch { responseText = bestMatch.NPCResponse; }
                }

                if (string.IsNullOrEmpty(responseText))
                {
                    responseText = bestMatch.NPCResponse;
                    emotion = "Sakin";
                }
            }
            else
            {
                responseText = GetDynamicFallback(rawTrLower, npc, isGuilty);
                emotion = "Düşünceli";
            }
        }
        else
        {
            responseText = GetGenericPersonaResponse(npc, isGuilty, userQuestion);
        }

        if (isGuilty && (rawTrLower.Contains("yalan") || rawTrLower.Contains("saklıyorsun") || rawTrLower.Contains("neden")))
        {
            stressIncrease += 15;
            emotion = "Gergin";
        }

        if (isGuilty && npcCluesInBagCount > 0 && _random.Next(100) < 40 && revealedSecret == null)
        {
            revealedSecret = $"Amirims, Çetin olarak söylüyorum: {npc.Name} konuşurken gözlerini kaçırıyor. Şu konuyla ilgisi var: '{npc.SecretInfo}'";
        }

        return new AIInteractionResponse
        {
            Dialogue = responseText,
            Emotion = emotion,
            TrustChange = trustChange,
            RevealedSecret = revealedSecret,
            StressIncrease = stressIncrease
        };
    }

    private double CalculateSemanticScore(string userQuestion, string playerText, string category, string currentIntent, int npcId)
    {
        double score = 0;
        if (category == currentIntent) score += 100;

        string processedUser = TurkishTextEngine.PreprocessSentence(userQuestion);
        string processedPlayer = TurkishTextEngine.PreprocessSentence(playerText);

        var userWords = processedUser.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var playerWords = processedPlayer.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var pWord in playerWords)
        {
            foreach (var uWord in userWords)
            {
                if (uWord == pWord) score += 20;
                else if (uWord.Contains(pWord) || pWord.Contains(uWord)) score += 10;
                else if (TurkishTextEngine.LevenshteinDistance(uWord, pWord) <= 1) score += 5;
            }
        }

        score += _random.NextDouble() * 5;
        return score;
    }

    private static string GetGuiltyEvadedResponse(int npcId)
    {
        return npcId switch
        {
            1 => "*Satırı tezgaha vurur* Sen ne diyorsun amirim?! O gece dükkândaydım diyorum! Kurbanla sorunumuz vardı ama katillik başka şey! Elinizde ne kanıt var ki beni suçluyorsunuz?!",
            2 => "*Gözlerini kaçırır* Ben bir eczacıyım amirim! Kurbanın ilacındaki sorunla benim ne ilgim olabilir? Başka birinin dükkânıma girip girmediğini araştırmalısınız!",
            3 => "*Masayı yumruklar* Beni katillikle mi itham ediyorsunuz?! Arazi anlaşmazlığımız vardı diye cinayeti bana yıkamazsınız! Kanıtınız yoksa muhtarlıktan çıkın!",
            4 => "*Resmi tavrını takınmaya çalışır ama sesi hafif titrer* Ben bu kasabanın komiseriyim amirim! Kanıtınız olmadan bir polise iftira atamazsınız!",
            5 => "*Gözlüklerini siler gibi yapar* Ben dikiş diken yaşlı bir terziyim amirim... Osman ile gizli işlerimiz vardı ama onu öldürmek benim harcım değil!",
            
            // Gölge Şehir Suçlu Kıvırma Tepkileri
            101 => "*Baltasını sıkar ve sertçe bakar* Ne diyorsun amirim sen?! Ormanda kaçak kereste kestim diye katil mi oldum? Elinizde kanıtınız yoksa baltamın yanından uzak durun!",
            102 => "*Neşesi birden kaybolur* Hahaha... amirim siz de şakacısınız! Borcum vardı diye adam mı öldürülür? Kasaların altına bakın, meyveden başka ne bulacaksınız?!",
            103 => "*Örsün üzerine çekici fırlatır* Ağzınızdan çıkanı kulağınız duysun amirim! Çelik kilit dövdüm diye katil mi ilan edildim? Kanıtınız varsa konuşun!",
            104 => "*Panikle ellerini ovuşturur* Aaa tövbeler olsun amirim! Bakkal Naciye bir karıncayı bile incitmez! Veresiye borcunu ödemedi diye fare zehriyle adam öldürülür mü hiç?!",
            105 => "*Sinir krizi eşiğinde titrer* Ben bir tıp adamıyım amirim! Banotu reçetelerim hastaları iyileştirmek içindir! Bana iftira atacağınıza sokaktaki serserileri arayın!",
            106 => "*Masasından kalkıp kurnazca gülümser* Sayın dedektif, makamıma saygısızlık ediyorsunuz. Sahte tapu iddialarınızla cinayeti bana yıkamazsınız!",
            107 => "*Köstekli saatini saklayıp hüzünle bakar* Ben 40 yıllık öğretmenim evladım... Saatimi geri istedim sadece, Ekrem'in ölümünü bana nasıl yakıştırırsınız?",
            108 => "*Çizme bıçağını tezgaha saplar* Huysuzum diye katil mi oldum amirim?! Çamurlu ayak izi kasabanın yarısında var! Kanıtınız yoksa dükkânımı terk edin!",
            
            _ => "Bu ithamı kesinlikle kabul etmiyorum amirim!"
        };
    }

    private static string GetGuiltyDefensiveResponse(int npcId)
    {
        return npcId switch
        {
            1 => "Ha! Kim söylemiş benim katil olduğumu?! Benim alacağım vardı Osman'dan, canlısı işime yarardı! Boş iddialarla dükkânımı meşgul etmeyin!",
            2 => "Bana katil demeden önce bir durun amirim! Elimde ne bir kanıt var ne bir şahit. Ben insan iyileştiririm, can almam!",
            3 => "Dedektif efendi, muhtarınızla doğru konuşun! Siyasi rakiplerimin uydurmasıyla karşıma çıkıp beni suçlayamazsınız!",
            4 => "Bu resmi bir soruşturma mı yoksa şahsi bir itham mı? Kanıtın varsa getir, yoksa karakolumdan dışarı çık!",
            5 => "Ceket dikmekten başka bir şey yapmadım ben. Yaşlı adama iftira atmak kolay tabii...",
            
            101 => "Oduncuyum ben, cinayetle işim olmaz amirim. Boş iddialarla beni oyalamayın!",
            102 => "Manav dükkânında katil aramak da yeni moda oldu galiba amirim!",
            103 => "Demir döverim, laf dövmem. Suçlamalarınız havada kalıyor.",
            104 => "Bakkal Naciye'yi tüm Gölge Şehir tanır! Bana katil demek büyük günahtır amirim!",
            105 => "Tıbbi teşhislerim kanuna uygundur. Boşuna şüphe üretmeyin!",
            106 => "Gölge Şehir Muhtarı olarak bu kasabada adaleti ben temsil ederim dedektif bey!",
            107 => "Yaşlı bir öğretmene iftira atmak hiç yakışmıyor amirim...",
            108 => "Kunduracı deriyi keser, canı değil! Lafınızı bilin de konuşun!",
            
            _ => "İddialarınız tamamen asılsız amirim!"
        };
    }

    private static string GetInnocentAccusationResponse(int npcId)
    {
        return npcId switch
        {
            1 => "Beni katillikle mi suçluyorsun amirim?! Saçmalama! Ben rızkında bir kasabım. Müşterimi niye öldüreyim? Alacağımı kim ödeyecek o zaman?!",
            2 => "Bana bu iftirayı atamazsınız! Yıllardır bu kasabada şifa dağıtıyorum. Katil arıyorsanız gidin Muhtarın kasasına bakın!",
            3 => "Haddinizi bilin amirim! Ben bu kasabanın seçilmiş muhtarıyım! Elinizde hiçbir kanıt yokken bana çamur atamazsınız!",
            4 => "Bir polise katil demek ağır bir iddiadır amirim! Kanıtın olmadan konuşma, resmi soruşturmayı engellemekten hakkında işlem yaparım!",
            5 => "Ben 70 yaşında dikiş diken bir adamım... Kıymayın bana amirim, günahımı almayın!",
            
            101 => "Ben masum bir oduncuyum amirim! Ekrem Bey'le aram iyi değildi ama ona kıymadım! Ormana gidin, asıl katil orada saklanıyor!",
            102 => "Bana iftira atmayın amirim! Ben dükkânımda neşeyle çalışan bir manavım. Gidin hekimin zehirli şişelerine bakın!",
            103 => "Masum insanlara çamur atmayın amirim. Örsümün başındaydım o gece. Katil arıyorsanız muhtarlığa gidin!",
            104 => "Aman amirim tövbe deyin! Bakkal adam katil olur mu? Kunduracının çizmelerine bakın, göl çamuru orada!",
            105 => "Bana bu hakareti edemezsiniz! Ben hayat kurtaran bir hekimim. Katil demircinin örsünden çıkan bıçağı kullandı!",
            106 => "Haddinizi aşmayın dedektif! Gölge Şehir halkının oylarıyla seçilmiş muhtara iftira atamazsınız!",
            107 => "Evladım ben emekli muallimim, tüm kasaba benim talebem sayılır. Bana bu kötülüğü nasıl kondurursunuz?",
            108 => "Yahu huysuzuz dediysek katil mi olduk?! Masum insanları darlamayı bırakın da gerçek katili bulun!",
            
            _ => "Masum insanlara çamur atmayı bırakın da gerçek faili bulun!"
        };
    }

    private static string GetGuiltyAlibiResponse(int npcId)
    {
        return npcId switch
        {
            1 => "Dükkândaydım diyorum! Et doğruyordum... *gözlerini kaçırır* Yağmur bardaktan boşalıyordu. Yani Osman'ın evine sadece borç konuşmaya gittim, o kadar!",
            2 => "Eczanede envanter sayıyordum. Dışarı çıkmadım... *titrer* Şey, gece yarısı sadece hava almak için Osman'ın sokağına doğru yürümüş olabilirim.",
            3 => "Evimdeydim, evrak inceliyordum! ...Gece saat 11 gibi yürüyüşe çıktım. Kurbanın evinin önünden geçtim ama içeri girmedim diyorum!",
            4 => "Karakoldaydım nöbette! ...Olay yerine ihbardan ÖNCE gittiğim yalan! Ben sadece devriye turundaydım!",
            5 => "Atölyemde dikiş dikiyordum. Makine sesi vardı... Bir anlığına sigara içmeye çıktım ama kurbanın evine kadar gitmedim!",
            
            101 => "Kulübede kereste istifliyordum... *terler* Sadece bir ara feneri alıp orman patikasına çıktım, Ekrem'in evine kadar gitmedim!",
            102 => "Dükkânı erken kapattım... *gözlerini kaçırır* Gece yarısı pelerinimi alıp sadece hava almak için göl kenarına yürümüştüm.",
            103 => "Ocakta demir dövüyordum... *sessizleşir* Gece yarısı fener sönünce dükkândan kısa süreliğine ayrıldım.",
            104 => "Erken uyudum amirim... *elleri titrer* Dükkânın arkasında lambayı yaktım ama sadece tütün sarıyordum!",
            105 => "Muayenehanede ilaç hazırlıyordum... *sinirle* Gece Ekrem'in sokağından geçmiş olabilirim ama sadece hastaya gidiyordum!",
            106 => "Ofiste tapu inceliyordum... *kurnazca* Gece 2 gibi kısa bir teftiş yürüyüşü yaptım, hepsi bu.",
            107 => "Penceremde kitap okuyordum... *hüzünle* Saat 02:14'te kapıya çıktım ama sadece temiz hava için.",
            108 => "Dükkânda çizme dikiyordum... *homurdanır* Çamurlu çizmeleri giyip dışarı çıktım ama göl kenarına uğramadım!",
            
            _ => "O gece kendi yerimdeydim."
        };
    }

    private static string GetInnocentAlibiResponse(int npcId)
    {
        return npcId switch
        {
            1 => "O gece dükkânımı geç kapattım, etleri soğuk hava deposuna yerleştiriyordum. Yağmurdak araba seslerini bile zor duydum.",
            2 => "Gece yarısına kadar dükkânım açıktı. Nöbetçi eczaneydim ama kimse gelmedi. Tezgah arkasında kitap okuyordum.",
            3 => "Muhtarlık binasındaydım, evrak işlerini yetiştirmeye çalışıyordum. Cinayet saatinde kasaba caddesi tamamen sakindi.",
            4 => "Devriye gezisindeydim amirim. Karakoldaki nöbetçi polis memurları da çıkış ve giriş saatimi onaylar.",
            5 => "Dükkânımda son diktiğim ceketin astarlarını dikiyordum. Yaşlı gözlerim yorulunca çay demleyip sokak lambasını izledim.",
            
            101 => "Orman kulübemdeydim amirim. Yağmur başlayınca keresteleri içeri taşıdım ve erkenden yattım.",
            102 => "Akşam üzeri manavı kapattım, komşum Naciye ile biraz laflayıp evime çekildim.",
            103 => "Demirci ocağını akşam söndürdüm, yorgunluktan erkenden uyuyakalmışım.",
            104 => "Bakkalın kepengini indirip evime geçtim. Gece bekçisi Rıfat amca da beni pencerede görmüştür.",
            105 => "Muayenehanemde tıp kitaplarımı inceliyordum. Gece boyunca kapım çalmadı.",
            106 => "Muhtarlık makamında köy meclisi kararlarını yazıyordum. Işığım gece boyu yanıktı.",
            107 => "Gaz lambasının ışığında polisiye romanımı okuyordum. 02:14'teki sesleri de o yüzden net duydum.",
            108 => "Kundura tezgâhımda sipariş çizmeleri kalıba alıyordum. Gece dışarı adım atmadım.",
            
            _ => "Kendi mekânımdaydım amirim."
        };
    }

    private static string GetGuiltyWeaponResponse(int npcId, string rawTrLower)
    {
        return npcId switch
        {
            1 => "*Tezgahtaki satıra bakıp terler* O satır... dükkânımdan çalınmıştı diyorum size! Birisi benim satırımı alıp Osman'a vurmuş, beni yakmak istiyorlar!",
            2 => "*Zehirli şişeyi görünce elleri titrer* O ilaç reçeteliydi! Şişenin boş olması kurbanın ilacı aşırı dozda içtiğini gösterir, benim suçum ne?!",
            3 => "*Gözlük ve tapuları görünce kızarır* Sahte tapular bir projedir! Kırık gözlük ise kurban bana saldırınca düştü!",
            4 => "*Kopan polis rozetine bakar* O rozet karakoldan çalınmıştı! Olay yerine ben düşürmedim, beni tuzağa düşürüyorlar!",
            5 => "*İplik makarasını cebine saklar* O iplik sağlamdır evet... Ben terziyim amirim, dükkânımda bin tane makara var!",
            
            101 => "*Baltasını arkasına saklar* Baltamdaki lekeler çam reçinesidir amirim! Kanla reçineyi ayırt edemiyor musunuz?!",
            102 => "*Pelerin parçasını görünce yutkunur* O kumaş her tezgâhta var! Pelerinimi çiviler yırttı, olay yeriyle ilgisi yok!",
            103 => "*Örsteki kilit ve bıçağa bakar* O bıçak benim örsümden çıktı ama ben satmadım! Çalınmış olabilir!",
            104 => "*Veresiye defterini kapatır* Yırtık sayfada sadece borç notları vardı! Zehir formülü falan yoktu!",
            105 => "*Mor şişeyi titreyen ellerle tutar* Bu şişedeki banotu özü tıbbi deneyler içindi! Kurbana ben içirmedim!",
            106 => "*Sahte tapuları masanın altına iter* Bu belgeler resmi taslaklardı! Kırık altın gözlük ise bana hediye gelmişti!",
            107 => "*Köstekli saate bakar* Saat babamın emanetiydi... Olay yerinde düştüyse Ekrem çalmıştı demektir!",
            108 => "*Mumlu ipi çeker* Bu ip sadece taban dikmek içindir! Boğulma izleriyle eşleşmesi tamamen tesadüf!",
            
            _ => "O bahsettiğiniz nesneyle benim ilgim yok!"
        };
    }

    private static string GetInnocentWeaponResponse(int npcId, string rawTrLower)
    {
        return npcId switch
        {
            1 => "O delil şüpheli görünüyor ama benim dükkânımla ilgisi yok. Kasabada herkes et yer, herkes bıçak kullanır.",
            2 => "Tıbbi malzemeler ve bitkiler uzmanlık alanımdır. Eğer zehirlenme varsa kurbanın ne içtiğini adli tıp raporu açıklar.",
            3 => "Resmi belgeler ve tapular belediye arşivindedir. Delil dedikleriniz sahtekârların işi olabilir.",
            4 => "Polis delil toplar, karartmaz. O nesne adli laboratuvara gönderilmeli.",
            5 => "O dikiş malzemesi her terzinin tezgahında bulunur. Önemli olan o malzemeyi kimin kullandığıdır.",
            
            101 => "Ormandaki balta ve aletler iş gereğidir. Olay yerindeki delilleri adli laboratuvarda inceleyin amirim.",
            102 => "Meyve kasaları ve kantar normal dükkân eşyası. Şüpheli bir şey varsa araştırın tabii.",
            103 => "Örsümdeki ay damgasını herkes bilir. Bıçak yaparım ama kime satıldığını defterim yazar.",
            104 => "Veresiye defterim herkese açıktır. Delil arıyorsanız dükkânımı didik didik edebilirsiniz.",
            105 => "Banotu zehirli bir bitkidir evet, ama ben hekimim. Zehirle cinayet işleyecek kadar gözü dönmüş biri değilim.",
            106 => "Muhtarlık mührü resmi evraklara basılır. Sahte evrak varsa arkasındaki çeteyi ortaya çıkarın.",
            107 => "Köstekli saatim eski bir antika. Cinayet aletiyle uzaktan yakından ilgisi olamaz evladım.",
            108 => "Mumlu ip ve kundura kalıbı her ayakkabıcıda bulunur. Katili kalıbın numarasından bulabilirsiniz.",
            
            _ => "Bu delili dikkatle incelemenizi tavsiye ederim amirim."
        };
    }

    private static string GetGuiltyMotiveResponse(int npcId)
    {
        return npcId switch
        {
            1 => "Osman bana 50.000 TL borçluydu! Yıllardır emeğimi sömürdü! 'Yarın öderim' deyip dalga geçti! Hangi insan dayanabilir buna?!",
            2 => "Osman beni geçmişimle tehdit ediyordu! Her ay benden şantajla para alıyordu... Artık dayanacak gücüm kalmamıştı!",
            3 => "O arsa belediyenin geleceğiydi! Osman bencillik yapıp vermiyordu. Kasabanın kalkınmasını engelliyordu!",
            4 => "Beni rüşvet almakla suçlayıp savcılığa gidecekti. Şantaj yapıyordu bana! 15 yıllık şerefimi karartacaktı!",
            5 => "O gizli cebe koyduğu USB bellekte tüm ortaklık sırları vardı. Osman beni saf dışı bırakıp servetime el koyacaktı!",
            
            101 => "Ekrem ormandaki kaçak kereste işimi öğrendi! Beni ihbar etmekle tehdit edip haraç istiyordu!",
            102 => "Tüm dükkânımı borç karşılığı elimden alacaktı! Çocuklarımın rızkını o tefeciye yediremezdim!",
            103 => "Gizli çelik kasanın sırrını bana yıktı! İşlediği kaçakçılığın faturasını benim ocağıma çıkaracaktı!",
            104 => "Veresiye borcu 5 bin lirayı bulmuştu! 'Dükkânını yakarım' diye tehdit etti beni!",
            105 => "Geçmişteki tıbbi hatamı kullanarak beni şantajla zehir üretmeye zorluyordu! Dayanamadım artık!",
            106 => "Çam ormanı arazisini ucuza kapatıp beni makamımdan edecekti! Gölge Şehir'in geleceğini ona bırakamazdım!",
            107 => "Babamdan kalan tek hatırayı, o altın köstekli saati zorla elimden aldı! Gururumla oynadı!",
            108 => "Kaçak deri sevkiyatımı öğrenip beni polise vermekle tehdit etti! Yaşlı kunduracıyı köle yapacaktı!",
            
            _ => "Herkesin kendine göre nedenleri vardır."
        };
    }

    private static string GetInnocentMotiveResponse(int npcId)
    {
        return npcId switch
        {
            1 => "Aramızda ticaret vardı, veresiye borcu vardı evet. Ama borçlu adam öldürülür mü amirim? Öldürürsem param hepten batar!",
            2 => "Osman Bey müşterimdi. Aramızda husumet yoktu. Sağlık sorunları dışında kendisiyle özel bir diyalogum olmadı.",
            3 => "Siyasette herkesle anlaşamazsınız. Osman'la fikir ayrılıklarımız oldu ama ben kanunlara inanan bir muhtarım.",
            4 => "Polis ile vatandaş arasındaki ilişki neyse bizimki de oydu. Görevimi yaptım, husumetim yoktu.",
            5 => "Osman iyi bir müşterimdi. Diktirdiğim kıyafetlerin parasını zamanında öderdi. Neden ona kıyayım?",
            
            101 => "Ekrem Bey kereste alırdı benden. Fiyat konusunda pazarlık ederdik ama cinayet sebebi olacak bir husumetimiz yoktu.",
            102 => "Meyve sebze alırdı, parasını da gecikmeli olsa öderdi. Müşterimi neden öldüreyim amirim?",
            103 => "Kasa kilidi siparişi vermişti, parasını da peşin ödedi. Aramızda husumet yoktu.",
            104 => "Veresiye yazdırırdı ama zengin adamdı, eninde sonunda kapatırdı. Husumetim yoktu.",
            105 => "Hastamdı, kalp ilacı yazardım. Hekim hastasına düşmanlık beslemez amirim.",
            106 => "Kasabanın önde gelen tüccarıydı. Fikir ayrılıklarımız oldu ama hepsi resmi çerçevedeydi.",
            107 => "Kitap koleksiyoncusuydu, eski romanları tartışırdık. Kültürlü bir adamdı, aramız iyiydi.",
            108 => "Çizmelerini bana tamir ettirirdi. Huysuzluğum ona özel değildi, herkese karşı böyleyim.",
            
            _ => "Benim kimseyle husumetim yok amirim."
        };
    }

    private static string GetOtherNpcOpinion(int currentNpcId, bool hasan, bool selma, bool kemal, bool gunes, bool yahya, int guiltyId)
    {
        if (hasan && currentNpcId != 1)
            return "Kasap Hasan öfkeli bir adamdır. O gece dükkânında ışık yanıyordu. Öfkesine yenik düşüp satıra sarılmış olabilir.";
        if (selma && currentNpcId != 2)
            return "Eczacı Selma çok sessizdir ama sessiz sudan korkacaksın. Tezgah altında zehirli sarmaşıklar yetiştirdiğini duymuştum.";
        if (kemal && currentNpcId != 3)
            return "Muhtar Kemal kasabayı parmağında oynatır. Osman ile arazi tapuları yüzünden şiddetli kavgaya tutuştuklarını biliyorum.";
        if (gunes && currentNpcId != 4)
            return "Komiser Güneş... Olay yerini çabucak kapatmaya çalıştı sanki. Karakoldaki gizli dosyada bir şeyler saklıyor.";
        if (yahya && currentNpcId != 5)
            return "Terzi Yahya yaşlı görünür ama Osman'la gizli işler çevirirdi. Ceketlerin astarına gizli cep dikerdi.";

        return "Bu kasabada herkes bir şeyler gizliyor amirim. Kimseye gözü kapalı güvenmeyin.";
    }

    private static string GetGolgeNpcOpinion(int currentNpcId, bool tahsin, bool ayse, bool kazim, bool naciye, bool sevgi, bool cevdet, bool fehmi, bool rasim, bool ekrem, int guiltyId)
    {
        if (tahsin && currentNpcId != 101)
            return "Oduncu Tahsin ormanın derinliklerinde kaçak işler çevirirdi. Ekrem Bey ile kereste konusunda şiddetli kavga ettiklerini duydum.";
        if (ayse && currentNpcId != 102)
            return "Manav Ayşe'nin dükkânı Ekrem'e ipotekliydi. Cinayet gecesi peleriniyle telaş içinde koştuğunu görenler olmuş.";
        if (kazim && currentNpcId != 103)
            return "Demirci Kazım çok az konuşur ama Ekrem için özel şifreli çelik kasa kilidi dövmüştü. Ocağın arkasında sırlar saklıyor olabilir.";
        if (naciye && currentNpcId != 104)
            return "Bakkal Naciye kasabadaki tüm borç ve para trafiğini bilir. Ekrem'in veresiye sayfasını yırttığı söyleniyor.";
        if (sevgi && currentNpcId != 105)
            return "Hekim Sevgi şifalı otlar hazırlar ama banotu gibi ölümcül zehirleri de çok iyi bilir. Ekrem'in vücudundaki lekeler şüpheli.";
        if (cevdet && currentNpcId != 106)
            return "Muhtar Cevdet çam ormanı arazisini ele geçirmek için sahte tapu düzenletmişti. Ekrem bunu ifşa etmekle tehdit ediyordu.";
        if (fehmi && currentNpcId != 107)
            return "Fehmi Bey emekli muallimdir, Ekrem onun babasından kalan değerli köstekli saatini gasp etmişti. O gece penceresinden sesler duymuş.";
        if (rasim && currentNpcId != 108)
            return "Kunduracı Rasim kaçak deri işinde Ekrem'e borçluydu. Göl kenarındaki 42 numara çamurlu çizme izleri doğrudan onun atölyesine çıkıyor.";
        if (ekrem)
            return "Tüccar Ekrem Bey kasabanın en zenginiydi ama herkesi borçla ve şantajla köşeye sıkıştırırdı. Sonunda birinin canına tak etti...";

        return "Gölge Şehir'de herkesin Ekrem Bey ile karanlık bir hesabı vardı amirim. Kimseye gözü kapalı güvenmeyin.";
    }

    private static string GetRandomGolgeSuspectOpinion(int currentNpcId, int guiltyId)
    {
        var possibleIds = Enumerable.Range(101, 8).Where(id => id != currentNpcId).ToList();
        int targetId = possibleIds[_random.Next(possibleIds.Count)];

        return targetId switch
        {
            101 => "Oduncu Tahsin'in baltasındaki reçine ve koyu lekelere dikkat edin amirim. Ormanda kaçak işler çevirirdi.",
            102 => "Manav Ayşe dükkânını Ekrem'e kaptırmamak için her şeyi yapabilirdi. Pelerinli hali o gece sokaktaydı.",
            103 => "Demirci Kazım usta Ekrem'in gizli kasasının kilidini yapan adamdır. Ocağının arkasını iyi araştırın.",
            104 => "Bakkal Naciye'nin veresiye defterindeki yırtık sayfaya ve sakladığı fare zehirlerine bakın amirim.",
            105 => "Hekim Sevgi'nin serasındaki mor banotu zehirlerini inceleyin. Ekrem'in tırnaklarındaki morluklar tesadüf değil.",
            106 => "Muhtar Cevdet'in kasasındaki sahte çam ormanı tapusuna ve resmi mühürlü tehdit mektubuna bakın.",
            107 => "Fehmi Bey'in durmuş köstekli saatini ve o gece 02:14'te duyduğu sesleri sorgulayın.",
            108 => "Kunduracı Rasim'in atölyesindeki mumlu ayakkabı iplerini ve 42 numara çamurlu çizmelerini kontrol edin.",
            _ => "Gölge Şehir'de 8 şüphelinin her biri potansiyel katildir amirim. İpuçlarını birleştirin."
        };
    }

    private static string GetRandomSuspectOpinion(int currentNpcId, int guiltyId)
    {
        var possibleIds = Enumerable.Range(1, 5).Where(id => id != currentNpcId).ToList();
        int targetId = possibleIds[_random.Next(possibleIds.Count)];

        return targetId switch
        {
            1 => "Kasap Hasan'a dikkat et amirim. O satırı sadece et kesmek için kullanmıyor. Öfke kontrolü sıfırdır.",
            2 => "Eczacı Selma'nın tezgahının altındaki şişeleri incelediniz mi? Çok sessiz bir kadındır ama sessiz sudan korkacaksın.",
            3 => "Muhtar Kemal... Kasabada her taşın altından o çıkar. Siyasi gücünü kullanarak herkesi eziyor.",
            4 => "Komiser Güneş'in üniformasına güvenmeyin amirim. Kendi karakolunda karanlık işler çeviriyor.",
            5 => "Terzi Yahya... İhtiyar göründüğüne bakmayın, o terzi dükkanı kasabanın tüm dedikodularının merkezidir.",
            _ => "Herkes şüpheli amirim, gözünüzü açık tutun."
        };
    }

    private static string GetDynamicFallback(string rawTrLower, NPC npc, bool isGuilty)
    {
        bool isGolge = npc.NPCId >= 100;
        string victimName = isGolge ? "Ekrem Bey" : "Osman Bey";

        if (rawTrLower.Contains("kim")) return $"Kimin yaptığını soruyorsanız amirim, {npc.Name} olarak bu kasabada herkesin karanlık bir hesabı olduğunu söyleyebilirim. Şüphelendiğiniz özel bir isim mi var?";
        if (rawTrLower.Contains("nerede") || rawTrLower.Contains("nerde")) return "Olay yerini veya cinayet saatini soruyorsanız, o gece yağmur ve sis her yeri kaplamıştı. Çamurlu ayak izlerini ve dükkânları incelemelisiniz.";
        if (rawTrLower.Contains("neden") || rawTrLower.Contains("niye") || rawTrLower.Contains("sebep") || rawTrLower.Contains("motif")) return $"{victimName}'in arkasında bıraktığı borçlar, sahte tapular ve tehditler herkesi şüpheli yapıyor amirim. Cinayetin sebebi bu sırlar olabilir.";
        if (rawTrLower.Contains("nasil")) return "Nasıl öldürüldüğünü öğrenmek için olay yerindeki delilleri Adli Tıbba göndermeli ve otopsi raporunu beklemelisiniz amirim.";
        if (rawTrLower.Contains("saat") || rawTrLower.Contains("zaman")) return isGolge ? "Cinayet gecesi saat 02:00 ile 02:30 arasında çam ormanı girişinde ve sokaklarda hareketlilik vardı. Fehmi Bey'in penceresinden gelen seslere bakın." : "Cinayet gecesi saat 23:00 ile 00:30 arasında meydanda ve dükkânların önünde hareketlilik vardı.";

        return npc.NPCId switch
        {
            1 => "*Satırını bilerken size ters bir bakış atar* O gece ben dükkanımı temizliyordum amirim. Kimin ne halt ettiği beni ilgilendirmez.",
            2 => "*Gözlüğünü düzelterek kısık sesle konuşur* Osman Bey'in kalbi hastaydı, bunu herkes biliyor... Ama sırlar bazen ölümcüldür amirim.",
            3 => "*Makam koltuğunda geriye yaslanır* Ben bu kasabanın muhtarıyım dedektif! Kanunsuz iş yapan kimse, onu bulmak sizin göreviniz.",
            4 => "*Polis rozetini parlatır* Karakolumuzda faili meçhul cinayetlere yer yoktur amirim. Kendi teşkilatınız kadar bize de güvenmelisiniz.",
            5 => "*Ceket astarlarını dikmeye devam eder* Ah amirim... Kimin nerede ne kadar kumaşı yırtıldığını sadece biz terziler ve Allah bilir.",
            101 => "*Baltasını tezgaha dayar* Ormanda o gece fırtına vardı amirim... Sorduğunuz konuda bildiğim tek şey, o gece göl kenarından aceleyle geçen biri olduğudur.",
            102 => "*Tezgahı silerek etrafına bakınır* Ekrem Bey ile olan borç meselemi kurcalamayın amirim... Ama o gece Demirci ile Muhtar'ın tartıştığını duydum!",
            103 => "*Çekici örse vurur* Soruşturmanızı anlıyorum amirim. Ekrem Bey için özel çelik kilit yapmıştım, o kilit açılmışsa katil anahtarı olan biridir!",
            104 => "*Veresiye defterini kapatır* Bakkal Naciye her şeyi duyar derler ama o gece kasaba mezarlık gibi sessizdi... Tek bildiğim Hekim'in gece yarısı dışarıda olduğudur.",
            105 => "*İlaç şişelerini düzenler* Tıbbi açıdan Ekrem Bey'in zehirlenmiş olabileceğini düşünüyorsanız, tırnaklarındaki morluklara ve Adli Tıp raporuna odaklanın.",
            106 => "*Resmi evrakları toplar* Muhtarlık olarak kasabanın huzurunu sağlamak görevim. Şüphelendiğiniz bir delil varsa Adli Tıbba gönderin dedektif bey.",
            107 => "*Köstekli saatini kontrol eder* Evladım, yaşlı bir muallim olarak şunu bilirim: O gece tam 02:14'te penceremin altından sert çizmeli biri geçti.",
            108 => "*Deri bıçağını bırakır* Çamurlu çizmeleri soruyorsanız kasabanın yarısı göl kenarındaydı. Gidin asıl delillere ve ayak numaralarına bakın!",
            _ => "*Gözlerini kısarak sizi süzer* Sorduğunuz konuyu tam olarak açarsanız, soruşturmanıza daha net bir cevap verebilirim amirim."
        };
    }

    private static string GetGenericPersonaResponse(NPC npc, bool isGuilty, string originalQuestion)
    {
        return GetDynamicFallback(originalQuestion.ToLower(_cultureTr), npc, isGuilty);
    }
}
