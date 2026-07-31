-- =============================================
-- AI Destekli Dedektiflik RPG - Veritabanı Şeması v2.0
-- 5 NPC, Kademesiz Karışık Diyalog, Rastgele Suçlu Sistemi
-- =============================================

-- Varsa tabloları sırayla sil
DROP TABLE IF EXISTS PlayerActions;
DROP TABLE IF EXISTS GameSessions;
DROP TABLE IF EXISTS ScenarioHints;
DROP TABLE IF EXISTS NPCRelationships;
DROP TABLE IF EXISTS DialogLogs;
DROP TABLE IF EXISTS NPCDialogues;
DROP TABLE IF EXISTS Clues;
DROP TABLE IF EXISTS SceneObjects;
DROP TABLE IF EXISTS NPCs;

-- =============================================
-- NPCs Tablosu (Binalarla Eşleşir)
-- =============================================
CREATE TABLE NPCs (
    NPCId       INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    BuildingName TEXT NOT NULL,
    Role        TEXT NOT NULL,
    TrustLevel  INTEGER NOT NULL DEFAULT 50,
    FearLevel   INTEGER NOT NULL DEFAULT 30,
    IsGuilty    INTEGER NOT NULL DEFAULT 0,
    SecretInfo  TEXT NOT NULL DEFAULT '',
    IsActive    INTEGER NOT NULL DEFAULT 1,
    ImageFile   TEXT NOT NULL DEFAULT '',
    InteriorFile TEXT NOT NULL DEFAULT ''
);

-- =============================================
-- SceneObjects Tablosu (Point & Click Nesneleri)
-- =============================================
CREATE TABLE SceneObjects (
    ObjectId    INTEGER PRIMARY KEY AUTOINCREMENT,
    NPCId       INTEGER NOT NULL,
    ObjectName  TEXT NOT NULL,
    Description TEXT NOT NULL,
    ImageFile   TEXT NOT NULL DEFAULT '',
    PosTop      TEXT NOT NULL DEFAULT '50%',
    PosLeft     TEXT NOT NULL DEFAULT '50%',
    IsDiscovered INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY(NPCId) REFERENCES NPCs(NPCId) ON DELETE CASCADE
);

-- =============================================
-- NPCDialogues Tablosu — KADEMESİZ, KARIŞIK HAVUZ
-- Her NPC için 20 soru, difficulty ve category ile etiketli
-- =============================================
CREATE TABLE NPCDialogues (
    DialogueId   INTEGER PRIMARY KEY AUTOINCREMENT,
    NPCId        INTEGER NOT NULL,
    Difficulty   INTEGER NOT NULL DEFAULT 1,  -- 1=Kolay, 5=Çok Zor
    Category     TEXT NOT NULL DEFAULT 'tanisma', -- tanisma, derinlesme, yuzlestirme, baski, son
    ButtonIndex  INTEGER NOT NULL DEFAULT 0,
    PlayerText   TEXT NOT NULL,
    NPCResponse  TEXT NOT NULL,
    -- Suçlu NPC'ye göre alternatif cevaplar (JSON format)
    GuiltyResponses TEXT DEFAULT NULL,
    -- İlişkili ipucu ID'leri (virgülle ayrılmış)
    RelatedClueIds TEXT DEFAULT NULL,
    IsAccusatory INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY(NPCId) REFERENCES NPCs(NPCId) ON DELETE CASCADE
);

-- =============================================
-- DialogLogs Tablosu (Konuşma Geçmişi)
-- =============================================
CREATE TABLE DialogLogs (
    LogId           INTEGER PRIMARY KEY AUTOINCREMENT,
    NPCId           INTEGER NOT NULL,
    PlayerQuestion  TEXT NOT NULL,
    NPCResponse     TEXT NOT NULL,
    Difficulty      INTEGER NOT NULL DEFAULT 1,
    Category        TEXT DEFAULT 'tanisma',
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(NPCId) REFERENCES NPCs(NPCId) ON DELETE CASCADE
);

-- =============================================
-- NPCRelationships Tablosu (NPC'ler arası ilişkiler)
-- =============================================
CREATE TABLE NPCRelationships (
    RelationId  INTEGER PRIMARY KEY AUTOINCREMENT,
    NPC1Id      INTEGER NOT NULL,
    NPC2Id      INTEGER NOT NULL,
    RelationType TEXT NOT NULL DEFAULT 'neutral', -- ally, enemy, suspicious, neutral
    Description TEXT DEFAULT '',
    FOREIGN KEY(NPC1Id) REFERENCES NPCs(NPCId) ON DELETE CASCADE,
    FOREIGN KEY(NPC2Id) REFERENCES NPCs(NPCId) ON DELETE CASCADE
);

-- =============================================
-- ScenarioHints Tablosu (Suçlu NPC'ye göre ek ipuçları)
-- =============================================
CREATE TABLE ScenarioHints (
    HintId      INTEGER PRIMARY KEY AUTOINCREMENT,
    GuiltyNPCId INTEGER NOT NULL,
    HintText    TEXT NOT NULL,
    HintType    TEXT NOT NULL DEFAULT 'clue', -- clue, red_herring, confession
    RevealOrder INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY(GuiltyNPCId) REFERENCES NPCs(NPCId) ON DELETE CASCADE
);

-- =============================================
-- GameSessions Tablosu (Oyun oturumu takibi)
-- =============================================
CREATE TABLE GameSessions (
    SessionId   INTEGER PRIMARY KEY AUTOINCREMENT,
    GuiltyNPCId INTEGER NOT NULL,
    StartedAt   TEXT NOT NULL DEFAULT (datetime('now')),
    EndedAt     TEXT DEFAULT NULL,
    Result      TEXT DEFAULT NULL, -- 'won', 'lost', 'abandoned'
    AccusedNPCId INTEGER DEFAULT NULL,
    TotalQuestions INTEGER DEFAULT 0,
    CluesCollected INTEGER DEFAULT 0,
    FOREIGN KEY(GuiltyNPCId) REFERENCES NPCs(NPCId),
    FOREIGN KEY(AccusedNPCId) REFERENCES NPCs(NPCId)
);

-- =============================================
-- PlayerActions Tablosu (Oyuncu aksiyonları kaydı)
-- =============================================
CREATE TABLE PlayerActions (
    ActionId    INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionId   INTEGER NOT NULL,
    ActionType  TEXT NOT NULL, -- 'enter_building', 'collect_clue', 'ask_question', 'accuse'
    TargetId    INTEGER DEFAULT NULL,
    Details     TEXT DEFAULT NULL,
    CreatedAt   TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(SessionId) REFERENCES GameSessions(SessionId) ON DELETE CASCADE
);

-- =============================================
-- Varsayılan NPC Verileri (5 Şüpheli)
-- =============================================
INSERT INTO NPCs (Name, BuildingName, Role, TrustLevel, FearLevel, IsGuilty, SecretInfo, IsActive, ImageFile, InteriorFile)
VALUES
    ('Kasap Hasan',     'Kasap',     'Kasabadaki eski kasap, herkesin tanıdığı sert bir figür.',     50, 20, 0, 'Cinayet gecesi dükkânında gizlice birine et sattı. O kişi muhtar Kemal''di.', 1, 'images/hasan.png', 'images/butcher_interior.png'),
    ('Eczacı Selma',    'Eczane',    'Eczane sahibi, ilaç ve zehir konusunda uzman bir kadın.',      50, 45, 0, 'Kurbanın kullandığı ilacın yan etkilerini biliyordu ama gizledi.',           1, 'images/selma.png', 'images/apothecary_interior.png'),
    ('Muhtar Kemal',    'Muhtarlık', 'Kasabanın muhtarı, herkesin sırrını bilen bir politikacı.',     50, 60, 0, 'Kurbanla arazi anlaşmazlığı vardı ve onu tehdit etmişti.',                   1, 'images/kemal.png', 'images/town_hall_interior.png'),
    ('Komiser Güneş',   'Karakol',   'Kasabanın kadın polis komiseri, sarışın ve kararlı.',           50, 30, 0, 'Olay yerindeki bazı delilleri rapor etmeden sakladı.',                       1, 'images/gunes.png', 'images/police_interior.png'),
    ('Terzi Yahya',     'Terzi',     'Kasabanın yaşlı terzisi, kurbana son kıyafeti diken kişi.',     50, 35, 0, 'Kurbana gizli cepli bir ceket dikti, son gören kişi olabilir.',              1, 'images/yahya.png', 'images/tailor_interior.png');

-- =============================================
-- SceneObjects (Kasap - NPC 1)
-- =============================================
INSERT INTO SceneObjects (NPCId, ObjectName, Description, ImageFile, PosTop, PosLeft) VALUES
    (1, 'Kanlı Satır',      'Tezgaha sertçe saplanmış, üzerinde taze kan lekeleri olan paslı bir satır. Kan kurbanın kanıyla eşleşiyor olabilir.', 'images/bloody_cleaver.png', '40%', '30%'),
    (1, 'Kara Kaplı Defter', 'Veresiye listesinde kurbanın isminin üzeri kırmızı kalemle çizilmiş. Son sayfada şifreli notlar var.', 'images/black_notebook.png', '60%', '70%'),
    (1, 'Yırtık Önlük',     'Kavga izleri taşıyan, yakası kopmuş bir kasap önlüğü. Cebinde küçük bir anahtar var.', 'images/torn_apron.png', '80%', '20%');

-- =============================================
-- SceneObjects (Eczane - NPC 2)
-- =============================================
INSERT INTO SceneObjects (NPCId, ObjectName, Description, ImageFile, PosTop, PosLeft) VALUES
    (2, 'Boş İlaç Şişesi',    'Zehirli olduğu bilinen, reçetesiz satılmayan ağır bir ilacın boş şişesi. Parmak izleri silinmiş.', 'images/empty_medicine_bottle.png', '50%', '20%'),
    (2, 'Reçete Defteri',     'Kurbanın adının geçtiği, son sayfaları aceleyle yırtılmış defter. Yırtılan sayfalarda ne yazıyordu?', 'images/prescription_notebook.png', '70%', '80%'),
    (2, 'Zehirli Sarmaşık',  'Tezgah altında kurumaya bırakılmış zehirli bir bitki türü. Bu bitki ölümcül dozda kullanılabilir.', 'images/poison_ivy.png', '30%', '60%');

-- =============================================
-- SceneObjects (Muhtarlık - NPC 3)
-- =============================================
INSERT INTO SceneObjects (NPCId, ObjectName, Description, ImageFile, PosTop, PosLeft) VALUES
    (3, 'Tehdit Mektubu',    'Muhtarın çekmecesinde kurbana yazılmış, henüz gönderilmemiş bir tehdit mektubu. El yazısı titrek.', 'images/threat_letter.png', '60%', '40%'),
    (3, 'Kırık Gözlük',     'Kurbana ait olduğu düşünülen, camı kırık bir okuma gözlüğü. Muhtarın odasında ne işi var?', 'images/broken_glasses.png', '30%', '70%'),
    (3, 'Gizli Kasa',       'Tablonun arkasında şifresi açık unutulmuş para dolu kasa. İçinde sahte belgeler de var.', 'images/hidden_safe.png', '80%', '30%');

-- =============================================
-- SceneObjects (Karakol - NPC 4)
-- =============================================
INSERT INTO SceneObjects (NPCId, ObjectName, Description, ImageFile, PosTop, PosLeft) VALUES
    (4, 'Polis Rozeti',      'Olay yerinde bulunan, numarası kazınmış bir polis rozeti. Kime ait olduğu belirsiz.', 'images/police_badge.png', '45%', '25%'),
    (4, 'Gizli Dosya',      'Komiser Güneş''in masasında kilitli çekmecede bulunan, "GİZLİ" damgalı bir dosya. İçinde kurbanın geçmişiyle ilgili bilgiler var.', 'images/evidence_file.png', '35%', '65%'),
    (4, 'Kayıp Düğme',      'Olay yerinden toplanan, pahalı bir paltonun kopmuş düğmesi. Terzi Yahya''nın diktiği kumaşa benziyor.', 'images/missing_button.png', '70%', '45%');

-- =============================================
-- SceneObjects (Terzi - NPC 5)
-- =============================================
INSERT INTO SceneObjects (NPCId, ObjectName, Description, ImageFile, PosTop, PosLeft) VALUES
    (5, 'Kanlı İplik Makarası', 'Tezgahın altında bulunan, üzerinde kurumuş kan lekeleri olan iplik makarası. İplik rengi kurbanın ceketindekiyle aynı.', 'images/thread_spool.png', '55%', '30%'),
    (5, 'Yırtık Kumaş',        'Atölyede bulunan, kurbanın ceketinden kopmuş olabilecek kumaş parçası. Kenar dikişi Yahya''nın tarzına uyuyor.', 'images/torn_fabric.png', '40%', '75%'),
    (5, 'Gizli Cep',           'Yahya''nın son diktiği ceketin astarında gizli bir cep. İçinde küçük bir not: "Bu gece gel, konuşalım."', 'images/hidden_pocket.png', '75%', '50%');

-- =============================================
-- NPC İlişkileri
-- =============================================
INSERT INTO NPCRelationships (NPC1Id, NPC2Id, RelationType, Description) VALUES
    (1, 3, 'suspicious', 'Kasap Hasan, Muhtar Kemal''in cinayet gecesi et almaya geldiğini biliyor.'),
    (2, 4, 'enemy', 'Eczacı Selma, Komiser Güneş''in defterinden sayfaları aldığından şüpheleniyor.'),
    (3, 4, 'ally', 'Muhtar Kemal ve Komiser Güneş dosya kapatma konusunda işbirliği yapıyor.'),
    (2, 5, 'suspicious', 'Eczacı Selma, Terzi Yahya''yı cinayet gecesi kurbanın evine giderken görmüş.'),
    (1, 5, 'neutral', 'Kasap Hasan, Terzi Yahya''dan bıçak kılıfı diktirmiş.'),
    (3, 1, 'enemy', 'Muhtar Kemal, Kasap Hasan''ın kendisini suçlamasından rahatsız.');

-- =============================================
-- Senaryo İpuçları (Her suçlu için ek ipuçları)
-- =============================================
INSERT INTO ScenarioHints (GuiltyNPCId, HintText, HintType, RevealOrder) VALUES
    -- Kasap Hasan suçlu olduğunda
    (1, 'Kasabın soğuk hava deposundaki kan lekeleri kurbanınkiyle eşleşiyor.', 'clue', 1),
    (1, 'Veresiye defterindeki şifreli notlar aslında bir cinayet planı.', 'clue', 2),
    (1, 'Hasan''ın yırtık önlüğündeki kavga izleri cinayet gecesinden.', 'confession', 3),
    -- Eczacı Selma suçlu olduğunda
    (2, 'Zehirli sarmaşığın dozajı tam olarak kurbanın ölüm raporuyla uyuşuyor.', 'clue', 1),
    (2, 'Reçete defterinin yırtılan sayfalarında zehir formülü yazıyordu.', 'clue', 2),
    (2, 'Selma kurbanı yavaş yavaş zehirliyor, son dozu cinayet gecesi verdi.', 'confession', 3),
    -- Muhtar Kemal suçlu olduğunda
    (3, 'Tehdit mektubu gönderilmemiş ama muhtarın niyetini açıkça gösteriyor.', 'clue', 1),
    (3, 'Sahte arazi tapuları Kemal''in cinayet motifini ortaya koyuyor.', 'clue', 2),
    (3, 'Kemal cinayet gecesi kurbanın evine gidip tartışmış ve onu öldürmüş.', 'confession', 3),
    -- Komiser Güneş suçlu olduğunda
    (4, 'Polis rozeti aslında Güneş''in kendisine ait ve "kayıp" hikayesi uydurma.', 'clue', 1),
    (4, 'Gizli dosyada komiser''in kurbanla geçmişteki bağlantısı ortaya çıkıyor.', 'clue', 2),
    (4, 'Güneş delilleri karartarak kendi suçunu gizlemeye çalışıyor.', 'confession', 3),
    -- Terzi Yahya suçlu olduğunda
    (5, 'Kanlı iplik makarasındaki kan kurbanın kanıyla eşleşiyor.', 'clue', 1),
    (5, 'USB bellekte Yahya''nın kurbanla olan gizli anlaşması ve ihanet detayları var.', 'clue', 2),
    (5, 'Yahya cinayet gecesi kurbanın evine gidip tartışmış ve kazara öldürmüş.', 'confession', 3);

-- =============================================
-- NPCDialogues — KARIŞIK HAVUZ SİSTEMİ (Kademe yok!)
-- Tüm sorular difficulty ve category ile etiketli
-- GuiltyResponses JSON formatında: {"1":"cevap","2":"cevap",...}
-- =============================================

-- KASAP HASAN (NPC 1) - 20 Soru
INSERT INTO NPCDialogues (NPCId, Difficulty, Category, PlayerText, NPCResponse, RelatedClueIds) VALUES
(1, 1, 'tanisma', 'Cinayet gecesi neredeydin Hasan?', 'Buradaydım, dükkânımda. Gece geç saate kadar et doğruyordum. Kimsecikler yoktu ortalıkta, yağmur bardaktan boşalırcasına yağıyordu.', NULL),
(1, 1, 'tanisma', 'Kurbanı ne kadar iyi tanıyordun?', 'Osman Bey mi? Herkes tanır onu. İyi müşterimdi, her hafta gelirdi. Ama son zamanlarda arası bazılarıyla açılmıştı...', NULL),
(1, 1, 'tanisma', 'Kasabada düşmanı olan var mıydı?', 'Düşman mı? Ha, bir sürü... Muhtar Kemal''le arazi meselesinden dolayı birbirlerine giriyorlardı. Eczacı Selma da ondan pek hazzetmezdi.', '7,8'),
(1, 1, 'tanisma', 'Dükkânında şüpheli bir şey gördün mü?', 'Şüpheli mi? Ben sadece kasabım dedektif bey. Ama... o gece garip sesler duydum sokaktan.', NULL),
(1, 2, 'derinlesme', 'O gece duyduğun garip sesler neydi?', 'Bağrışma gibiydi... Ama yağmurdan net duyamadım. Saat gece yarısı civarıydı. Sonra bir araba kapısı çarpma sesi... Sonra sessizlik.', NULL),
(1, 2, 'derinlesme', 'Dükkânına gelen şüpheli biri oldu mu?', 'Cinayet gecesi muhtar Kemal geldi aslında. Gece vakti et istedi. Aceleyle aldı gitti. Garip buldum ama sormadım.', '7'),
(1, 2, 'derinlesme', 'Kurbanla son ne zaman konuştun?', 'Cinayet gününden bir gün önce geldi. "Yarın büyük bir para gelecek" dedi. Bir daha göremedim...', NULL),
(1, 2, 'derinlesme', 'Seni şüpheli görüyorlar, biliyor musun?', 'Ha! Beni mi? Ben niye öldüreyim müşterimi? Borcunu ödeyecekti, öldürsem para gider! Aklını kullan dedektif...', '1,2'),
(1, 3, 'yuzlestirme', 'Bu kanlı satır senin tezgahından çıktı!', 'O... o satır çalınmıştı! Bir hafta önce kayboldu, polise söyledim ama kimse ciddiye almadı! Birisi beni suçlu göstermek istiyor!', '1'),
(1, 3, 'yuzlestirme', 'Kara defterdeki kurbanın ismi neden çizili?', 'Veresiye borcunu ödeyeceğini söyledi diye çizdim! O kadar! Herkes veresiye defteri tutar!', '2'),
(1, 3, 'yuzlestirme', 'Yırtık önlüğündeki anahtar neyin anahtarı?', '*Terler* O... o anahtar arka odanın anahtarı. Soğuk hava deposu. İçinde sadece etler var...', '3'),
(1, 3, 'yuzlestirme', 'Muhtar cinayet gecesi sana geldiğini inkâr ediyor.', 'Yalancı! O gece buraya geldi, gözleri dönmüştü! Eğer inkâr ediyorsa gizleyecek bir şeyi var demektir!', '7'),
(1, 4, 'baski', 'Soğuk hava deposunda sadece et mi var?', '... İyi tamam. Orada eski belgeler de var. Kurbanın bazı evrakları... O bana emanet bırakmıştı.', '3,9'),
(1, 4, 'baski', 'Kurbanın sana emanet bıraktığı şey neydi?', 'Bir zarf... İçinde arazi tapuları vardı. Muhtarın üzerine kayıtlı arazilerin aslında kurbana ait olduğunu gösteren belgeler.', '9,7'),
(1, 4, 'baski', 'Neden bu belgeleri polise vermedin?', 'Korktum! Muhtar bu kasabada herkesin efendisi! Komiser Güneş zaten muhtarın adamı, kime güveneyim?', '10,11'),
(1, 4, 'baski', 'Terziden kurbanın ceketini aldığını biliyoruz.', 'Hayır! Ben terziye hiç gitmedim! ... Tamam, Yahya''dan bir bıçak kılıfı diktirdim ama kurbanla alakası yok!', '13,14'),
(1, 5, 'son', 'Son sözün nedir Hasan?', 'Ben masum bir kasabım! Evet, korkağım, belgeleri sakladım, ama kimseyi öldürmedim! Gerçek katili bulun!', NULL),
(1, 5, 'son', 'Katil kim sence?', 'Muhtar Kemal! Arazi meselesi yüzünden... Ama komiser de bu işin içinde olabilir. O gece karanlıkta bir kadın silueti gördüm...', '7,10'),
(1, 5, 'son', 'Söylemediklerin var mı hâlâ?', '*Uzun sessizlik* Eczacı Selma... O gece dükkânını geç kapattı. Pencereden ışık gördüm. Elinde bir şişe vardı...', '4,6'),
(1, 5, 'son', 'Masum olduğunu kanıtlayamazsan...', 'Emanet zarfı açın! İçindeki belgeler her şeyi anlatır! Ben sadece bir kasabım, korkak bir kasap...', '9');
