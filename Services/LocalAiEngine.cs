using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DedektiflikRPG.Models;
using DedektiflikRPG.Data;

namespace DedektiflikRPG.Services;

/// <summary>
/// %100 Türkçeye Duyarlı, Gelişmiş Senaryo ve Suçlu Psikolojisi Yöneten Türkçe Yapay Zeka Motoru v3.0.
/// 
/// ÖZELLİKLER:
/// 1. Türkçedeki tüm özel harfleri (ş, ç, ı, ü, ö, ğ, İ, Ş, Ç, Ü, Ö, Ğ) hem orijinal hem esnek haliyle tam işler.
/// 2. Levenshtein Distance & Fuzzy Match algoritması ile yazım hatalarını kompanse eder.
/// 3. Doğrudan suçlamalarda katil bile olsa hemen itiraf etmez; aşamalı baskı ve delil mekanizması kullanır.
/// 4. Çetin'in yapay zeka olduğunu belli etmeden dedektif sezgileriyle amirine bilgi sunmasını sağlar.
/// 5. Her NPC için masum ve suçlu hallerinde zengin Türkçe replik şablonları barındırır.
/// </summary>
public class LocalAiEngine
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

        // 0. CONTEXT MEMORY (Bağlam Hafızası)
        // Eğer bir önceki cevapta "isim mi duymak istiyorsunuz?" sorulmuşsa ve oyuncu evet/isim dediyse
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
        // Yeni NLP motoru: Cümleyi köklerine ayır ve sokak ağzını düzelt
        string processedSentence = TurkishTextEngine.PreprocessSentence(normalizedAscii);

        // Neden / Selamlama / Nezaket Kontrolü
        bool isGreeting = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, 
            "merhaba", "selam", "gunaydin", "iyi gunler", "iyi aksamlar", "kolay gelsin", "nasilsin", "nasilsiniz", "hos bulduk", "tesekkur", "saol", "sagol");

        if (isGreeting)
        {
            string greetingResponse = npc.NPCId switch
            {
                1 => "Aleykümselam amirim, dükkânıma hoş geldiniz. Buyurun, cinayet soruşturmasında size nasıl yardımcı olabilirim?",
                2 => "Merhaba amirim, şifa dükkânıma hoş geldiniz. İnşallah bu acı olayı kısa sürede aydınlatırsınız. Dinliyorum amirim.",
                3 => "Selamlar amirim, muhtarlık makamımıza safalar getirdiniz. Kasabamızın huzuru için ne gerekiyorsa sormaktan çekinmeyin.",
                4 => "Merhaba amirim, kolay gelsin. Karakolumuz ve tüm imkânlarımız emrinizdedir, buyurun.",
                5 => "Hoş geldiniz amirim, sefalar getirdiniz. Şöyle oturun, bir sıcak çayımı için... Sorularınızı dinliyorum.",
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

        bool isDirectAccusation = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, 
            "sen yap", "sen oldur", "katil sen", "itiraf et", "suclu sen", "sucu sen", 
            "kurban sen", "sen kiy", "cinayet sen", "kaza degil", "katil kim");

        bool isAlibiQuery = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence,
            "nere", "gece", "saat", "evde", "dukkan", "ne yap", "gor", "zaman", "olay");

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

        bool isOpinionQuery = TurkishTextEngine.ContainsAnyConcept(rawTrLower, processedSentence, 
            "suphe", "sence", "baska", "biri", "kusku", "fikir", "dusun");

        // 2. Determine Intent Category & Calculate Annoyance Level based on history
        string currentIntent = "none";
        
        // Priority: Accusation > Motive > Weapon > Alibi (Motive takes precedence over random weapon fuzzy matches)
        if (isDirectAccusation) currentIntent = "local_ai_accusation";
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
                
                bool logAcc = TurkishTextEngine.ContainsAnyConcept(logText, logAscii, "sen yaptin", "katil sensin", "itiraf et", "sen öldürdün");
                bool logAlibi = TurkishTextEngine.ContainsAnyConcept(logText, logAscii, "neredeydin", "o gece", "evde miydin", "ne yapiyordun");
                bool logWeapon = TurkishTextEngine.ContainsAnyConcept(logText, logAscii, "satir", "bicak", "zehir", "sise", "gozluk", "rozet", "iplik", "delil");
                bool logMotive = TurkishTextEngine.ContainsAnyConcept(logText, logAscii, "borc", "para", "tapu", "tehdit", "husumet", "sebep");
                
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

        // 3. Fetch dynamic dialogues from database
        var dbPool = _repository != null ? (await _repository.GetLocalAIPoolAsync(npc.NPCId)).ToList() : new List<NPCDialogue>();
        
        if (annoyanceLevel >= 3)
        {
            emotion = "Sinirli";
            trustChange = -10;
            responseText = isGuilty 
                ? "*Bağırarak* Yeter artık amirim! Aynı şeyi sorup duruyorsunuz! Beni rahat bırakın!" 
                : "*Bıkkınlıkla* Size bunu daha önce defalarca söyledim. Lütfen aynı soruları tekrarlamayın!";
            
            if (isGuilty && npcCluesInBagCount > 0 && revealedSecret == null)
            {
                revealedSecret = $"Amirims, Çetin olarak söylüyorum: {npc.Name} öfkesinden kontrolünü kaybetti ve şu bilgiyi ağzından kaçırdı: '{npc.SecretInfo}'";
            }
        }
        else if (mentionsHasan || mentionsSelma || mentionsKemal || mentionsGunes || mentionsYahya)
        {
            responseText = GetOtherNpcOpinion(npc.NPCId, mentionsHasan, mentionsSelma, mentionsKemal, mentionsGunes, mentionsYahya, guiltyNpcId);
            emotion = "Düşünceli";
        }
        else if (isOpinionQuery)
        {
            responseText = GetOtherNpcOpinion(npc.NPCId, false, false, false, false, false, guiltyNpcId);
            emotion = "Düşünceli";
        }
        else if (dbPool.Any())
        {
            var usedResponses = history.Select(h => h.NPCResponse).ToHashSet();
            
            // SEMANTIC SCORING (TF-IDF benzeri Eşleştirme)
            NPCDialogue bestMatch = null;
            double highestScore = -1;
            
            foreach (var dialogue in dbPool)
            {
                // Daha önce kullanılan cevapların puanını kır
                bool isUsed = usedResponses.Contains(dialogue.NPCResponse);
                double score = CalculateSemanticScore(normalizedAscii, dialogue.PlayerText, dialogue.Category, currentIntent, npc.NPCId);
                
                if (isUsed) score -= 50; // Penaltı
                
                if (score > highestScore)
                {
                    highestScore = score;
                    bestMatch = dialogue;
                }
            }
            
            // Eğer en yüksek skor çok düşükse fallback'e düş (Eşiği 10 olarak belirliyoruz)
            if (highestScore > 10 && bestMatch != null)
            {
                var selected = bestMatch;

                if (isGuilty && !string.IsNullOrEmpty(selected.GuiltyResponses))
                {
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(selected.GuiltyResponses);
                        if (dict != null && dict.TryGetValue(npc.NPCId.ToString(), out var gResp))
                        {
                            responseText = gResp;
                            emotion = "Tedirgin";
                            trustChange = -5;
                        }
                    }
                    catch { responseText = selected.NPCResponse; }
                }
                
                if (string.IsNullOrEmpty(responseText))
                {
                    responseText = selected.NPCResponse;
                    emotion = isDirectAccusation ? "Gergin" : "Sakin";
                }
            }
            else
            {
                // Eğer kelime sayısı çok kısaysa (örn: sadece "selam" veya anlamsız) genel cevap ver
                if (rawTrLower.Length < 10 && !rawTrLower.Contains("kim") && !rawTrLower.Contains("ne"))
                {
                    responseText = GetGenericPersonaResponse(npc, isGuilty, userQuestion);
                }
                else
                {
                    // Dinamik Akıllı Fallback (Manuel Cevaplar)
                    responseText = GetDynamicFallback(rawTrLower);
                    emotion = "Düşünceli";
                    emotion = "Kararsız";
                    stressIncrease = isGuilty ? 15 : 5; // YZ bilinmeyen soruda suçluysa daha çok streslenir
                }
            }
        }
        else
        {
            responseText = GetGenericPersonaResponse(npc, isGuilty, userQuestion);
        }

        // Suçluyu darlayan kilit kelimeler stresi artırır
        if (isGuilty && (rawTrLower.Contains("neden") || rawTrLower.Contains("yalan") || rawTrLower.Contains("saklıyorsun")))
        {
            stressIncrease += 20;
            emotion = "Gergin";
        }

        if (isGuilty && npcCluesInBagCount > 0 && _random.Next(100) < 45 && revealedSecret == null)
        {
            revealedSecret = $"Amirims, Çetin olarak söylüyorum: {npc.Name} konuşurken gözlerini kaçırıyor. Benim hislerime göre şu konuyla ilgisi var: '{npc.SecretInfo}'";
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

    private static string GetDynamicFallback(string rawTrLower)
    {
        if (rawTrLower.Contains("kim")) return "Kimin yaptığını soruyorsanız amirim, bu kasabada herkesin karanlık bir sırrı var. Özel bir isim mi duymak istiyorsunuz?";
        if (rawTrLower.Contains("nerede") || rawTrLower.Contains("nerde")) return "Mekanı veya yeri soruyorsanız, cinayet gecesi herkesin kendine göre bir mazereti vardı... Detaylandırmanızı rica edeceğim.";
        if (rawTrLower.Contains("neden") || rawTrLower.Contains("niye") || rawTrLower.Contains("sebep")) return "Cinayetin sebebini arıyorsanız, paranın ve gücün olduğu yerde her zaman bir husumet bulunur. Sizce motif ne olabilir?";
        if (rawTrLower.Contains("nasil")) return "Nasıl olduğunu ancak adli tıp ve bulduğunuz deliller söyleyebilir amirim. Sadece ipuçlarını takip edin.";
        if (rawTrLower.Contains("ne zaman")) return "Zamanlamayı soruyorsanız, o gece yağmur çok şiddetliydi, herkes saatler konusunda yalan söylüyor olabilir.";
        
        return "Sorduğunuz sorunun bağlamını tam çözemedim. Olay gecesinden mi yoksa somut bir delilden mi bahsediyorsunuz?";
    }



    private double CalculateSemanticScore(string userQuestion, string playerText, string category, string currentIntent, int npcId)
    {
        double score = 0;
        
        // 1. Intent Match Boost (Niyet Eşleşmesi)
        if (category == currentIntent) score += 100;
        
        // 2. Exact Word Match (Kelime Eşleşmesi)
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
        
        // 3. NPC Specific Lore Boost (Karaktere Özel Anahtar Kelime Uyumu)
        if (category == "local_ai_weapon")
        {
            if (npcId == 1 && userQuestion.Contains("satir")) score += 50;
            if (npcId == 2 && (userQuestion.Contains("zehir") || userQuestion.Contains("sise"))) score += 50;
            if (npcId == 3 && (userQuestion.Contains("gozluk") || userQuestion.Contains("kasa") || userQuestion.Contains("mektup"))) score += 50;
            if (npcId == 4 && (userQuestion.Contains("rozet") || userQuestion.Contains("dugme") || userQuestion.Contains("dosya"))) score += 50;
            if (npcId == 5 && (userQuestion.Contains("iplik") || userQuestion.Contains("kumas") || userQuestion.Contains("cep") || userQuestion.Contains("usb"))) score += 50;
        }
        else if (category == "local_ai_motive")
        {
            if (npcId == 1 && (userQuestion.Contains("borc") || userQuestion.Contains("para"))) score += 50;
            if (npcId == 2 && (userQuestion.Contains("tehdit") || userQuestion.Contains("santaj"))) score += 50;
            if (npcId == 3 && (userQuestion.Contains("tapu") || userQuestion.Contains("arazi"))) score += 50;
            if (npcId == 4 && (userQuestion.Contains("rusvet") || userQuestion.Contains("santaj"))) score += 50;
        }

        // 4. Randomization to avoid exact same answers on ties
        score += _random.NextDouble() * 5;
        
        return score;
    }

    // --- KATİL SIKIŞTIĞINDA (KÖŞEYE SIKIŞMA - 2+ DELİL VARKEN) ---
    private static string GetGuiltyCorneredResponse(int npcId)
    {
        return npcId switch
        {
            1 => "*Gözleri seğirir, elindeki satırı tezgaha fırlatır* Tamam! Yeter amirim, üstüme gelme! O gece Osman'ın evine gittim evet! Borcumu ödemedi, alaycı bir şekilde 'Paran yarın gelecek' dedi... Bir anlık öfke krizine girdim, satırı salladım ama kasten öldürmek istemedim!",
            2 => "*Yüzü sapsarı kesilir, masadan destek alır* Yerin dibine girsin bu iş! Osman beni aylardır şantajla soyuyordu... O ilaca sarmaşık özünü ekledim evet! Ama kıvranarak değil, acısız ölsün istedim!",
            3 => "*Kravatını yırtar gibi gevşetir, nefes nefese kalır* Tamam ulan, tamam! Gece yarısı evine sızdım! Sahte tapuları yırtmaya kalktı, bana saldırdı! Bronz mühürü indirdim şakağına... meşru müdafaaydı benimki!",
            4 => "*Polis copunu masaya bırakır, elini şakaklarına koyar* 15 yıllık mesleğim bitecekti amirim! Osman beni savcılığa ihbar edecekti... O gece gittim, copla vurdum... Rozetim de düğmem de orada koptu. Adalet dedikleri bu mu?!",
            5 => "*Elleri şiddetle titrer, gözyaşlarını siler* Yeter artık, dayanamıyorum! Dikiş ipliğini boynuna dolayan bendim... Osman bana alay edip 'USB'yi asla alamazsın' dedi, kendimi kaybettim!",
            _ => "*Panik içinde nefes alıp verir* Her şey kontrolden çıktı amirim..."
        };
    }

    // --- KATİL İLK DEFA / ŞÜPHE İLE SUÇLANDIĞINDA (KIVIRMA) ---
    private static string GetGuiltyEvadedResponse(int npcId)
    {
        return npcId switch
        {
            1 => "*Satırı daha sıkı tutar* Sen ne diyorsun amirim?! O gece dükkândaydım diyorum! Kurbanla sorunumuz vardı ama katillik başka şey! Başkasının oyununa gelmeyin!",
            2 => "*Gözlerini kaçırır* Ben bir sağlık çalışanıyım amirim! Kurbanın ilacındaki sorunla benim ne ilgim olabilir? Başka birinin tezgahıma girip girmediğini araştırmalısınız!",
            3 => "*Masayı yumruklar* Beni katillikle mi itham ediyorsunuz?! Arazi anlaşmazlığımız vardı diye cinayeti bana yıkamazsınız! Gidin Kasap Hasan'ı sorgulayın!",
            4 => "*Resmi tavrını takınmaya çalışır ama sesi hafif titrer* Ben bu kasabanın komiseriyim amirim! Kanıtınız olmadan bir polise iftira atamazsınız!",
            5 => "*Gözlüklerini siler gibi yapar* Ben dikiş diken yaşlı bir terziyim amirim... Osman ile gizli işlerimiz vardı ama onu öldürmek benim harcım değil!",
            _ => "Bu ithamı kabul etmiyorum amirim!"
        };
    }

    // --- KATİL DELİLSİZ SUÇLANDIĞINDA (DİK DURMA & REDDETME) ---
    private static string GetGuiltyDefensiveResponse(int npcId)
    {
        return npcId switch
        {
            1 => "Ha! Kim söylemiş katil olduğumu?! Benim alacağım vardı Osman'dan, canlısı işime yarardı! Boş iddialarla dükkânımı meşgul etmeyin!",
            2 => "Bana katil demeden önce bir durun amirim! Elimde ne bir kanıt var ne bir şahit. Ben insan iyileştiririm, can almam!",
            3 => "Dedektif efendi, muhtarınızla doğru konuşun! Siyasi rakiplerimin uydurmasıyla karşıma çıkıp beni suçlayamazsınız!",
            4 => "Bu resmi bir soruşturma mı yoksa şahsi bir itham mı? Kanıtın varsa getir, yoksa karakolumdan dışarı çık!",
            5 => "Ceket dikmekten başka bir şey yapmadım ben. Yaşlı adama iftira atmak kolay tabii...",
            _ => "İddialarınız tamamen asılsız amirim!"
        };
    }

    // --- MASUM NPC SUÇLANDIĞINDA ---
    private static string GetInnocentAccusationResponse(int npcId)
    {
        return npcId switch
        {
            1 => "Beni katillikle mi suçluyorsun amirim?! Saçmalama! Ben rızkında bir kasabım. Müşterimi niye öldüreyim? Alacağımı kim ödeyecek o zaman?!",
            2 => "Bana bu iftirayı atamazsınız! Yıllardır bu kasabada şifa dağıtıyorum. Katil arıyorsanız gidin Muhtarın kasasına bakın!",
            3 => "Haddinizi bilin amirim! Ben bu kasabanın seçilmiş muhtarıyım! Elinizde hiçbir kanıt yokken bana çamur atamazsınız!",
            4 => "Bir polise katil demek ağır bir iddiadır amirim! Kanıtın olmadan konuşma, resmi soruşturmayı engellemekten hakkında işlem yaparım!",
            5 => "Ben 70 yaşında dikiş diken bir adamım... Kıymayın bana amirim, günahımı almayın!",
            _ => "Masum insanlara çamur atmayı bırakın da gerçek faili bulun!"
        };
    }

    // --- ALİBİ YANITLARI ---
    private static string GetGuiltyAlibiResponse(int npcId)
    {
        return npcId switch
        {
            1 => "Dükkândaydım diyorum! Et doğruyordum... *gözlerini kaçırır* Yağmur bardaktan boşalıyordu. Kimseyi görmedim... Yani Osman'ın evine sadece borç konuşmaya gittim, o kadar!",
            2 => "Eczanede envanter sayıyordum. Dışarı çıkmadım... *titrer* Şey, gece yarısı sadece hava almak için Osman'ın sokağına doğru yürümüş olabilirim.",
            3 => "Evimdeydim, televizyon izliyordum! ...Pekala, gece saat 11 gibi yürüyüşe çıktım. Kurbanın evinin önünden geçtim ama içeri girmedim diyorum!",
            4 => "Karakoldaydım nöbette! ...Olay yerine ihbardan ÖNCE gittiğim yalan! Ben sadece devriye turundaydım!",
            5 => "Atölyemde dikiş dikiyordum. Makine sesi vardı... Bir anlığına sigara içmeye çıktım ama kurbanın evine kadar gitmedim!",
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
            _ => "Kendi mekânımdaydım amirim."
        };
    }

    // --- WEAPON YANITLARI ---
    private static string GetGuiltyWeaponResponse(int npcId, string rawTrLower)
    {
        return npcId switch
        {
            1 => "*Tezgahtaki satıra bakıp terler* O satır... çalınmıştı diyorum size! Birisi dükkânımdan satırımı alıp Osman'a vurmuş, beni yakmak istiyorlar!",
            2 => "*Zehirli şişeyi görünce elleri titrer* O ilaç reçeteliydi! Şişenin boş olması kurbanın ilacı aşırı dozda içtiğini gösterir, benim suçum ne?!",
            3 => "*Gözlük ve tapuları görünce kızarır* Sahte tapular bir projedir! Kırık gözlük ise kurban bana saldırınca düştü!",
            4 => "*Kopan polis rozetine bakar* O rozet karakoldan çalınmıştı! Olay yerine ben düşürmedim, beni tuzağa düşürüyorlar!",
            5 => "*İplik makarasını cebine saklar* O iplik sağlamdır evet... Ben terziyim amirim, dükkânımda bin tane makara var!",
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
            _ => "Bu delili dikkatle incelemenizi tavsiye ederim amirim."
        };
    }

    // --- MOTİF YANITLARI ---
    private static string GetGuiltyMotiveResponse(int npcId)
    {
        return npcId switch
        {
            1 => "Osman bana 50.000 TL borçluydu! Yıllardır emeğimi sömürdü! 'Yarın öderim' deyip dalga geçti! Hangi insan dayanabilir buna?!",
            2 => "Osman beni geçmişimle tehdit ediyordu! Her ay benden şantajla para alıyordu... Artık dayanacak gücüm kalmamıştı!",
            3 => "O arsa belediyenin geleceğiydi! Osman bencillik yapıp vermiyordu. Kasabanın kalkınmasını engelliyordu!",
            4 => "Beni rüşvet almakla suçlayıp savcılığa gidecekti. Şantaj yapıyordu bana! 15 yıllık şerefimi karartacaktı!",
            5 => "O gizli cebe koyduğu USB bellekte tüm ortaklık sırları vardı. Osman beni saf dışı bırakıp servetime el koyacaktı!",
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
            _ => "Benim kimseyle husumetim yok amirim."
        };
    }

    // --- DİĞER NPC İÇİN GÖRÜŞ ---
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

    // --- RASTGELE/HEDEF ŞÜPHELİ GÖSTERME (CONTEXT MEMORY) ---
    private static string GetRandomSuspectOpinion(int currentNpcId, int guiltyId)
    {
        int targetId = guiltyId;
        if (currentNpcId == guiltyId)
        {
            // Katil kendisi ise hedef şaşırtır
            targetId = currentNpcId == 1 ? 2 : (currentNpcId == 2 ? 3 : 1); 
        }

        return targetId switch
        {
            1 => "Kasap Hasan'a dikkat et amirim. O satırı sadece et kesmek için kullanmıyor. Öfke kontrolü sıfırdır.",
            2 => "Eczacı Selma'nın tezgahının altındaki şişeleri incelediniz mi? Çok sessiz bir kadındır ama sessiz sudan korkacaksın.",
            3 => "Muhtar Kemal... Kasabada her taşın altından o çıkar. Siyasi gücünü kullanarak herkesi eziyor.",
            4 => "Komiser Güneş'in üniformasına güvenmeyin amirim. Kendi karakolunda karanlık işler çeviriyor.",
            5 => "Terzi Yahya... İhtiyar göründüğüne bakmayın, o terzi dükkanı kasabanın tüm dedikodularının ve şantajlarının merkezidir.",
            _ => "Herkes şüpheli amirim, gözünüzü açık tutun."
        };
    }

    // --- GENEL PERSONA YANITI ---
    private static string GetGenericPersonaResponse(NPC npc, bool isGuilty, string originalQuestion)
    {
        return npc.NPCId switch
        {
            1 => "Buyur amirim. Kasap dükkânında cinayet mi çözülür bilmem ama aklıma gelen ne varsa yardımcı olurum.",
            2 => "Hoş geldiniz amirim. Tıbbi konular, reçeteler veya kurban Osman Bey hakkında ne öğrenmek istersiniz?",
            3 => "Buyrun amirim, muhtarlık kapısı devletimizin her temsilcisine açıktır. Sorularınızı dinliyorum.",
            4 => "Amirim, emniyet mensubu olarak soruşturmanızda her türlü kolaylığı sağlamaya hazırım. Dinliyorum.",
            5 => "Hoş geldiniz amirim. Bir sıcak çayımı için, ne merak ediyorsanız sorun laflayalım.",
            _ => "Dinliyorum amirim, buyurun."
        };
    }
}

/// <summary>
/// Türkçe Karakter ve Metin Analiz Motoru (Turkish Text Engine).
/// ş, ç, ı, ü, ö, ğ, İ, Ş, Ç, Ü, Ö, Ğ harflerini korur ve esnek aramaları destekler.
/// </summary>
public static class TurkishTextEngine
{
    private static readonly CultureInfo _cultureTr = new CultureInfo("tr-TR");

    public static string NormalizeToAscii(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string s = text.ToLower(_cultureTr);
        StringBuilder sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            switch (c)
            {
                case 'ç': sb.Append('c'); break;
                case 'ğ': sb.Append('g'); break;
                case 'ı': sb.Append('i'); break;
                case 'i': sb.Append('i'); break;
                case 'ö': sb.Append('o'); break;
                case 'ş': sb.Append('s'); break;
                case 'ü': sb.Append('u'); break;
                default:
                    if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    public static string PreprocessSentence(string text)
    {
        string normalized = NormalizeToAscii(text);
        var tokens = normalized.Split(new[] { ' ', '.', ',', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
        var stopWords = new HashSet<string> { "mi", "mu", "var", "yok", "bir", "ve", "ile", "icin", "diye", "bu", "su", "da", "de", "ki", "iste", "ise" };
        
        List<string> processed = new List<string>();
        foreach (var t in tokens)
        {
            if (t.Length <= 2 && t != "ne") continue; 
            if (stopWords.Contains(t)) continue;
            
            string stemmed = Stem(t);
            string mapped = MapSlang(stemmed);
            processed.Add(mapped);
        }
        return string.Join(" ", processed);
    }

    public static string Stem(string word)
    {
        if (word.Length <= 3) return word;

        // Çok kaba bir Türkçe Stemmer: Sık kullanılan çekim ve yapım eklerini atar
        string[] suffixes = { 
            "yordu", "yorsun", "yorsunuz", "lerdir", "lardir", "lardan", "lerden", "larina", "lerine", 
            "misin", "musun", "misiniz", "musunuz", "miyor", "kiyor", "diler", "dilar", "tilar", "tiler",
            "acak", "ecek", "iyor", "iyor", "iyor", "lar", "ler", "dan", "den", "tan", "ten", "nin", "nun",
            "yla", "yle", "siz", "suz", "sun", "sunuz", "siniz", "sin", "yim", "dik", "tik", "duk", "tuk",
            "di", "ti", "du", "tu", "yi", "ya", "ye", "in", "un", "im", "um"
        };

        foreach (var suffix in suffixes)
        {
            if (word.EndsWith(suffix) && word.Length - suffix.Length >= 3)
            {
                return word.Substring(0, word.Length - suffix.Length);
            }
        }
        
        // Tekil harf ekleri (yönelme, iyelik)
        if ((word.EndsWith("a") || word.EndsWith("e") || word.EndsWith("i") || word.EndsWith("u")) && word.Length >= 4)
        {
            return word.Substring(0, word.Length - 1);
        }

        return word;
    }

    public static string MapSlang(string word)
    {
        return word switch
        {
            "kanki" or "kral" or "abi" or "dayi" or "usta" or "aga" or "haci" or "bilader" => "amirim",
            "sikti" or "kesti" or "deldi" or "cizdi" or "vurdu" or "indirdi" or "deşti" or "kiydi" => "oldur",
            "para" or "mangir" or "sakal" or "avanta" or "cukka" => "borc",
            "cirkef" or "pislik" or "kavga" or "gurultu" => "tartisma",
            "suphe" or "kusku" => "suphe",
            _ => word
        };
    }

    public static bool ContainsAnyConcept(string rawTrLower, string normalizedAscii, params string[] concepts)
    {
        var inputTokens = normalizedAscii.Split(new[] { ' ', '.', ',', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
        string noSpaceInput = normalizedAscii.Replace(" ", "");

        foreach (var concept in concepts)
        {
            string conceptNorm = NormalizeToAscii(concept);
            string noSpaceConcept = conceptNorm.Replace(" ", "");

            if (rawTrLower.Contains(concept) || normalizedAscii.Contains(conceptNorm) || noSpaceInput.Contains(noSpaceConcept))
            {
                return true;
            }

            var conceptTokens = conceptNorm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Eğer kelimeler bitişik yazıldıysa (örn: senmiyaptn) kaba fuzzy eşleşme
            if (noSpaceConcept.Length >= 5)
            {
                int maxTypos = Math.Max(1, noSpaceConcept.Length / 4); 
                // Girdinin uzunluğu konsepte yakınsa direkt mesafe ölç
                if (Math.Abs(noSpaceInput.Length - noSpaceConcept.Length) <= maxTypos + 2)
                {
                    int dist = LevenshteinDistance(noSpaceInput, noSpaceConcept);
                    if (dist <= maxTypos) return true;
                }
            }

            // Kelime kelime bulanık (fuzzy) eşleştirme toleransı
            int matchedTokens = 0;
            int meaningfulTokens = 0;
            foreach (var cToken in conceptTokens)
            {
                if (cToken.Length <= 2) continue; // mi, mu, de gibi ekleri atla
                meaningfulTokens++;
                bool foundToken = false;
                foreach (var iToken in inputTokens)
                {
                    if (iToken.Length > 2)
                    {
                        // Kelime diğerinin içinde tam geçiyorsa (örn: 'sen' ile 'senmi' veya 'yaptn' ile 'yaptın')
                        if (iToken == cToken || iToken.StartsWith(cToken) || iToken.EndsWith(cToken))
                        {
                            foundToken = true;
                            break;
                        }

                        int dist = LevenshteinDistance(iToken, cToken);
                        // Harf sayısına göre dinamik tolerans
                        int maxTypos = cToken.Length >= 6 ? 2 : (cToken.Length >= 4 ? 1 : 0);
                        if (dist <= maxTypos)
                        {
                            foundToken = true;
                            break;
                        }
                    }
                }
                if (foundToken) matchedTokens++;
            }
            
            if (meaningfulTokens > 0 && matchedTokens >= meaningfulTokens)
            {
                return true;
            }
        }
        return false;
    }

    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
