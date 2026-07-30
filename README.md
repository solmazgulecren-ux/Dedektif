# 🔍 AI Destekli Dedektiflik RPG Oyunu

Yapay zeka destekli, konsol tabanlı bir dedektiflik rol yapma oyunu. Bir kasabadaki şüphelileri sorgulayarak, ipuçları toplayarak ve AI destekli NPC diyaloglarıyla suçluyu bulmaya çalışın.

---

## 🎮 Oyun Hakkında

Kasabada bir cinayet işlenmiştir. Siz bir dedektif olarak 3 şüpheliyi sorgulamanız, ipuçlarını birleştirmeniz ve gerçek suçluyu ortaya çıkarmanız gerekiyor. Her NPC'nin kendine özgü bir kişiliği, sakladığı sırları ve değişen güven/korku seviyeleri vardır.

**Gemini AI** sayesinde NPC'ler sorularınıza dinamik, bağlama uygun ve duygusal yanıtlar verir. Doğru sorular sorarak güven kazanın, baskı uygulayarak korkularını tetikleyin ve sırlarını ortaya çıkarın!

---

## 🛠️ Teknoloji Altyapısı

| Teknoloji | Açıklama |
|-----------|----------|
| **C# / .NET 10** | Ana programlama dili ve framework |
| **SQL Server** | Veritabanı yönetim sistemi |
| **Dapper** | Hafif ve hızlı ORM (Object-Relational Mapping) |
| **Gemini AI API** | NPC diyalogları için yapay zeka motoru |
| **System.Text.Json** | JSON serileştirme ve ayrıştırma |

---

## 📁 Proje Mimarisi

```
Dedektiflik/
├── Models/                          # Veri modelleri
│   ├── NPC.cs                       # Şüpheli karakter modeli
│   ├── Clue.cs                      # İpucu modeli
│   ├── DialogLog.cs                 # Diyalog kayıt modeli
│   └── AIInteractionResponse.cs     # AI yanıt modeli
├── Data/                            # Veritabanı katmanı
│   ├── schema.sql                   # SQL tablo şeması ve seed data
│   └── DatabaseRepository.cs        # Dapper ile CRUD işlemleri
├── Services/                        # İş mantığı katmanı
│   ├── AntigravityAiService.cs      # Gemini AI API entegrasyonu
│   └── DialogManager.cs             # Diyalog akış yönetimi
├── UI/                              # Kullanıcı arayüzü
│   └── ConsoleUI.cs                 # Renkli interaktif konsol arayüzü
├── Program.cs                       # Ana giriş noktası
├── appsettings.Example.json         # Konfigürasyon şablonu
├── DedektiflikRPG.csproj            # Proje dosyası
└── .gitignore                       # Git hariç tutulan dosyalar
```

---

## 🚀 Kurulum ve Çalıştırma

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB, Express veya üstü)
- [Gemini API Anahtarı](https://aistudio.google.com/apikey)

### 1. Projeyi Klonlayın

```bash
git clone https://github.com/solmazgulecren-ux/Dedektif.git
cd Dedektif
```

### 2. Veritabanını Kurun

SQL Server Management Studio veya `sqlcmd` ile:

```sql
CREATE DATABASE DedektiflikRPG;
GO
```

Ardından `Data/schema.sql` dosyasını çalıştırarak tabloları ve varsayılan verileri oluşturun:

```bash
sqlcmd -S localhost -d DedektiflikRPG -i Data/schema.sql
```

### 3. Konfigürasyonu Ayarlayın

`appsettings.Example.json` dosyasını `appsettings.json` olarak kopyalayın ve değerleri doldurun:

```bash
copy appsettings.Example.json appsettings.json
```

```json
{
  "ConnectionString": "Server=localhost;Database=DedektiflikRPG;Trusted_Connection=true;TrustServerCertificate=true;",
  "GeminiApiKey": "BURAYA_GEMINI_API_ANAHTARINIZI_GIRIN",
  "GeminiModel": "gemini-2.0-flash"
}
```

### 4. Projeyi Derleyin ve Çalıştırın

```bash
dotnet restore
dotnet build
dotnet run
```

---

## 🎯 Oyun Komutları

| Komut | Açıklama |
|-------|----------|
| `1`, `2`, `3` | Şüpheliyle sorgulama başlat |
| `ipucular` | Eldeki tüm ipuçlarını göster |
| `sorgula` | Bir NPC'nin sorgulama geçmişini görüntüle |
| `degistir` | Sorgulama sırasında ana menüye dön |
| `cikis` | Oyundan çık |

---

## 🎭 Kasabadaki Şüpheliler

| # | İsim | Rol |
|---|------|-----|
| 1 | Kasap Hasan | Kasabadaki eski kasap, herkesin tanıdığı bir figür |
| 2 | Eczacı Selma | Eczane sahibi, ilaç ve zehir konusunda uzman |
| 3 | Muhtar Kemal | Kasabanın muhtarı, herkesin sırrını bilen bir politikacı |

---

## 🧠 AI Sistemi Nasıl Çalışır?

1. **Dinamik Prompt**: Her sorgulama sırasında NPC'nin güven seviyesi, korku seviyesi, suçluluk durumu ve sakladığı sır bilgilerinden otomatik bir system prompt oluşturulur.
2. **Bağlamsal Yanıtlar**: AI, oyuncunun elindeki ipuçlarını da dikkate alarak tutarlı ve hikâyeye uygun cevaplar üretir.
3. **Duygu Durumu**: Her yanıtta NPC'nin duygu durumu (sinirli, korkmuş, sakin, samimi, pişman vb.) raporlanır.
4. **Güven Mekanizması**: Doğru sorular güveni artırır, agresif sorular güveni düşürür. Yüksek güvende NPC sırlarını paylaşabilir.

---

## 📄 Lisans

Bu proje eğitim ve kişisel kullanım amaçlıdır.

---

## 👩‍💻 Geliştirici

**Solmaz Gülecren** — [GitHub](https://github.com/solmazgulecren-ux)
