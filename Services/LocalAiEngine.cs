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

        // 1. Türkçe Anlamsal / Niyet Analizi (Intent & Concept Detection)
        bool isDirectAccusation = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii, 
            "sen yaptin", "sen oldurdun", "katil sensin", "itiraf et", "suclu sensin", "sucu sen mi isledin", 
            "kurbani sen mi oldurdun", "sen mi kıydın", "cinayeti sen mi isledin", "cinayeti sen mi yaptin", 
            "kaza degil sen yaptin", "katil kim", "itiraf et artık");

        bool isAlibiQuery = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii,
            "neredeydin", "neredeydiniz", "o gece", "gece yarisi", "saat kacta", "evde miydin", "dukkan", 
            "ne yapiyordun", "gordun mu", "zaman", "saat", "neredeydi", "neredelerdi", "olay ani", "olay gecesi");

        bool isWeaponQuery = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii,
            "satir", "bicak", "zehir", "sise", "mektup", "gozluk", "kasa", "rozet", "dugme", "iplik", 
            "kumas", "usb", "cep", "defter", "delil", "kanit", "esya", "kanli", "kırık", "boş", "yırtık");

        bool isMotiveQuery = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii,
            "borc", "para", "tapu", "arazi", "rusvet", "tehdit", "santaj", "kavga", "tartisma", 
            "neden", "niye", "sebep", "hakkinda", "ilişki", "ilişkin", "dusman", "husumet");

        bool mentionsHasan = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii, "hasan", "kasap");
        bool mentionsSelma = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii, "selma", "eczaci");
        bool mentionsKemal = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii, "kemal", "muhtar");
        bool mentionsGunes = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii, "gunes", "komiser", "polis");
        bool mentionsYahya = TurkishTextEngine.ContainsAnyConcept(rawTrLower, normalizedAscii, "yahya", "terzi");

        // 2. Determine Intent Category & Calculate Annoyance Level based on history
        string currentIntent = "none";
        if (isDirectAccusation) currentIntent = "local_ai_accusation";
        else if (isAlibiQuery) currentIntent = "local_ai_alibi";
        else if (isWeaponQuery) currentIntent = "local_ai_weapon";
        else if (isMotiveQuery) currentIntent = "local_ai_motive";

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
        else if (currentIntent != "none" && dbPool.Any())
        {
            var usedResponses = history.Select(h => h.NPCResponse).ToHashSet();
            var availableOptions = dbPool.Where(x => x.Category == currentIntent).ToList();
            
            if (availableOptions.Any())
            {
                var unusedOptions = availableOptions.Where(x => !usedResponses.Contains(x.NPCResponse)).ToList();
                var selected = unusedOptions.Any() 
                    ? unusedOptions[_random.Next(unusedOptions.Count)] 
                    : availableOptions[_random.Next(availableOptions.Count)];

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
        }
        else if (mentionsHasan || mentionsSelma || mentionsKemal || mentionsGunes || mentionsYahya)
        {
            responseText = GetOtherNpcOpinion(npc.NPCId, mentionsHasan, mentionsSelma, mentionsKemal, mentionsGunes, mentionsYahya, guiltyNpcId);
        }
        else
        {
            responseText = GetGenericPersonaResponse(npc, isGuilty, userQuestion);
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
            RevealedSecret = revealedSecret
        };
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

    // --- GENEL PERSONA YANITI ---
    private static string GetGenericPersonaResponse(NPC npc, bool isGuilty, string originalQuestion)
    {
        return npc.NPCId switch
        {
            1 => isGuilty ? "*Et satırını tezgaha vurur* Ne soracaksan net sor amirim! İşim başımdan aşkın, etler bozulacak!" : "Buyur amirim. Kasap dükkânında cinayet mi çözülür bilmem ama ne biliyorsam anlatırım.",
            2 => isGuilty ? "*İlaç şişelerini düzeltirken elleri titrer* Lütfen hızlı olun, reçeteleri hazırlamam lazım..." : "Hoş geldiniz amirim. Tıbbi konular veya kurbanın ilaçları hakkında bir soru mu soracaksınız?",
            3 => isGuilty ? "*Kravatını gevşetir, terini siler* Kasabamızın huzuru için soruşturmayı uzatmayın lütfen." : "Buyrun amirim, muhtarlık kapısı vatandaşımıza da devlete de açıktır. Ne gerekiyorsa sorun.",
            4 => isGuilty ? "*Masasındaki dosyayı kapatır* Soruşturma gizlidir amirim. Sorularınızı resmi kanaldan sorun." : "Amirim, polis teşkilatı olarak soruşturmanızda her türlü kolaylığı sağlamaya hazırız.",
            5 => isGuilty ? "*Gözlüklerinin üstünden bakar, yutkunur* Kumaşlar, dikişler... Yaşlı bir terziye ne sorabilirsiniz ki?" : "Hoş geldiniz amirim. Bir çayımı için, dikiş dikerken laflayalım.",
            _ => "Dinliyorum amirim."
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
                        if (iToken.Contains(cToken) || cToken.Contains(iToken))
                        {
                            foundToken = true;
                            break;
                        }

                        int dist = LevenshteinDistance(iToken, cToken);
                        // Harf sayısının üçte biri kadar hatayı kabul et (minimum 1 hata)
                        if (dist <= Math.Max(1, cToken.Length / 3))
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
