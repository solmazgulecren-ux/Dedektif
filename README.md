# 🔍 Dedektif — Karanlık Kasabanın Sırrı

Yapay zeka (AI) hikaye tabanlı, **Point & Click (Tıkla & Bul)** tarzı bir web dedektiflik oyunudur. Karanlık bir kasabada işlenen cinayeti çözmek için binaları gezmeli, ipuçlarını toplamalı ve 5 farklı şüpheliyi sorguya çekerek gerçek suçluyu bulmalısınız!

---

## 🎮 Oyun Hakkında

Yağmurlu bir sonbahar gecesi... Kasabanın meydanında bir ceset bulundu. Kurban, herkesin tanıdığı tüccar Osman Bey'di. Siz bir dedektif olarak **5 şüpheliyi** sorgulamalı, gizli kalmış ipuçlarını birleştirmeli ve gerçek suçluyu ortaya çıkarmalısınız. 

Oyun her başladığında suçlu karakter **rastgele** belirlenir. Bu sayede her oynayışınızda farklı bir deneyim yaşarsınız!

---

## 🛠️ Teknoloji Altyapısı

| Teknoloji | Açıklama |
|-----------|----------|
| **C# / .NET 10** | Arka plan (Backend) ve Minimal API |
| **SQLite & Dapper** | Hafif ve hızlı veritabanı yönetimi |
| **Vanilla HTML/CSS/JS** | Tarayıcı tabanlı (Point & Click) kullanıcı arayüzü |
| **Gemini AI API** | NPC diyaloglarının ve hikaye üretiminin altyapısı |

---

## 📁 Proje Mimarisi

```
Dedektiflik/
├── Models/                          # Veri modelleri (NPC, Clue, vb.)
├── Data/                            # Veritabanı katmanı
│   ├── schema.sql                   # SQLite tablo şeması (100 diyalog tohumu)
│   └── DatabaseRepository.cs        # Dapper ile veritabanı işlemleri
├── wwwroot/                         # Frontend (Kullanıcı Arayüzü)
│   ├── index.html                   # Ana oyun ekranı (Harita, Menüler)
│   ├── css/style.css                # Noir tarzı oyun tasarımları
│   ├── js/app.js                    # Oyun motoru (UI mantığı, tıkla/bul)
│   └── images/                      # AI ile üretilmiş oyun görselleri
├── Program.cs                       # Ana giriş noktası ve API uç noktaları
├── appsettings.json                 # Konfigürasyon
└── README.md                        # Bu dosya
```

---

## 🚀 Kurulum ve Çalıştırma

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Modern bir web tarayıcısı (Chrome, Firefox, Edge)

### 1. Projeyi Klonlayın

```bash
git clone https://github.com/solmazgulecren-ux/Dedektif.git
cd Dedektif
```

### 2. Konfigürasyonu Ayarlayın

`appsettings.Example.json` dosyasını `appsettings.json` olarak kopyalayın ve gerekli değerleri (API anahtarı vs.) doldurun:

```bash
copy appsettings.Example.json appsettings.json
```

### 3. Projeyi Derleyin ve Çalıştırın

```bash
dotnet restore
dotnet build
dotnet run
```

Konsolda uygulamanın başladığı URL'yi (genellikle `http://localhost:5000`) göreceksiniz. Bu adresi tarayıcınızda açarak oyuna başlayabilirsiniz.

---

## 🎯 Oyun Mekanikleri

- **Keşif (Harita):** Kasabanın kuşbakışı haritasından binalara tıklayarak içeri girin.
- **İpucu Arama:** Binaların içinde karanlık köşelere saklanmış kanıtları bularak dedektif çantanıza ekleyin.
- **Sorgulama:** Her binanın sahibini (NPC) sorgulayın. Her karakter için 5 kademeli derinleşen bir diyalog sistemi mevcuttur.
- **Bina Kilitleme:** Bir binadan çıktığınızda o bina kilitlenir. Çıkmadan önce tüm ipuçlarını bulduğunuzdan ve yeterince soru sorduğunuzdan emin olun!
- **Suçlama (BULDUM!):** Tüm ipuçlarını toplayıp şüphelilerle konuştuktan sonra "BULDUM!" butonuna basıp konuşma geçmişlerini (💬 Konuşma Geçmişi butonu ile) inceleyerek gerçek katili seçin.

---

## 🎭 Kasabadaki Şüpheliler

| # | İsim | Mekan | Rol |
|---|------|-------|-----|
| 1 | Kasap Hasan | Kasap | Kasabadaki eski kasap, herkesin tanıdığı sert bir figür. |
| 2 | Eczacı Selma | Eczane | Eczane sahibi, ilaç ve zehir konusunda uzman. |
| 3 | Muhtar Kemal | Muhtarlık | Kasabanın muhtarı, herkesin sırrını bilen bir politikacı. |
| 4 | Komiser Güneş| Karakol | Kasabanın kadın polis komiseri, olay yerini ilk inceleyen kişi. |
| 5 | Terzi Yahya | Terzi | Kasabanın yaşlı terzisi, kurbana son kıyafeti diken kişi. |

---

## 📄 Lisans

Bu proje eğitim ve kişisel kullanım amaçlıdır.

---

## 👩‍💻 Geliştirici

**Ecren Solmazgül** — [GitHub](https://github.com/solmazgulecren-ux)
