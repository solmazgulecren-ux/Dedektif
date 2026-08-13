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

        // Selamlama / Nezaket Kontrolü
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

        string currentIntent = "none";
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

                bool logAcc = TurkishTextEngine.ContainsAnyConcept(logText, logAscii, "sen yaptin", "katil sensin", "itiraf et", "sen öldürdün", "sen misin katil");
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
                responseText = GetDynamicFallback(rawTrLower);
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

    private static string GetGuiltyWeaponResponse(int npcId, string rawTrLower)
    {
        return npcId switch
        {
            1 => "*Tezgahtaki satıra bakıp terler* O satır... dükkânımdan çalınmıştı diyorum size! Birisi benim satırımı alıp Osman'a vurmuş, beni yakmak istiyorlar!",
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

    private static string GetRandomSuspectOpinion(int currentNpcId, int guiltyId)
    {
        int targetId = guiltyId;
        if (currentNpcId == guiltyId)
        {
            targetId = currentNpcId == 1 ? 2 : (currentNpcId == 2 ? 3 : 1);
        }

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

    private static string GetDynamicFallback(string rawTrLower)
    {
        if (rawTrLower.Contains("kim")) return "Kimin yaptığını soruyorsanız amirim, bu kasabada herkesin karanlık bir sırrı var. Özel bir isim mi duymak istiyorsunuz?";
        if (rawTrLower.Contains("nerede") || rawTrLower.Contains("nerde")) return "Mekanı veya yeri soruyorsanız, cinayet gecesi herkesin kendine göre bir mazereti vardı... Detaylandırmanızı rica edeceğim.";
        if (rawTrLower.Contains("neden") || rawTrLower.Contains("niye") || rawTrLower.Contains("sebep")) return "Cinayetin sebebini arıyorsanız, paranın ve gücün olduğu yerde her zaman bir husumet bulunur. Sizce motif ne olabilir?";
        if (rawTrLower.Contains("nasil")) return "Nasıl olduğunu ancak adli tıp ve bulduğunuz deliller söyleyebilir amirim. Sadece ipuçlarını takip edin.";
        if (rawTrLower.Contains("ne zaman")) return "Zamanlamayı soruyorsanız, o gece yağmur çok şiddetliydi, herkes saatler konusunda yalan söylüyor olabilir.";

        return "Sorduğunuz sorunun bağlamını tam çözemedim. Olay gecesinden mi yoksa somut bir delilden mi bahsediyorsunuz?";
    }

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
