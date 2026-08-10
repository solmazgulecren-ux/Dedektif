using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DedektiflikRPG.Models;
using System.Text.Json;

namespace DedektiflikRPG.Data;

public class AISeeder
{
    private readonly DatabaseRepository _repository;

    public AISeeder(DatabaseRepository repository)
    {
        _repository = repository;
    }

    public async Task Seed1000PlusDialoguesAsync()
    {
        using var db = _repository.CreateConnection();
        db.Open();
        
        int count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM NPCDialogues WHERE Category LIKE 'local_ai_%'");
        if (count >= 1200) return; // Zaten seedlenmiş

        // Eğer veritabanı daha önce seedlendiyse (Eski jenerik verileri) temizle
        await db.ExecuteAsync("DELETE FROM NPCDialogues WHERE Category LIKE 'local_ai_%'");

        Console.WriteLine("Yapay Zeka İçin 1250+ Senaryo Bazlı Cümle Veritabanına Yükleniyor...");

        var allNPCs = await db.QueryAsync<NPC>("SELECT * FROM NPCs");
        
        string[] alibiQuestions = { "neredeydin", "o gece", "cinayet saati nerede idin", "evde miydin", "ne yapiyordun" };
        string[] motiveQuestions = { "borc", "para", "tapu", "tehdit", "neden", "husumet", "ilişki", "sebep" };
        string[] weaponQuestions = { "satir", "bicak", "zehir", "sise", "gozluk", "rozet", "iplik", "delil" };
        string[] accusationQuestions = { "sen yaptin", "katil sensin", "itiraf et", "suclu sensin", "sen öldürdün" };

        var insertList = new List<NPCDialogue>();

        // 5 NPC * 4 Kategori * 60 varyasyon = 1200 Satır
        foreach (var npc in allNPCs)
        {
            // 1. Alibi Üretimi
            for (int i = 0; i < 60; i++)
            {
                insertList.Add(new NPCDialogue
                {
                    NPCId = npc.NPCId,
                    Category = "local_ai_alibi",
                    PlayerText = alibiQuestions[i % alibiQuestions.Length],
                    Difficulty = (i % 3) + 1,
                    NPCResponse = GenerateInnocentAlibi(npc.NPCId, i),
                    GuiltyResponses = JsonSerializer.Serialize(new Dictionary<string, string> { { npc.NPCId.ToString(), GenerateGuiltyAlibi(npc.NPCId, i) } }),
                    IsAccusatory = false
                });
            }

            // 2. Motif Üretimi
            for (int i = 0; i < 60; i++)
            {
                insertList.Add(new NPCDialogue
                {
                    NPCId = npc.NPCId,
                    Category = "local_ai_motive",
                    PlayerText = motiveQuestions[i % motiveQuestions.Length],
                    Difficulty = (i % 3) + 1,
                    NPCResponse = GenerateInnocentMotive(npc.NPCId, i),
                    GuiltyResponses = JsonSerializer.Serialize(new Dictionary<string, string> { { npc.NPCId.ToString(), GenerateGuiltyMotive(npc.NPCId, i) } }),
                    IsAccusatory = false
                });
            }

            // 3. Silah/Delil Üretimi
            for (int i = 0; i < 60; i++)
            {
                insertList.Add(new NPCDialogue
                {
                    NPCId = npc.NPCId,
                    Category = "local_ai_weapon",
                    PlayerText = weaponQuestions[i % weaponQuestions.Length],
                    Difficulty = (i % 3) + 1,
                    NPCResponse = GenerateInnocentWeapon(npc.NPCId, i),
                    GuiltyResponses = JsonSerializer.Serialize(new Dictionary<string, string> { { npc.NPCId.ToString(), GenerateGuiltyWeapon(npc.NPCId, i) } }),
                    IsAccusatory = false
                });
            }

            // 4. Doğrudan Suçlama Üretimi
            for (int i = 0; i < 70; i++)
            {
                insertList.Add(new NPCDialogue
                {
                    NPCId = npc.NPCId,
                    Category = "local_ai_accusation",
                    PlayerText = accusationQuestions[i % accusationQuestions.Length],
                    Difficulty = (i % 3) + 2, 
                    NPCResponse = GenerateInnocentAccusation(npc.NPCId, i),
                    GuiltyResponses = JsonSerializer.Serialize(new Dictionary<string, string> { { npc.NPCId.ToString(), GenerateGuiltyAccusation(npc.NPCId, i) } }),
                    IsAccusatory = true
                });
            }
        }

        using var transaction = db.BeginTransaction();
        try
        {
            var sql = @"
                INSERT INTO NPCDialogues (NPCId, Difficulty, Category, PlayerText, NPCResponse, GuiltyResponses, IsAccusatory)
                VALUES (@NPCId, @Difficulty, @Category, @PlayerText, @NPCResponse, @GuiltyResponses, @IsAccusatory)";
            
            await db.ExecuteAsync(sql, insertList, transaction);
            transaction.Commit();
            Console.WriteLine($"Başarıyla {insertList.Count} adet senaryo bazlı AI diyalog varyasyonu eklendi!");
        }
        catch(Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine("AI Seed Hatası: " + ex.Message);
        }
    }

    private string GenerateInnocentAlibi(int npcId, int i)
    {
        string[] bases = npcId switch {
            1 => new[] { "Gece dükkandaydım, et doğruyordum.", "Soğuk hava deposunu temizliyordum.", "Veresiye defterimi kontrol ediyordum." },
            2 => new[] { "Eczanede nöbetçiydim, envanter sayıyordum.", "Arka odada tıbbi bitkilerimi kurutuyordum.", "Dükkandan hiç çıkmadım, pencereden sokağı izledim." },
            3 => new[] { "Evimdeydim, ertesi günün toplantısına hazırlanıyordum.", "Muhtarlık ofisinde geç saate kadar evrak imzaladım.", "Televizyon izliyordum, muhtarın sokakta işi olmaz." },
            4 => new[] { "Karakolda nöbetteydim, telsiz dinliyordum.", "Gece devriyesinden dönmüş, çay içiyordum.", "Olay raporlarını düzenliyordum amirim." },
            5 => new[] { "Atölyede dikiş dikiyordum, gözlerim yorulana kadar çalıştım.", "Yeni bir siparişi bitirmeye çalışıyordum.", "İhtiyar bir terzinin gece evinden başka nerede işi olur?" },
            _ => new[] { "Evimdeydim.", "Çalışıyordum.", "Dinleniyordum." }
        };
        string[] extras = { " Neden sordunuz?", " Yağmur yağıyordu zaten dışarı çıkmadım.", " Kimseyi görmedim.", " Siz de beni mi sorguluyorsunuz?" };
        return bases[i % bases.Length] + extras[(i / bases.Length) % extras.Length];
    }
    private string GenerateGuiltyAlibi(int npcId, int i)
    {
        string[] bases = npcId switch {
            1 => new[] { "Gece... dükkandaydım. Yağmurdan dolayı kapıyı erken kitledim sadece.", "Evime geçmiştim... Evet, kesinlikle evimdeydim.", "Etleri depoya koyup... sokağa adımımı atmadım." },
            2 => new[] { "Eczanedeydim... Belki bir ara hava almak için kapıya çıkmışımdır.", "Reçete defterime bakıyordum, dükkandan çıkmam gerekecek bir durum yoktu.", "Arka tarafta bitkilerle ilgileniyordum... Dışarıdaki gürültüyü duymadım." },
            3 => new[] { "Evimdeydim dedim ya! Belki bir ara camdan dışarı bakmışımdır.", "Ofisteydim... Bazı tapu meselelerini... yani sıradan işleri hallediyordum.", "Sokağa çıkmadım. O saatte yağmurda ne işim olur?" },
            4 => new[] { "Devriyedeydim... Ama kurbanın sokağına hiç uğramadım.", "Karakoldan çıkmadım. Telsizden anons gelene kadar bekledim sadece.", "Bana sormayın, telsiz kayıtlarına bakın." },
            5 => new[] { "Dikiş dikiyordum... Ellimdeki iğne battı, biraz hava almak istedim o kadar.", "Evimdeydim... Yani atölyemde. Sadece birine bir paket teslim etmem gerekiyordu ama gitmedim.", "Ben yaşlı bir adamım, o saatte dışarıda dolaşmam." },
            _ => new[] { "Evimdeydim diyorum!", "O saatte dışarı çıkmadım!" }
        };
        string[] extras = { " İnanmıyorsanız komşulara sorun.", " O saatte herkes uyur, kimse sizi görmez zaten.", " Sorularınız çok bunaltıcı amirim.", " Eminim gerçek katil dışarıda geziyordu." };
        return bases[i % bases.Length] + extras[(i / bases.Length) % extras.Length];
    }
    
    private string GenerateInnocentMotive(int npcId, int i)
    {
        string[] bases = npcId switch {
            1 => new[] { "Osman iyi müşterimdi, veresiye borcu vardı ama öderdi.", "Kurbanla sadece et ticaretimiz vardı.", "Benim onla bir derdim yok, borcu olan benim derdim olmaz." },
            2 => new[] { "Sadece sıradan bir hastaydı, kalbi için ilaç alırdı.", "Kurbanın sağlık sorunları vardı, ben sadece tedavi etmeye çalışıyordum.", "Arkamızdan konuşanlar olur ama ben kimseye kin tutmam." },
            3 => new[] { "Arazi meseleleri her zaman olur, ama bunlar siyasetin cilvesi.", "Osman ile ara sıra tartışırdık ama kasaba için ortak paydada buluşurduk.", "Ölümünden benim ne çıkarım olabilir? Belediye işleri zaten kanunla yürür." },
            4 => new[] { "Bir polis olarak kasabadaki herkesle mesafeliyimdir. Osman da sadece bir vatandaştı.", "O adamın bazı karanlık işleri vardı ama benim masama hiç düşmedi.", "Bana rüşvet teklif eden çok olur, ben adaletten şaşmam." },
            5 => new[] { "Ben sadece ceketini diken yaşlı bir terziyim, adamla ne alıp veremediğim olabilir?", "Osman bey kıyafetine düşkündü, gizli cepler isterdi ama ne sakladığını hiç sormadım.", "Bizim gibi esnaf, müşterisiyle husumet kurmaz." },
            _ => new[] { "Kurbanla ticaretimiz vardı.", "Sadece bir müşterimdi." }
        };
        return bases[i % bases.Length] + " Lütfen beni asılsız şeylerle suçlamayın.";
    }
    private string GenerateGuiltyMotive(int npcId, int i)
    {
        string[] bases = npcId switch {
            1 => new[] { "Osman o borcu asla ödemeyecekti... Bunu herkes biliyordu. Onun gibi kibirli adamlar başkalarını ezmeyi sever.", "Ete para vermez, ama lüks içinde yaşardı... Borcunu istediğimde bana güldü.", "O adamın ne kadar zalim olduğunu kimse bilmiyor. Belki de birisi artık dayanamamıştır." },
            2 => new[] { "Bazı hastalar çok talepkardır... Sürekli tehdit ederler. Osman da haddini aşanlardandı.", "Bana sırrımı ifşa edeceğini söylüyordu... Kendini çok akıllı sanıyordu.", "Eczacı olmak zordur, bazen yanlış reçete yazan doktorların hatalarını bizim temizlememiz beklenir." },
            3 => new[] { "O arsalar kasabanın geleceği için lazımdı! Ama Osman hep engel oluyordu, vizyonsuz bir adamdı.", "Beni şikayet etmekle tehdit ediyordu. Sanki bu kasabayı ben inşa etmemişim gibi!", "Onun inadı yüzünden kasaba kaybediyordu. Birinin onu yoldan çekmesi... yani ikna etmesi lazımdı." },
            4 => new[] { "Osman çok tehlikeli sulara girdi. Benim gibi birine şantaj yapmak akıl karı değil.", "Herkesin sırrı vardır amirim. Osman benim sırrımı öğrenmişti... ve bunu kullanmaya kalktı.", "Adalet bazen kör değildir amirim. Bazen adaleti kendi ellerinle sağlaman gerekir." },
            5 => new[] { "Beni o USB ile köşeye sıkıştırdı. Yılların emeğini bir çırpıda silecekti.", "Benden gizli cepler dikmemi isterdi, ama o ceplerde ne taşıdığını öğrendiğimde beni susturmak istedi.", "Yaşlı bir adamı küçümserseniz, o iğnenin ne kadar derine batacağını tahmin edemezsiniz." },
            _ => new[] { "Beni tehdit ediyordu!", "Başka çarem kalmamıştı." }
        };
        return "*Soğukkanlı görünmeye çalışarak* " + bases[i % bases.Length] + " Kim yaptıysa, kasabayı büyük bir yükten kurtarmış.";
    }
    
    private string GenerateInnocentWeapon(int npcId, int i)
    {
        string[] bases = npcId switch {
            1 => new[] { "O kanlı satır çalınmıştı amirim! Polise de bildirdim.", "Önlüğümdeki kan dana kanı, insan değil. Kasabım ben!", "O defteri herkes görebilir, üstünü çizmem borcunu ödediği içindi." },
            2 => new[] { "Boş şişeyi çöpe atmıştım, biri oradan almış olmalı.", "Zehirli sarmaşığı sadece merhemler için kullanıyorum. Asla içilmez.", "Yırtık sayfalar... Onlar sadece yanlış yazdığım reçetelerdi." },
            3 => new[] { "Kırık gözlük mü? Kurban buraya geldiğinde düşürmüştü, geri verecektim.", "O mektup sinirle yazıldı, ama asla gönderilmedi.", "Kasadaki evraklar sadece eski kopyalar. Cinayet silahı sayılamazlar." },
            4 => new[] { "Polis rozetim mi? Onu devriyede düşürmüş olmalıyım.", "O düğme çok sıradan bir düğme, benim paltoma ait olduğunu nereden çıkardınız?", "Dosya kilitliydi, kurbanın geçmişini araştırmak benim görevim." },
            5 => new[] { "İplik makarasındaki kan benim elime ait, dikiş dikerken iğne battı.", "Kurbanın ceketi burada dikildi, kumaş parçası kalması çok doğal.", "Gizli cebi o istedi, içine ne koyduğunu ben bilemem." },
            _ => new[] { "O eşya benimle ilgili değil.", "Dikkatli incelerseniz başkasına ait olduğunu anlarsınız." }
        };
        return bases[i % bases.Length];
    }
    private string GenerateGuiltyWeapon(int npcId, int i)
    {
        string[] bases = npcId switch {
            1 => new[] { "Satır benim evet ama... herkesin satırı var! O kan... hayır, o dana kanı olmalı!", "Defterdeki kırmızı çizgi... Sadece sinirle çizdim. Bu beni katil yapmaz ki!", "Önlüğümdeki kan... Ben bütün gün et kesiyorum, ne bekliyordunuz?" },
            2 => new[] { "O zehirli bitkiyi herkes ormandan toplayabilir. Şişeyi de dışarıdan biri almış olabilir.", "Reçete sayfalarını ben yırttım çünkü... ticari sırdı. İlaç dozajıyla ilgisi yok!", "Kurbanın ilacına ben bir şey katmadım. Kendi kalbi dayanamamıştır belki..." },
            3 => new[] { "Gözlük... Evet kavga ettik, itişme oldu ve düştü. Ama onu ben öldürmedim!", "Tehdit mektubu sadece korkutmak içindi, icraata geçmek için değil.", "Sahte tapular benim siyasi kariyerim için, cinayet için değil. Bu deliller alakasız!" },
            4 => new[] { "Rozetim olay yerindeyse, cinayeti incelemeye gittiğim içindir! Olay yerini ilk ben gördüm.", "Düğme... Palto bana ait olabilir ama o düğme çoktan kopmuştu.", "Kurbanı copla mı darp etmişler? Benim copum her zaman belimdedir." },
            5 => new[] { "Kanlı iplik... O ip çok sağlamdır evet ama boğmak için kullanıldığını nereden çıkardınız?", "Ceketteki gizli cepte ne olduğu umurumda değil. Belki de notu o yazmıştır.", "Kumaş benden alınmış olabilir ama herkes benden kumaş alıyor." },
            _ => new[] { "O... O bana ait değil! Hayır, biri tezgahımı karıştırmış!" }
        };
        string[] extras = { " Bunlar sadece tesadüf amirim.", " Delilleri benim aleyhime kullanmaya çalışıyorsunuz.", " Mahkemede bu kanıtlar yetersiz kalır.", " Gerçek bir kanıtınız olsaydı şu an burada konuşmuyor olurduk." };
        return "*Gözlerini hafifçe kısarak* " + bases[i % bases.Length] + extras[(i / bases.Length) % extras.Length];
    }
    
    private string GenerateInnocentAccusation(int npcId, int i)
    {
        string[] bases = { "Haddinizi bilin amirim!", "Beni böyle doğrudan suçlayamazsınız!", "Masum bir insana iftira atmayın!", "Avukatımı çağıracağım!" };
        return bases[i % bases.Length] + " Gidip gerçek katili bulun.";
    }
    private string GenerateGuiltyAccusation(int npcId, int i)
    {
        string[] bases = npcId switch {
            1 => new[] { "Ben sadece paramı istedim! Üzerime yürüdü, bana hakaret etti. Satır elimdeydi...", "Beni suçlayamazsınız! O adam kasabanın kanını emiyordu!", "Tamam! Tartıştık ama amacım sadece korkutmaktı... Kazaydı diyorum!" },
            2 => new[] { "Beni hapse atamazsınız! Ben hayat kurtarırım! O adamın yaşatılması bir hataydı.", "O zehir acısızdır amirim. Ona bir lütufta bulundum aslında.", "Bana şantaj yapıyordu! Başka ne yapabilirdim? Siz olsanız ne yapardınız?" },
            3 => new[] { "Ben bu kasabanın lideriyim! Osman benim yoluma çıkmamalıydı.", "Beni tutuklarsanız kasaba kaosa sürüklenir. O adamın ölmesi hepimizin iyiliği içindi.", "Evet ben yaptım! Çünkü kimsenin onu durduracak cesareti yoktu!" },
            4 => new[] { "Bana kelepçe takmak sizin haddinize mi? Ben yıllarımı bu mesleğe verdim!", "O beni teşkilata satacaktı. Kariyerimi bir çöp uğruna mahvedemezdim.", "Delilleri istediğim gibi karartırım. Kanıtlayamazsınız... En azından deneyene kadar." },
            5 => new[] { "Yıllarca onun pis işlerini dikip gizledim. Sonunda beni çöpe atamazdı.", "O ip onun boynuna çok yakıştı. Tıpkı diktiğim ceketler gibi kusursuz.", "Ben yaşlı biriyim. Cezaevinde ne kadar yaşarım ki? Ama o... o cezasını buldu." },
            _ => new[] { "Ben yapmadım! Ben değildim!", "O... O bir kazaydı!" }
        };
        string[] evasions = { 
            "Ama elinizdeki kanıtlar beni içeri tıkmaya yetecek mi?", 
            "Bunu ispatlayamazsınız. Mahkemede her şeyi reddedeceğim.", 
            "Bunu kasaba halkına nasıl açıklayacaksınız? Herkes onu sevmediğimi biliyordu ama katil olduğuma inanmazlar.", 
            "Belki de... Belki de başkası da yardım etmiştir. Ne malum?"
        };
        return bases[i % bases.Length] + " " + evasions[(i / bases.Length) % evasions.Length];
    }
}
