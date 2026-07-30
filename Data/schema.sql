-- =============================================
-- AI Destekli Dedektiflik RPG - Veritabanı Şeması (Point & Click + SQLite)
-- 5 NPC, 5 Kademe Diyalog, Rastgele Suçlu Sistemi
-- =============================================

-- Varsa tabloları sırayla sil
DROP TABLE IF EXISTS DialogLogs;
DROP TABLE IF EXISTS NPCDialogues;
DROP TABLE IF EXISTS Clues;
DROP TABLE IF EXISTS Dialogues;
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
-- NPCDialogues Tablosu (5 Kademe × 4 Buton × 5 NPC)
-- =============================================
CREATE TABLE NPCDialogues (
    DialogueId   INTEGER PRIMARY KEY AUTOINCREMENT,
    NPCId        INTEGER NOT NULL,
    Stage        INTEGER NOT NULL,
    ButtonIndex  INTEGER NOT NULL,
    PlayerText   TEXT NOT NULL,
    NPCResponse  TEXT NOT NULL,
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
    Stage           INTEGER NOT NULL DEFAULT 1,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(NPCId) REFERENCES NPCs(NPCId) ON DELETE CASCADE
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
-- NPCDialogues - KASAP HASAN (NPC 1) - 5 Kademe × 4 Buton
-- =============================================

-- Kademe 1: Tanışma
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(1, 1, 1, 'Cinayet gecesi neredeydin Hasan?', 'Buradaydım, dükkânımda. Gece geç saate kadar et doğruyordum. Kimsecikler yoktu ortalıkta, yağmur bardaktan boşalırcasına yağıyordu.'),
(1, 1, 2, 'Kurbanı ne kadar iyi tanıyordun?', 'Osman Bey mi? Herkes tanır onu. İyi müşterimdi, her hafta gelirdi. Ama son zamanlarda arası bazılarıyla açılmıştı...'),
(1, 1, 3, 'Kasabada düşmanı olan var mıydı?', 'Düşman mı? Ha, bir sürü... Muhtar Kemal''le arazi meselesinden dolayı birbirlerine giriyorlardı. Eczacı Selma da ondan pek hazzetmezdi.'),
(1, 1, 4, 'Bu dükkânda şüpheli bir şey görmüş olabilir misin?', 'Şüpheli mi? Ben sadece kasabım dedektif bey. Sabahtan akşama et doğrarım, başımı kaldırıp bakmam bile. Ama... o gece garip sesler duydum sokaktan.');

-- Kademe 2: Olay hakkında derinleşme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(1, 2, 1, 'O gece duyduğun garip sesler neydi?', 'Bağrışma gibiydi... Ama yağmurdan net duyamadım. Saat gece yarısı civarıydı. Sonra bir araba kapısı çarpma sesi... Sonra sessizlik.'),
(1, 2, 2, 'Son zamanlarda dükkânına gelen şüpheli biri oldu mu?', 'Şüpheli mi... Cinayet gecesi muhtar Kemal geldi aslında. Gece vakti et istedi. Aceleyle aldı gitti. Garip buldum ama sormadım.'),
(1, 2, 3, 'Kurbanla son ne zaman konuştun?', 'Cinayet gününden bir gün önce geldi. Veresiye defterindeki borcunu ödeyeceğini söyledi. "Yarın büyük bir para gelecek" dedi. Bir daha göremedim...'),
(1, 2, 4, 'Seni şüpheli olarak görüyorlar, bunu biliyor musun?', 'Ha! Beni mi? Ben niye öldüreyim müşterimi? Borcunu ödeyecekti, öldürsem para gider! Aklını kullan dedektif...');

-- Kademe 3: İpuçlarıyla yüzleştirme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(1, 3, 1, 'Bu kanlı satır senin tezgahından çıktı, ne diyorsun?', 'O... o satır çalınmıştı! Bir hafta önce kayboldu, polise söyledim ama kimse ciddiye almadı! Birisi beni suçlu göstermek istiyor!'),
(1, 3, 2, 'Kara defterdeki kurbanın üstü çizili ismini açıkla!', 'Veresiye borcunu ödeyeceğini söyledi diye çizdim! O kadar! Cinayet mi bu şimdi? Herkes veresiye defteri tutar!'),
(1, 3, 3, 'Yırtık önlüğün cebindeki anahtar neyin anahtarı?', '*Terler* O... o anahtar arka odanın anahtarı. Soğuk hava deposu. İçinde sadece etler var, inanmazsanız bakın.'),
(1, 3, 4, 'Muhtar Kemal cinayet gecesi sana geldiğini inkâr ediyor.', 'Yalancı! O gece buraya geldi, gözleri dönmüştü! "Et lazım, acil" dedi. Ben de verdim. Eğer inkâr ediyorsa gizleyecek bir şeyi var demektir!');

-- Kademe 4: Baskı altına alma
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(1, 4, 1, 'Soğuk hava deposunda sadece et mi var gerçekten?', '... İyi tamam. Orada eski belgeler de var. Kurbanın bazı evrakları... O bana emanet bırakmıştı, öleceğini biliyormuş gibi davranıyordu.'),
(1, 4, 2, 'Kurbanın sana emanet bıraktığı şey neydi?', 'Bir zarf... İçinde arazi tapuları vardı. Muhtar''ın üzerine kayıtlı arazilerin aslında kurbana ait olduğunu gösteren belgeler. "Başıma bir şey gelirse bunları polise ver" demişti.'),
(1, 4, 3, 'Neden bu belgeleri polise vermedin?', 'Korktum! Muhtar bu kasabada herkesin efendisi! Bilse beni de... *susar* Komiser Güneş zaten muhtarın adamı, kime güveneyim?'),
(1, 4, 4, 'Terziden kurbanın ceketini aldığını biliyoruz.', 'Ne? Hayır! Ben terziye hiç gitmedim! ... Tamam, Yahya''dan bir bıçak kılıfı diktirdim, ama kurbanla alakası yok!');

-- Kademe 5: Son sorular
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(1, 5, 1, 'Son sözün nedir Hasan?', 'Ben masum bir kasabım! Evet, korkağım, belgeleri sakladım, ama kimseyi öldürmedim! Muhtara sorun, eczacıya sorun... Gerçek katili bulun!'),
(1, 5, 2, 'Katil kim sence?', 'Muhtar Kemal! Arazi meselesi yüzünden... Ama komiser de bu işin içinde olabilir. O gece birilerini gördüm... Karanlıkta bir kadın silueti vardı sokakta.'),
(1, 5, 3, 'Söylemediklerin var mı hâlâ?', '*Uzun bir sessizlik* Eczacı Selma... O gece dükkânını geç kapattı. Pencereden ışık gördüm. Ve elinde bir şişe vardı... Ama emin değilim.'),
(1, 5, 4, 'Eğer masum olduğunu kanıtlayamazsan...', 'Emanet zarfı açın! İçindeki belgeler her şeyi anlatır! Ben sadece bir kasabım, korkak bir kasap... Ama katil değilim!');

-- =============================================
-- NPCDialogues - ECZACI SELMA (NPC 2) - 5 Kademe × 4 Buton
-- =============================================

-- Kademe 1: Tanışma
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(2, 1, 1, 'Cinayet gecesi eczaneniz açık mıydı?', 'Gece yarısına kadar açıktı. Envanter sayımı yapıyordum... Dışarıda yağmur yağıyordu, içeri müşteri falan gelmedi.'),
(2, 1, 2, 'Kurbanla ilişkiniz nasıldı?', 'Sadece müşterimdi. Düzenli ilaç alırdı, kronik bir rahatsızlığı vardı. Son zamanlarda daha sık geliyordu...'),
(2, 1, 3, 'Kasabada zehirlenme vakaları olduğunu duydunuz mu?', 'Ne zehirlenmesi? Ben eczacıyım, ilaç satarım! Zehir değil! Böyle iftiralar atılmasına tahammülüm yok!'),
(2, 1, 4, 'Kurbanın sağlık durumu hakkında bilginiz var mı?', 'Hasta bir adamdı. Kalp ilacı kullanıyordu. Ama son haftalarda reçetesiz bir ilaç daha istemeye başladı... Vermedim tabii.');

-- Kademe 2: Derinleşme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(2, 2, 1, 'Kurban reçetesiz hangi ilacı istedi?', 'Güçlü bir uyku ilacı istedi. Uykusuzluk çektiğini söyledi ama... O dozda alırsa kalp hastası için çok tehlikeli olurdu.'),
(2, 2, 2, 'Gece yarısına kadar neden açıktınız, gerçek sebep?', '... Birini bekliyordum tamam mı? Muhtar Kemal aradı, "Acil ilaç lazım, geç geleceğim" dedi. Ama gelmedi.'),
(2, 2, 3, 'Komiser Güneş sizi cinayet gecesi gördüğünü söylüyor.', 'Nerede görmüş? Ben dükkânımdan çıkmadım! Eğer öyle diyorsa yalan söylüyor... Ya da başka birini benimle karıştırdı.'),
(2, 2, 4, 'Kurbanın ölüm sebebi zehirlenme olabilir mi?', '*Yüzü solar* Zehirlenme mi? Bu... bu çok kötü. Hangi zehir? Ben hiçbir şey satmadım, yemin ederim!');

-- Kademe 3: İpuçlarıyla yüzleştirme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(2, 3, 1, 'Bu boş ilaç şişesindeki zehri kime sattın?', 'O... o ilacı ben kimseye satmadım! Şişe çalınmış olmalı! Belki biri gece dükkâna girdi ve aldı...'),
(2, 3, 2, 'Reçete defterinin son sayfasını neden yırttın?', '*Titreyerek* Orada... orada önemli bir not vardı. Kurbanın gerçek teşhisi... Eğer ortaya çıkarsa benim mesleki sorumluluğum...'),
(2, 3, 3, 'Tezgah altındaki zehirli sarmaşık ne için?', 'Tıbbi araştırma! Geleneksel tıpta kullanılır! Ben onu ilaç yapmak için yetiştiriyorum, zehir olarak değil!'),
(2, 3, 4, 'Kurbanın gerçek teşhisi neydi?', '*Uzun sessizlik* Osman Bey zehirleniyordu... Yavaş yavaş. Ama ben yapmadım! Birisi ona düzenli olarak küçük dozlarda zehir veriyordu. Ben bunu fark ettim ama... kanıtlayamadım.');

-- Kademe 4: Baskı
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(2, 4, 1, 'Neden polise söylemedin zehirlenme şüpheni?', 'Komiser Güneş''e söyledim! Ama ciddiye almadı. "Kanıtın var mı?" dedi, ben de... O defteri gösterdim. Sonra defter... bazı sayfaları kayboldu.'),
(2, 4, 2, 'Komiser defterin sayfalarını mı aldı?', 'Bilmiyorum! Ama o gün komiserin gelişinden sonra sayfalar yoktu. Belki tesadüftür... belki değildir.'),
(2, 4, 3, 'Muhtar neden seni arayıp ilaç istedi o gece?', 'Stres ilacı istedi. "Çok gerginim, uyuyamıyorum" dedi. Ama sesinde korku vardı... Normal bir ilaç isteği gibi değildi.'),
(2, 4, 4, 'Terzi Yahya''yla ilişkin nedir?', 'Yahya mı? Komşuyuz, bazen çay içeriz. Ama... Yahya kurbanın son günlerinde onu çok sık ziyaret etti. Bir şeyler dikiyordu, gizli gizli.');

-- Kademe 5: Son
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(2, 5, 1, 'Son sözün nedir Selma?', 'Ben eczacıyım, insanları iyileştirmek için çalışıyorum! Evet, şüphemi sakladım, ama korktum! Bu kasabada kim kime güvenecek?'),
(2, 5, 2, 'Katil kim sence?', 'Bilmiyorum... Ama muhtar ve komiser arasında bir bağ var. Cinayet gecesi muhtar et almak için kasaba gitti, komiser ise olay yerini çok geç inceledi...'),
(2, 5, 3, 'Sakladığın başka bir şey var mı?', 'Cinayet gecesi... Pencereden Terzi Yahya''yı gördüm. Elinde bir paket vardı ve kurbanın evine doğru gidiyordu. Saat 11 civarıydı.'),
(2, 5, 4, 'Bu zehirli bitkiyi kurban için mi kullandın?', 'HAYIR! O bitki deneysel ilaç çalışmam için! Ben birini zehirlemek istesem daha etkili yollar bilirim! ... Şey, yani teorik olarak...');

-- =============================================
-- NPCDialogues - MUHTAR KEMAL (NPC 3) - 5 Kademe × 4 Buton
-- =============================================

-- Kademe 1: Tanışma
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(3, 1, 1, 'Muhtar bey, cinayet gecesi neredeydiniz?', 'Evimdeydim tabii ki. Televizyon izledim, sonra uyudum. Muhtarın gece sokakta ne işi olur?'),
(3, 1, 2, 'Kurbanla aranızdaki ilişki nasıldı?', 'Normal komşuluk ilişkisi. Bazen anlaşamadığımız konular oldu ama bu normaldir. Siyasette düşman olmak cinayet sebebi değildir.'),
(3, 1, 3, 'Kasabada gerginliğin sebebi nedir?', 'Arazi meseleleri... Belediye yeni yol geçirecek, bazı araziler kamulaştırılacak. Herkes pay kapmaya çalışıyor. Osman da bunlardan biriydi.'),
(3, 1, 4, 'Kurbanın ölümü sana yaradı diyorlar.', 'Kim diyor? Kimin diline düşmüşüm? Ben muhtarım, herkese eşit davranırım! Osman''ın ölümü bana hiçbir şey kazandırmadı!');

-- Kademe 2: Derinleşme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(3, 2, 1, 'Arazi meselesi hakkında daha fazla bilgi ver.', 'Kurbanın arazisi yolun tam üzerinde kalıyor. Kamulaştırma bedeli çok yüksek olacaktı. Ama Osman satmak istemiyordu... İnatçı adamdı.'),
(3, 2, 2, 'Cinayet gecesi kasaba gidip et aldığın doğru mu?', '*Duraksır* Kim söyledi bunu? Kasap Hasan mı? O... o gece sadece kısa bir yürüyüşe çıktım. Evet, kasabın önünden geçtim ama et almadım!'),
(3, 2, 3, 'Eczacı Selma seni aradığını söylüyor o gece.', 'Hayır! Ben kimseyi aramadım! Selma yanlış hatırlıyor... Ya da bilerek yalan söylüyor. Neden bilmem ama kadına güvenilmez.'),
(3, 2, 4, 'Kurbanla son görüşmeniz ne zamandı?', 'Cinayet gününden iki gün önce. Buraya geldi, bağırdı çağırdı. "Arazimi vermeyeceğim, mahkemeye giderim!" dedi. Ben de sakin ol dedim...');

-- Kademe 3: İpuçlarıyla yüzleştirme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(3, 3, 1, 'Bu tehdit mektubunu sen yazdın, çekmecende bulduk!', '*Yüzü kızarır* Bu... bunu sinirle yazdım! Göndermeyecektim! Herkes kızgınken bir şeyler yazar, bu cinayet kanıtı değil!'),
(3, 3, 2, 'Kurbanın kırık gözlüğü senin odanda ne işi var?', 'Kavga ettiğimizde düştü! Kırdım evet, ama sonra pişman oldum. Geri verecektim... Artık veremem tabii.'),
(3, 3, 3, 'Kasadaki sahte belgeler neyin nesi?', '*Terler* Onlar... eski belediye evrakları. Bazen prosedürler hızlansın diye bazı belgeler... düzenlenir. Suç değil, bürokratik zorunluluk.'),
(3, 3, 4, 'Kasap Hasan cinayet gecesi geldiğini kanıtlayabilir.', 'Tamam! Evet, kasaba gittim! Et aldım! Ama bu beni katil yapmaz! Bir adam et almak için dışarı çıkamaz mı?!');

-- Kademe 4: Baskı
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(3, 4, 1, 'Gece yarısı et almak için mi çıktın gerçekten?', '... Tamam, et bahaneydi. Kurbanın evinin önünden geçmek istedim. Durumu kontrol etmek... Tehdit mektubu yazdığım için vicdan azabı çekiyordum.'),
(3, 4, 2, 'Kurbanın evinde ne gördün?', 'Işıklar yanıyordu. Bir gölge gördüm pencerede... Kurban değildi. Başka birisi vardı orada. Ama kim olduğunu göremedim, yağmur çok şiddetliydi.'),
(3, 4, 3, 'Komiser Güneş''le ilişkin nedir?', 'Komiser devletin memuru, ben muhtarım. Resmi ilişkimiz var... *duraksır* Bazen bazı dosyaların kapanması konusunda ortak çalışırız. Hepsi bu.'),
(3, 4, 4, 'Arazi tapuları aslında kurbanın üzerineymiş, bunu biliyor muydun?', '*Şok olur* Ne?! Tapular... Kim söyledi bunu?! O araziler yasal olarak belediyeye aittir! Osman''ın iddiaları asılsızdı!');

-- Kademe 5: Son
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(3, 5, 1, 'Son sözün nedir Kemal?', 'Ben bu kasabanın muhtarıyım! 20 yıldır hizmet ediyorum. Evet, hatalarım oldu ama kimseyi öldürmedim! O tapular sahte, birisi beni tuzağa düşürüyor!'),
(3, 5, 2, 'Katil kim sence?', 'Kasap Hasan! O satırla... Ama belki de eczacı. O kadının ne zehirler bildiğini düşünün! Ya da terzi... Kurbanı en son gören o!'),
(3, 5, 3, 'Söylemediklerin var mı?', '*İç çeker* Komiser Güneş... O gece beni aradı. "Muhtar, bir sorun var, evinde kal" dedi. Neden böyle dediğini hiç sormadım... Sormam gerekiyordu.'),
(3, 5, 4, 'Kurbanın evindeki gölge kim olabilir?', 'Uzun boylu biriydi... Erkek mi kadın mı emin değilim. Ama terzi Yahya''nın boyu uzundur... Ve o gece herkes bir yerlere gidiyordu bu kasabada.');

-- =============================================
-- NPCDialogues - KOMİSER GÜNEŞ (NPC 4) - 5 Kademe × 4 Buton
-- =============================================

-- Kademe 1: Tanışma
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(4, 1, 1, 'Komiser, olay yerine ilk gelen siz miydiniz?', 'Evet, saat 02:30 civarında ihbar aldık. 10 dakika içinde oradaydım. Ceset meydanda yatıyordu, yağmur tüm izleri siliyordu.'),
(4, 1, 2, 'Kurban hakkında ne biliyorsunuz?', 'Osman Bey, 58 yaşında, tüccar. Kasabada tanınan bir isim. Bazı arazi anlaşmazlıkları dışında bilinen bir düşmanı yoktu... Resmi olarak.'),
(4, 1, 3, 'Cinayet gecesi siz neredeydiniz ihbardan önce?', 'Karakolda nöbetteydim. Evrak işleriyle uğraşıyordum. Yağmurlu gecelerde genelde sakin olur kasaba...'),
(4, 1, 4, 'İlk bulgular neler?', 'Kafa travması. Sert bir cisimle vurulmuş. Ölüm saati gece 23:00 ile 01:00 arası. Olay yerinde az sayıda fiziksel delil vardı.');

-- Kademe 2: Derinleşme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(4, 2, 1, 'Olay yerinde hangi delilleri buldunuz?', 'Bir düğme, bazı ayak izleri... Yağmur çoğunu sildi. Standart prosedür uyguladık. Daha fazla detay... dosyada yazıyor.'),
(4, 2, 2, 'Neden bir dış dedektif çağrıldı?', '*Rahatsız olur* Üst makamın kararı. Ben gayet iyi yürütüyordum soruşturmayı ama... "Tarafsız göz lazım" dediler. Kasabada herkes birbirini tanıyor.'),
(4, 2, 3, 'Muhtar Kemal''le ilişkiniz profesyonel mi?', 'Tabii ki profesyonel! Muhtar resmi makam, ben de polis. Raporlarımı düzenli sunarım. Başka bir ilişki yok!'),
(4, 2, 4, 'Eczacı Selma zehirlenme şüphesini size bildirmiş miydi?', '*Duraksır* Selma mı söyledi bunu? O... bir keresinde "Osman Bey''in kan değerleri garip" gibi bir şey demişti ama somut kanıt yoktu.');

-- Kademe 3: İpuçlarıyla yüzleştirme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(4, 3, 1, 'Bu polis rozeti olay yerinde bulundu, numarası kazınmış!', '*Yüzü değişir* Bu rozet... Kayıp olarak rapor edilmişti. Bir ay önce karakoldan çalındı. Kim aldıysa olay yerine bırakmış.'),
(4, 3, 2, 'Gizli dosyadaki bilgiler neden rapor edilmedi?', 'O dosya... devam eden bir soruşturmanın parçası! Her şeyi kamuoyuyla paylaşamam. Prosedür gereği gizli kalması gereken bilgiler var!'),
(4, 3, 3, 'Bu düğme terzi Yahya''nın diktiği bir paltoya ait.', 'İlginç... Yahya''dan bu düğmenin kime ait olduğunu sorduk ama net bir cevap vermedi. "Çok müşterim var" dedi.'),
(4, 3, 4, 'Eczacının defterinden sayfaları siz mi aldınız?', 'NE?! Ben kimsenin defterinden sayfa almadım! Bu çok ciddi bir itham! Kanıtınız var mı?');

-- Kademe 4: Baskı
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(4, 4, 1, 'O gece muhtarı neden arayıp evinde kalmasını söylediniz?', '*Şaşırır* Muhtar... bunu mu söyledi? Ben onu güvenlik için uyardım! İhbar gelmişti, herkesin güvende olmasını istedim!'),
(4, 4, 2, 'İhbar gelmeden ÖNCE muhtarı aradığınız kanıtlandı.', '*Uzun sessizlik* ... Tamam. Birisi beni aradı o gece. Tanımadığım bir numara. "Meydanda bir şey olacak, muhtar ve Kemal''i uyar" dedi. Ciddiye aldım.'),
(4, 4, 3, 'Delilleri karartma şüpheniz var, bunu biliyorsunuz.', 'Karartma mı?! Ben 15 yıllık polisim! Evet, bazı bilgileri gizli tuttum ama bu prosedür gereği! Her şeyi dosyaya koydum!'),
(4, 4, 4, 'Olay yerinden başka neler topladınız ama rapora yazmadınız?', '*Masaya bakar* Bir mektup... Kurbanın cebinden çıktı. "Beni takip ediyorlar, bu gece her şeyi açıklayacağım" yazıyordu. Bunu... henüz rapora eklememistim.');

-- Kademe 5: Son
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(4, 5, 1, 'Son sözünüz nedir Komiser?', 'Ben görevimi yaptım! Evet, prosedür hataları oldu ama kasabadaki herkes birbirini tanıyor. Tarafsız kalmak zor... Ama ben adaletin yanındayım.'),
(4, 5, 2, 'Katil kim sizce?', 'Kanıtlar muhtarı gösteriyor... Arazi meselesi, tehdit mektubu, o gece dışarı çıkması. Ama kasap da şüpheli. O satır tesadüf olamaz.'),
(4, 5, 3, 'Sakladığınız başka bilgi var mı?', 'Kurbanın cebindeki mektupta bir isim daha vardı... Terzi Yahya. "Yahya her şeyi biliyor" yazıyordu. Bu ne anlama geliyor bilmiyorum.'),
(4, 5, 4, 'Sizi de şüpheli listesine ekliyorum.', '*Sertleşir* Bu sizin hakkınız. Ama ben bu kasabayı korumak için buradayım. Gerçek katili bulun, o zaman benim masumiyetimi de görürsünüz.');

-- =============================================
-- NPCDialogues - TERZİ YAHYA (NPC 5) - 5 Kademe × 4 Buton
-- =============================================

-- Kademe 1: Tanışma
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(5, 1, 1, 'Yahya usta, cinayet gecesi neredeydin?', 'Dükkânımdaydım, gece geç saate kadar dikiş dikiyordum. Sipariş yetişmesi lazımdı. Makinenin sesinden başka bir şey duymadım.'),
(5, 1, 2, 'Kurbanı tanıyor muydun?', 'Tabii, eski müşterimdi. Son birkaç haftadır sık geliyordu. Özel bir ceket sipariş etmişti... Teslim edemedim, vaktinden önce öldü.'),
(5, 1, 3, 'Kasabadaki gerginliklerden haberin var mı?', 'Ben terziyim, kumaşla uğraşırım. İnsanların kavgalarına karışmam. Ama... son zamanlarda herkes gergindi, o doğru.'),
(5, 1, 4, 'Dükkânında ilginç bir şey fark ettin mi son günlerde?', 'İlginç mi? Hayır, her şey normal... Dikişler, kumaşlar, müşteriler. Sıradan günler. *Gözlerini kaçırır*');

-- Kademe 2: Derinleşme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(5, 2, 1, 'Kurbanın sipariş ettiği ceket nasıl bir ceketti?', 'Özel bir ceket... Gizli cepleri olan, astarı kalın. "İçine belgeler koyacağım" demişti. Ne belgeleri olduğunu sormadım.'),
(5, 2, 2, 'Kurban sana belge saklatmak mı istedi?', 'Hayır hayır! Sadece cekete cep dikeyim dedi. Belgeleri kendisi koyacaktı. Ben sadece terziyim, insanların işlerine karışmam!'),
(5, 2, 3, 'Cinayet gecesi dükkânından çıktın mı?', '*Duraksır* ... Bir kez çıktım. Sigara içmeye. Ama hemen döndüm. 10 dakika bile sürmedi. Sadece taze hava aldım.'),
(5, 2, 4, 'Eczacı Selma seni cinayet gecesi kurbanın evine giderken gördü.', 'Selma mı?! O... yanılmış olmalı! Ben sadece sigara içmeye çıktım! Kurbanın evine neden gideyim? *Elleri titrer*');

-- Kademe 3: İpuçlarıyla yüzleştirme
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(5, 3, 1, 'Bu kanlı iplik makarası senin dükkânından çıktı!', 'Kan mı?! İmkânsız! Ben kumaş keserken bazen elimi keserim, o benim kanım olabilir! Başka bir açıklaması yok!'),
(5, 3, 2, 'Bu yırtık kumaş parçası kurbanın ceketinden kopmuş.', '*Yutkunur* O... o kumaş bende vardı evet. Ceketi dikerken artan parça. Ama bu delil değil, her terzi artık kumaş saklar!'),
(5, 3, 3, 'Gizli cepteki not "Bu gece gel, konuşalım" yazıyor. Bu senin el yazın!', '*Terlemeye başlar* Ben... o notu ben yazdım evet. Kurban benimle konuşmak istedi! Gizli bir şey anlatacaktı ama... varamadım!'),
(5, 3, 4, 'Neden varamadın? Yola çıktığını biliyoruz.', 'Çıktım evet! Ama yarı yolda döndüm! Korktum... O gece sokaklar çok karanlıktı ve birini gördüm... Gölge gibi bir figür. Korkup geri döndüm!');

-- Kademe 4: Baskı
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(5, 4, 1, 'Gördüğün gölge kim olabilir?', 'Bilmiyorum! Karanlıktı, yağmur yağıyordu... Ama uzun bir palto giyiyordu. Belki dedektif paltosu gibi... Ya da muhtarın paltosu.'),
(5, 4, 2, 'Kasap Hasan''a bıçak kılıfı diktin mi?', '*Gözleri büyür* Kim söyledi bunu?! ... Evet, diktim. Ama bu normal bir sipariş! Kasap bıçak kılıfı kullanır, ne var bunda?'),
(5, 4, 3, 'Kurbanın sana anlattığı gizli şey neydi?', 'Tam anlatmadı... Ama "Bu kasabada herkes bir şeyler gizliyor, sen de dikkat et Yahya" dedi. Tapulardan, belgelerden bahsetti. Detay vermeden öldü.'),
(5, 4, 4, 'Komiserin gizli dosyasında senin adın geçiyor.', 'BENİM ADIM MI?! Ne... ne yazıyor o dosyada?! Ben hiçbir suç işlemedim! Sadece elbise dikerim! *Panik yapar*');

-- Kademe 5: Son
INSERT INTO NPCDialogues (NPCId, Stage, ButtonIndex, PlayerText, NPCResponse) VALUES
(5, 5, 1, 'Son sözün nedir Yahya?', 'Ben masum bir terziyim! Evet, kurbanla görüşmeye çalıştım ama varamadım! O gece herkes sokaktaydı... Kim yaptıysa, benden daha güçlü biriydi.'),
(5, 5, 2, 'Katil kim sence?', 'Muhtar Kemal! Arazi meselesi yüzünden... Hem o gece dışarıdaydı, kasaba gitti. Ya da komiser... O kadın bir şeyler saklıyor, gözlerinden belli.'),
(5, 5, 3, 'Sakladığın son bir şey var mı?', '*Gözyaşları* Kurbanın ceketi... Bitmiş haliyle asıldı duvarda. Teslim edemedim. İçindeki gizli cepte... bir USB bellek var. Onu kimseye vermedim. Korkuyorum.'),
(5, 5, 4, 'USB bellekte ne var?', 'Bilmiyorum! Açmadım! Kurban "Eğer başıma bir şey gelirse bunu doğru kişiye ver" demişti. Ama doğru kişi kim? Polise güvenemiyorum, muhtara güvenemiyorum...');
