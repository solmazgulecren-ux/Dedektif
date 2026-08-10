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
        if (count >= 1000) return; // Zaten seedlenmiş

        Console.WriteLine("Yapay Zeka İçin 1000+ Cümle Veritabanına Yükleniyor...");

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
            Console.WriteLine($"Başarıyla {insertList.Count} adet AI diyalog varyasyonu (Memory Database) eklendi!");
        }
        catch(Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine("AI Seed Hatası: " + ex.Message);
        }
    }

    private string GenerateInnocentAlibi(int npcId, int i)
    {
        string[] bases = { "Dükkandaydım.", "Evimdeydim.", "Çalışıyordum.", "Dinleniyordum.", "Kendi işimle meşguldüm." };
        string[] extras = { " Neden sordunuz?", " Yağmur yağıyordu zaten.", " Kimseyi görmedim.", " Siz de beni mi suçluyorsunuz?" };
        return bases[i % bases.Length] + extras[(i / bases.Length) % extras.Length];
    }
    private string GenerateGuiltyAlibi(int npcId, int i)
    {
        string[] bases = { "Dükkan... Dükkandaydım!", "Televizyon izliyordum...", "Uyuyordum diyorum!", "O saatte dışarı çıkmadım!" };
        string[] extras = { " *Gözlerini kaçırır*", " *Terler*", " Lütfen üstüme gelmeyin.", " Ben masumum!" };
        return extras[(i / bases.Length) % extras.Length] + " " + bases[i % bases.Length];
    }
    private string GenerateInnocentMotive(int npcId, int i)
    {
        string[] bases = { "Kurbanla ticaretimiz vardı.", "Sadece bir müşterimdi.", "Husumetim yoktu.", "Aramız iyiydi." };
        return bases[i % bases.Length] + " Beni o tür işlere karıştırmayın.";
    }
    private string GenerateGuiltyMotive(int npcId, int i)
    {
        string[] bases = { "Beni tehdit ediyordu!", "Paramı çaldı!", "Hayatımı mahvetmek üzereydi...", "Başka çarem kalmamıştı." };
        return "*Nefesi hızlanır* " + bases[i % bases.Length] + " O adamın ne kadar zalim olduğunu bilmiyorsunuz!";
    }
    private string GenerateInnocentWeapon(int npcId, int i)
    {
        return "O eşya benimle ilgili değil. Dikkatli incelerseniz başkasına ait olduğunu anlarsınız.";
    }
    private string GenerateGuiltyWeapon(int npcId, int i)
    {
        return "*Eşyayı görünce rengi atar* O... O bana ait değil! Hayır, biri tezgahımı karıştırmış!";
    }
    private string GenerateInnocentAccusation(int npcId, int i)
    {
        string[] bases = { "Haddinizi bilin amirim!", "Beni böyle suçlayamazsınız!", "Masum bir insana iftira atmayın!", "Avukatımı çağıracağım!" };
        return bases[i % bases.Length] + " İşinize dönün lütfen.";
    }
    private string GenerateGuiltyAccusation(int npcId, int i)
    {
        string[] bases = { "Ben yapmadım! Ben değildim!", "O... O bir kazaydı!", "Kanıtınız var mı?! Beni hapse atamazsınız!", "Beni o kışkırttı!" };
        return "*Panikle masaya tutunur* " + bases[i % bases.Length];
    }
}
