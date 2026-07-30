// =============================================================
// 🔍 DEDEKTİFLİK RPG - TAM OYUN MOTORU
// 5 NPC, 5 Kademe Diyalog, Rastgele Suçlu, Buldum Sistemi
// =============================================================

// === DOM ELEMENTS ===
const splashScreen = document.getElementById('splash-screen');
const storyIntroScreen = document.getElementById('story-intro-screen');
const townMapScreen = document.getElementById('town-map-screen');
const interiorScreen = document.getElementById('interior-screen');
const clueInspectModal = document.getElementById('clue-inspect-modal');
const npcTalkModal = document.getElementById('npc-talk-modal');
const bagModal = document.getElementById('bag-modal');
const foundModal = document.getElementById('found-modal');
const npcHistoryModal = document.getElementById('npc-history-modal');
const jailOverlay = document.getElementById('jail-overlay');
const resultModal = document.getElementById('result-modal');
const exitWarningModal = document.getElementById('exit-warning-modal');
const transitionOverlay = document.getElementById('transition-overlay');

// === GAME STATE ===
let currentBag = [];
const MAX_BAG_SIZE = 10;
let activeNpcId = null;
let currentPendingObject = null;
let visitedBuildings = new Set();
let dialogHistory = {}; // { npcId: [{player, npc, stage}] }
let currentTalkStage = 1;
let guiltyNpcId = null;
let npcTalkCompleted = {}; // { npcId: true/false }

// === GAME DATA (Client-side, SQL benzeri) ===
const NPC_DATA = {
    1: { id: 1, name: 'Kasap Hasan', building: 'Kasap', role: 'Kasabadaki eski kasap', img: 'images/hasan.png', bg: 'images/butcher_interior.png', secret: 'Cinayet gecesi dükkânında gizlice muhtara et sattı.' },
    2: { id: 2, name: 'Eczacı Selma', building: 'Eczane', role: 'Eczane sahibi', img: 'images/selma.png', bg: 'images/apothecary_interior.png', secret: 'Kurbanın zehirlendiğini biliyordu ama gizledi.' },
    3: { id: 3, name: 'Muhtar Kemal', building: 'Muhtarlık', role: 'Kasabanın muhtarı', img: 'images/kemal.png', bg: 'images/town_hall_interior.png', secret: 'Kurbanla arazi anlaşmazlığı vardı.' },
    4: { id: 4, name: 'Komiser Güneş', building: 'Karakol', role: 'Kadın polis komiseri', img: 'images/gunes.png', bg: 'images/police_interior.png', secret: 'Olay yerindeki delilleri sakladı.' },
    5: { id: 5, name: 'Terzi Yahya', building: 'Terzi', role: 'Kasabanın terzisi', img: 'images/yahya.png', bg: 'images/tailor_interior.png', secret: 'Kurbana gizli cepli ceket dikti, son gören kişi.' }
};

const SCENE_OBJECTS = {
    1: [
        { id: 1, name: 'Kanlı Satır', desc: 'Tezgaha sertçe saplanmış, üzerinde taze kan lekeleri olan paslı bir satır. Kan kurbanın kanıyla eşleşiyor olabilir.', top: '40%', left: '30%', img: 'images/bloody_cleaver.png' },
        { id: 2, name: 'Kara Kaplı Defter', desc: 'Veresiye listesinde kurbanın isminin üzeri kırmızı kalemle çizilmiş. Son sayfada şifreli notlar var.', top: '60%', left: '70%', img: 'images/black_notebook.png' },
        { id: 3, name: 'Yırtık Önlük', desc: 'Kavga izleri taşıyan, yakası kopmuş bir kasap önlüğü. Cebinde küçük bir anahtar var.', top: '78%', left: '18%', img: 'images/torn_apron.png' }
    ],
    2: [
        { id: 4, name: 'Boş İlaç Şişesi', desc: 'Zehirli olduğu bilinen, reçetesiz satılmayan ağır bir ilacın boş şişesi. Parmak izleri silinmiş.', top: '50%', left: '20%', img: 'images/empty_medicine_bottle.png' },
        { id: 5, name: 'Reçete Defteri', desc: 'Kurbanın adının geçtiği, son sayfaları aceleyle yırtılmış defter.', top: '68%', left: '75%', img: 'images/prescription_notebook.png' },
        { id: 6, name: 'Zehirli Sarmaşık', desc: 'Tezgah altında kurumaya bırakılmış zehirli bir bitki türü. Ölümcül dozda kullanılabilir.', top: '30%', left: '60%', img: 'images/poison_ivy.png' }
    ],
    3: [
        { id: 7, name: 'Tehdit Mektubu', desc: 'Muhtarın çekmecesinde kurbana yazılmış, henüz gönderilmemiş bir tehdit mektubu. El yazısı titrek.', top: '58%', left: '38%', img: 'images/threat_letter.png' },
        { id: 8, name: 'Kırık Gözlük', desc: 'Kurbana ait olduğu düşünülen, camı kırık bir okuma gözlüğü.', top: '32%', left: '68%', img: 'images/broken_glasses.png' },
        { id: 9, name: 'Gizli Kasa', desc: 'Tablonun arkasında şifresi açık unutulmuş para dolu kasa. İçinde sahte belgeler de var.', top: '78%', left: '28%', img: 'images/hidden_safe.png' }
    ],
    4: [
        { id: 10, name: 'Polis Rozeti', desc: 'Olay yerinde bulunan, numarası kazınmış bir polis rozeti. Kime ait olduğu belirsiz.', top: '45%', left: '25%', img: 'images/police_badge.png' },
        { id: 11, name: 'Gizli Dosya', desc: '"GİZLİ" damgalı bir dosya. İçinde kurbanın geçmişiyle ilgili bilgiler var.', top: '35%', left: '65%', img: 'images/evidence_file.png' },
        { id: 12, name: 'Kayıp Düğme', desc: 'Pahalı bir paltonun kopmuş düğmesi. Terzi Yahya\'nın diktiği kumaşa benziyor.', top: '70%', left: '45%', img: 'images/missing_button.png' }
    ],
    5: [
        { id: 13, name: 'Kanlı İplik Makarası', desc: 'Üzerinde kurumuş kan lekeleri olan iplik makarası. İplik rengi kurbanın ceketindekiyle aynı.', top: '55%', left: '30%', img: 'images/thread_spool.png' },
        { id: 14, name: 'Yırtık Kumaş', desc: 'Kurbanın ceketinden kopmuş olabilecek kumaş parçası.', top: '40%', left: '72%', img: 'images/torn_fabric.png' },
        { id: 15, name: 'Gizli Cep', desc: 'Yahya\'nın diktiği ceketin astarında gizli bir cep. İçinde: "Bu gece gel, konuşalım."', top: '75%', left: '50%', img: 'images/hidden_pocket.png' }
    ]
};

// 5 NPC × 5 Kademe × 4 Buton = 100 Diyalog
const NPC_DIALOGUES = {
    1: { // Kasap Hasan
        1: [
            { q: 'Cinayet gecesi neredeydin Hasan?', a: 'Buradaydım, dükkânımda. Gece geç saate kadar et doğruyordum. Kimsecikler yoktu ortalıkta, yağmur bardaktan boşalırcasına yağıyordu.' },
            { q: 'Kurbanı ne kadar iyi tanıyordun?', a: 'Osman Bey mi? Herkes tanır onu. İyi müşterimdi, her hafta gelirdi. Ama son zamanlarda arası bazılarıyla açılmıştı...' },
            { q: 'Kasabada düşmanı olan var mıydı?', a: 'Düşman mı? Ha, bir sürü... Muhtar Kemal\'le arazi meselesinden dolayı birbirlerine giriyorlardı. Eczacı Selma da ondan pek hazzetmezdi.' },
            { q: 'Dükkânında şüpheli bir şey gördün mü?', a: 'Şüpheli mi? Ben sadece kasabım dedektif bey. Ama... o gece garip sesler duydum sokaktan.' }
        ],
        2: [
            { q: 'O gece duyduğun garip sesler neydi?', a: 'Bağrışma gibiydi... Ama yağmurdan net duyamadım. Saat gece yarısı civarıydı. Sonra bir araba kapısı çarpma sesi... Sonra sessizlik.' },
            { q: 'Dükkânına gelen şüpheli biri oldu mu?', a: 'Cinayet gecesi muhtar Kemal geldi aslında. Gece vakti et istedi. Aceleyle aldı gitti. Garip buldum ama sormadım.' },
            { q: 'Kurbanla son ne zaman konuştun?', a: 'Cinayet gününden bir gün önce geldi. "Yarın büyük bir para gelecek" dedi. Bir daha göremedim...' },
            { q: 'Seni şüpheli görüyorlar, biliyor musun?', a: 'Ha! Beni mi? Ben niye öldüreyim müşterimi? Borcunu ödeyecekti, öldürsem para gider! Aklını kullan dedektif...' }
        ],
        3: [
            { q: 'Bu kanlı satır senin tezgahından çıktı!', a: 'O... o satır çalınmıştı! Bir hafta önce kayboldu, polise söyledim ama kimse ciddiye almadı! Birisi beni suçlu göstermek istiyor!' },
            { q: 'Kara defterdeki kurbanın ismi neden çizili?', a: 'Veresiye borcunu ödeyeceğini söyledi diye çizdim! O kadar! Herkes veresiye defteri tutar!' },
            { q: 'Yırtık önlüğündeki anahtar neyin anahtarı?', a: '*Terler* O... o anahtar arka odanın anahtarı. Soğuk hava deposu. İçinde sadece etler var...' },
            { q: 'Muhtar cinayet gecesi sana geldiğini inkâr ediyor.', a: 'Yalancı! O gece buraya geldi, gözleri dönmüştü! Eğer inkâr ediyorsa gizleyecek bir şeyi var demektir!' }
        ],
        4: [
            { q: 'Soğuk hava deposunda sadece et mi var?', a: '... İyi tamam. Orada eski belgeler de var. Kurbanın bazı evrakları... O bana emanet bırakmıştı.' },
            { q: 'Kurbanın sana emanet bıraktığı şey neydi?', a: 'Bir zarf... İçinde arazi tapuları vardı. Muhtarın üzerine kayıtlı arazilerin aslında kurbana ait olduğunu gösteren belgeler.' },
            { q: 'Neden bu belgeleri polise vermedin?', a: 'Korktum! Muhtar bu kasabada herkesin efendisi! Komiser Güneş zaten muhtarın adamı, kime güveneyim?' },
            { q: 'Terziden kurbanın ceketini aldığını biliyoruz.', a: 'Hayır! Ben terziye hiç gitmedim! ... Tamam, Yahya\'dan bir bıçak kılıfı diktirdim ama kurbanla alakası yok!' }
        ],
        5: [
            { q: 'Son sözün nedir Hasan?', a: 'Ben masum bir kasabım! Evet, korkağım, belgeleri sakladım, ama kimseyi öldürmedim! Gerçek katili bulun!' },
            { q: 'Katil kim sence?', a: 'Muhtar Kemal! Arazi meselesi yüzünden... Ama komiser de bu işin içinde olabilir. O gece karanlıkta bir kadın silueti gördüm...' },
            { q: 'Söylemediklerin var mı hâlâ?', a: '*Uzun sessizlik* Eczacı Selma... O gece dükkânını geç kapattı. Pencereden ışık gördüm. Elinde bir şişe vardı...' },
            { q: 'Masum olduğunu kanıtlayamazsan...', a: 'Emanet zarfı açın! İçindeki belgeler her şeyi anlatır! Ben sadece bir kasabım, korkak bir kasap...' }
        ]
    },
    2: { // Eczacı Selma
        1: [
            { q: 'Cinayet gecesi eczaneniz açık mıydı?', a: 'Gece yarısına kadar açıktı. Envanter sayımı yapıyordum... Dışarıda yağmur yağıyordu, içeri müşteri gelmedi.' },
            { q: 'Kurbanla ilişkiniz nasıldı?', a: 'Sadece müşterimdi. Düzenli ilaç alırdı, kronik bir rahatsızlığı vardı. Son zamanlarda daha sık geliyordu...' },
            { q: 'Kasabada zehirlenme vakaları olduğunu duydunuz mu?', a: 'Ne zehirlenmesi? Ben eczacıyım, ilaç satarım! Zehir değil! Böyle iftiralar atılmasına tahammülüm yok!' },
            { q: 'Kurbanın sağlık durumu hakkında bilginiz var mı?', a: 'Hasta bir adamdı. Kalp ilacı kullanıyordu. Ama son haftalarda reçetesiz bir ilaç daha istemeye başladı...' }
        ],
        2: [
            { q: 'Kurban reçetesiz hangi ilacı istedi?', a: 'Güçlü bir uyku ilacı. Uykusuzluk çektiğini söyledi ama... O dozda kalp hastası için çok tehlikeli olurdu.' },
            { q: 'Gece yarısına kadar neden açıktınız?', a: '... Birini bekliyordum tamam mı? Muhtar Kemal aradı, "Acil ilaç lazım, geç geleceğim" dedi. Ama gelmedi.' },
            { q: 'Komiser Güneş sizi cinayet gecesi gördüğünü söylüyor.', a: 'Nerede görmüş? Ben dükkânımdan çıkmadım! Eğer öyle diyorsa yalan söylüyor...' },
            { q: 'Kurbanın ölüm sebebi zehirlenme olabilir mi?', a: '*Yüzü solar* Zehirlenme mi? Bu... bu çok kötü. Ben hiçbir şey satmadım, yemin ederim!' }
        ],
        3: [
            { q: 'Bu boş ilaç şişesindeki zehri kime sattın?', a: 'O... o ilacı ben kimseye satmadım! Şişe çalınmış olmalı! Belki biri gece dükkâna girdi ve aldı...' },
            { q: 'Reçete defterinin son sayfasını neden yırttın?', a: '*Titreyerek* Orada önemli bir not vardı. Kurbanın gerçek teşhisi... Mesleki sorumluluğum...' },
            { q: 'Tezgah altındaki zehirli sarmaşık ne için?', a: 'Tıbbi araştırma! Geleneksel tıpta kullanılır! Ben onu ilaç yapmak için yetiştiriyorum, zehir olarak değil!' },
            { q: 'Kurbanın gerçek teşhisi neydi?', a: '*Uzun sessizlik* Osman Bey zehirleniyordu... Yavaş yavaş. Ama ben yapmadım! Birisi ona düzenli olarak küçük dozlarda zehir veriyordu.' }
        ],
        4: [
            { q: 'Neden polise söylemedin zehirlenme şüpheni?', a: 'Komiser Güneş\'e söyledim! Ama ciddiye almadı. Defteri gösterdim. Sonra... bazı sayfaları kayboldu.' },
            { q: 'Komiser defterin sayfalarını mı aldı?', a: 'Bilmiyorum! Ama o gün komiserin gelişinden sonra sayfalar yoktu. Belki tesadüftür... belki değildir.' },
            { q: 'Muhtar neden seni arayıp ilaç istedi o gece?', a: 'Stres ilacı istedi. "Çok gerginim, uyuyamıyorum" dedi. Ama sesinde korku vardı... Normal değildi.' },
            { q: 'Terzi Yahya\'yla ilişkin nedir?', a: 'Yahya mı? Komşuyuz, bazen çay içeriz. Ama... Yahya kurbanın son günlerinde onu çok sık ziyaret etti.' }
        ],
        5: [
            { q: 'Son sözün nedir Selma?', a: 'Ben eczacıyım, insanları iyileştirmek için çalışıyorum! Evet, şüphemi sakladım ama korktum!' },
            { q: 'Katil kim sence?', a: 'Muhtar ve komiser arasında bir bağ var. Cinayet gecesi muhtar et almaya gitti, komiser olay yerini geç inceledi...' },
            { q: 'Sakladığın başka bir şey var mı?', a: 'Cinayet gecesi... Pencereden Terzi Yahya\'yı gördüm. Elinde bir paket vardı ve kurbanın evine doğru gidiyordu.' },
            { q: 'Bu zehirli bitkiyi kurban için mi kullandın?', a: 'HAYIR! O bitki deneysel ilaç çalışmam için! Ben birini zehirlemek istesem... şey, yani teorik olarak...' }
        ]
    },
    3: { // Muhtar Kemal
        1: [
            { q: 'Muhtar bey, cinayet gecesi neredeydiniz?', a: 'Evimdeydim tabii ki. Televizyon izledim, sonra uyudum. Muhtarın gece sokakta ne işi olur?' },
            { q: 'Kurbanla aranızdaki ilişki nasıldı?', a: 'Normal komşuluk. Bazen anlaşamadığımız konular oldu ama siyasette düşman olmak cinayet sebebi değildir.' },
            { q: 'Kasabada gerginliğin sebebi nedir?', a: 'Arazi meseleleri... Belediye yeni yol geçirecek, bazı araziler kamulaştırılacak. Herkes pay kapmaya çalışıyor.' },
            { q: 'Kurbanın ölümü sana yaradı diyorlar.', a: 'Kim diyor? Kimin diline düşmüşüm? Osman\'ın ölümü bana hiçbir şey kazandırmadı!' }
        ],
        2: [
            { q: 'Arazi meselesi hakkında detay ver.', a: 'Kurbanın arazisi yolun tam üzerinde. Kamulaştırma bedeli çok yüksek olacaktı. Osman satmak istemiyordu...' },
            { q: 'Cinayet gecesi kasaba gidip et aldığın doğru mu?', a: '*Duraksır* Kim söyledi? Kasap Hasan mı? Sadece kısa bir yürüyüşe çıktım. Kasabın önünden geçtim ama et almadım!' },
            { q: 'Eczacı Selma seni aradığını söylüyor o gece.', a: 'Hayır! Ben kimseyi aramadım! Selma yanlış hatırlıyor... Ya da bilerek yalan söylüyor.' },
            { q: 'Kurbanla son görüşmeniz ne zamandı?', a: 'Cinayet gününden iki gün önce. Bağırdı çağırdı. "Arazimi vermeyeceğim, mahkemeye giderim!" dedi.' }
        ],
        3: [
            { q: 'Bu tehdit mektubunu sen yazdın, çekmecende bulduk!', a: '*Yüzü kızarır* Bunu sinirle yazdım! Göndermeyecektim! Herkes kızgınken bir şeyler yazar!' },
            { q: 'Kurbanın kırık gözlüğü senin odanda ne işi var?', a: 'Kavga ettiğimizde düştü! Kırdım evet, ama sonra pişman oldum. Geri verecektim...' },
            { q: 'Kasadaki sahte belgeler neyin nesi?', a: '*Terler* Onlar... eski belediye evrakları. Bazen prosedürler hızlansın diye bazı belgeler... düzenlenir.' },
            { q: 'Kasap Hasan cinayet gecesi geldiğini kanıtlayabilir.', a: 'Tamam! Evet, kasaba gittim! Et aldım! Ama bu beni katil yapmaz!' }
        ],
        4: [
            { q: 'Gece yarısı et almak için mi çıktın gerçekten?', a: '... Tamam, et bahaneydi. Kurbanın evinin önünden geçmek istedim. Tehdit mektubu yazdığım için vicdan azabı çekiyordum.' },
            { q: 'Kurbanın evinde ne gördün?', a: 'Işıklar yanıyordu. Bir gölge gördüm pencerede... Kurban değildi. Başka birisi vardı orada.' },
            { q: 'Komiser Güneş\'le ilişkin nedir?', a: 'Resmi ilişkimiz var... *duraksır* Bazen bazı dosyaların kapanması konusunda ortak çalışırız. Hepsi bu.' },
            { q: 'Arazi tapuları aslında kurbanın üzerineymiş.', a: '*Şok olur* Ne?! Tapular... Kim söyledi bunu?! O araziler yasal olarak belediyeye aittir!' }
        ],
        5: [
            { q: 'Son sözün nedir Kemal?', a: 'Ben bu kasabanın muhtarıyım! 20 yıldır hizmet ediyorum. Evet hatalarım oldu ama kimseyi öldürmedim!' },
            { q: 'Katil kim sence?', a: 'Kasap Hasan! O satırla... Ama belki eczacı. O kadının ne zehirler bildiğini düşünün! Ya da terzi...' },
            { q: 'Söylemediklerin var mı?', a: '*İç çeker* Komiser Güneş... O gece beni aradı. "Muhtar, evinde kal" dedi. Neden böyle dediğini hiç sormadım...' },
            { q: 'Kurbanın evindeki gölge kim olabilir?', a: 'Uzun boylu biriydi... Terzi Yahya\'nın boyu uzundur... O gece herkes bir yerlere gidiyordu bu kasabada.' }
        ]
    },
    4: { // Komiser Güneş
        1: [
            { q: 'Komiser, olay yerine ilk gelen siz miydiniz?', a: 'Evet, saat 02:30 civarında ihbar aldık. 10 dakika içinde oradaydım. Ceset meydanda yatıyordu, yağmur izleri siliyordu.' },
            { q: 'Kurban hakkında ne biliyorsunuz?', a: 'Osman Bey, 58 yaşında, tüccar. Kasabada tanınan bir isim. Arazi anlaşmazlıkları dışında bilinen düşmanı yoktu...' },
            { q: 'Cinayet gecesi siz neredeydiniz?', a: 'Karakolda nöbetteydim. Evrak işleriyle uğraşıyordum. Yağmurlu gecelerde genelde sakin olur kasaba...' },
            { q: 'İlk bulgular neler?', a: 'Kafa travması. Sert bir cisimle vurulmuş. Ölüm saati 23:00-01:00 arası. Olay yerinde az sayıda fiziksel delil vardı.' }
        ],
        2: [
            { q: 'Olay yerinde hangi delilleri buldunuz?', a: 'Bir düğme, bazı ayak izleri... Yağmur çoğunu sildi. Standart prosedür uyguladık.' },
            { q: 'Neden dış dedektif çağrıldı?', a: '*Rahatsız olur* Üst makamın kararı. "Tarafsız göz lazım" dediler. Kasabada herkes birbirini tanıyor.' },
            { q: 'Muhtar Kemal\'le ilişkiniz profesyonel mi?', a: 'Tabii ki profesyonel! Muhtar resmi makam, ben de polis. Başka bir ilişki yok!' },
            { q: 'Eczacı Selma zehirlenme şüphesini bildirmiş miydi?', a: '*Duraksır* Selma mı söyledi? Bir keresinde "kan değerleri garip" gibi bir şey demişti ama somut kanıt yoktu.' }
        ],
        3: [
            { q: 'Bu polis rozeti olay yerinde bulundu!', a: '*Yüzü değişir* Kayıp olarak rapor edilmişti. Bir ay önce karakoldan çalındı. Kim aldıysa olay yerine bırakmış.' },
            { q: 'Gizli dosyadaki bilgiler neden rapor edilmedi?', a: 'O dosya devam eden bir soruşturmanın parçası! Her şeyi kamuoyuyla paylaşamam. Prosedür gereği gizli!' },
            { q: 'Bu düğme terzi Yahya\'nın diktiği paltoya ait.', a: 'İlginç... Yahya\'dan düğmenin kime ait olduğunu sorduk ama net cevap vermedi.' },
            { q: 'Eczacının defterinden sayfaları siz mi aldınız?', a: 'NE?! Ben kimsenin defterinden sayfa almadım! Bu çok ciddi bir itham! Kanıtınız var mı?' }
        ],
        4: [
            { q: 'O gece muhtarı neden arayıp evinde kalmasını söylediniz?', a: '*Şaşırır* Muhtar bunu mu söyledi? Ben onu güvenlik için uyardım! İhbar gelmişti!' },
            { q: 'İhbar gelmeden ÖNCE muhtarı aradığınız kanıtlandı.', a: '*Uzun sessizlik* ... Birisi beni aradı. Tanımadığım numara. "Meydanda bir şey olacak" dedi. Ciddiye aldım.' },
            { q: 'Delilleri karartma şüpheniz var.', a: 'Karartma mı?! 15 yıllık polisim! Evet, bazı bilgileri gizli tuttum ama prosedür gereği!' },
            { q: 'Olay yerinden rapora yazmadığınız neler var?', a: '*Masaya bakar* Bir mektup... Kurbanın cebinden. "Beni takip ediyorlar, bu gece her şeyi açıklayacağım" yazıyordu...' }
        ],
        5: [
            { q: 'Son sözünüz nedir Komiser?', a: 'Ben görevimi yaptım! Prosedür hataları oldu ama tarafsız kalmak zor... Adaletin yanındayım.' },
            { q: 'Katil kim sizce?', a: 'Kanıtlar muhtarı gösteriyor... Arazi meselesi, tehdit mektubu, o gece dışarı çıkması. Ama kasap da şüpheli.' },
            { q: 'Sakladığınız başka bilgi var mı?', a: 'Kurbanın cebindeki mektupta: "Yahya her şeyi biliyor" yazıyordu. Ne anlama geliyor bilmiyorum.' },
            { q: 'Sizi de şüpheli listesine ekliyorum.', a: '*Sertleşir* Bu sizin hakkınız. Ama gerçek katili bulun, o zaman masumiyetimi görürsünüz.' }
        ]
    },
    5: { // Terzi Yahya
        1: [
            { q: 'Yahya usta, cinayet gecesi neredeydin?', a: 'Dükkânımdaydım, gece geç saate kadar dikiş dikiyordum. Sipariş yetişmesi lazımdı. Makinenin sesinden başka bir şey duymadım.' },
            { q: 'Kurbanı tanıyor muydun?', a: 'Eski müşterimdi. Son birkaç haftadır sık geliyordu. Özel bir ceket sipariş etmişti... Teslim edemedim.' },
            { q: 'Kasabadaki gerginliklerden haberin var mı?', a: 'Ben terziyim, kumaşla uğraşırım. İnsanların kavgalarına karışmam. Ama... son zamanlarda herkes gergindi.' },
            { q: 'Dükkânında ilginç bir şey fark ettin mi?', a: 'İlginç mi? Hayır, her şey normal... Dikişler, kumaşlar, müşteriler. *Gözlerini kaçırır*' }
        ],
        2: [
            { q: 'Kurbanın sipariş ettiği ceket nasıl bir ceketti?', a: 'Özel bir ceket... Gizli cepleri olan, astarı kalın. "İçine belgeler koyacağım" demişti.' },
            { q: 'Kurban sana belge saklatmak mı istedi?', a: 'Hayır! Sadece cekete cep dikeyim dedi. Belgeleri kendisi koyacaktı. Ben sadece terziyim!' },
            { q: 'Cinayet gecesi dükkânından çıktın mı?', a: '*Duraksır* Bir kez çıktım. Sigara içmeye. Ama hemen döndüm. 10 dakika bile sürmedi.' },
            { q: 'Eczacı Selma seni kurbanın evine giderken gördü.', a: 'Selma mı?! Yanılmış olmalı! Sadece sigara içmeye çıktım! *Elleri titrer*' }
        ],
        3: [
            { q: 'Bu kanlı iplik makarası senin dükkânından!', a: 'Kan mı?! İmkânsız! Kumaş keserken elimi keserim, o benim kanım olabilir!' },
            { q: 'Bu yırtık kumaş kurbanın ceketinden kopmuş.', a: '*Yutkunur* O kumaş bende vardı evet. Ceketi dikerken artan parça. Her terzi artık kumaş saklar!' },
            { q: 'Gizli cepteki not "Bu gece gel, konuşalım" yazıyor. Senin el yazın!', a: '*Terler* Ben yazdım evet. Kurban benimle konuşmak istedi! Gizli bir şey anlatacaktı ama... varamadım!' },
            { q: 'Neden varamadın? Yola çıktığını biliyoruz.', a: 'Çıktım evet! Ama yarı yolda döndüm! Korktum... Gölge gibi bir figür gördüm. Korkup geri döndüm!' }
        ],
        4: [
            { q: 'Gördüğün gölge kim olabilir?', a: 'Bilmiyorum! Karanlıktı, yağmur yağıyordu... Uzun bir palto giyiyordu. Belki muhtarın paltosu.' },
            { q: 'Kasap Hasan\'a bıçak kılıfı diktin mi?', a: '*Gözleri büyür* Kim söyledi?! Evet diktim. Normal bir sipariş! Kasap bıçak kılıfı kullanır!' },
            { q: 'Kurbanın sana anlattığı gizli şey neydi?', a: 'Tam anlatmadı. "Bu kasabada herkes bir şeyler gizliyor, dikkat et Yahya" dedi. Tapulardan, belgelerden bahsetti.' },
            { q: 'Komiserin gizli dosyasında senin adın geçiyor.', a: 'BENİM ADIM MI?! Ne yazıyor?! Ben hiçbir suç işlemedim! Sadece elbise dikerim! *Panik yapar*' }
        ],
        5: [
            { q: 'Son sözün nedir Yahya?', a: 'Ben masum bir terziyim! Kurbanla görüşmeye çalıştım ama varamadım! O gece herkes sokaktaydı...' },
            { q: 'Katil kim sence?', a: 'Muhtar Kemal! Arazi meselesi yüzünden... Ya da komiser. O kadın bir şeyler saklıyor, gözlerinden belli.' },
            { q: 'Sakladığın son bir şey var mı?', a: '*Gözyaşları* Kurbanın ceketi... Bitmiş haliyle duvarda asılı. İçindeki gizli cepte bir USB bellek var. Kimseye vermedim.' },
            { q: 'USB bellekte ne var?', a: 'Bilmiyorum! Açmadım! Kurban "Başıma bir şey gelirse bunu doğru kişiye ver" demişti. Doğru kişi kim?' }
        ]
    }
};

// =============================================================
// GAME INITIALIZATION
// =============================================================

function initGame() {
    currentBag = [];
    visitedBuildings = new Set();
    dialogHistory = {};
    npcTalkCompleted = {};
    currentTalkStage = 1;
    activeNpcId = null;
    
    // Suçluyu backend API'den sıfırla (Frontend artık bilmiyor!)
    fetch('/api/game/reset', { method: 'POST' })
        .then(res => res.json())
        .then(data => console.log('🔍 API:', data.message))
        .catch(err => console.error('API Error:', err));
    
    // Haritadaki binaları resetle
    document.querySelectorAll('.map-building').forEach(b => {
        b.classList.remove('visited');
    });
}

initGame();

// =============================================================
// 1. SPLASH → STORY INTRO
// =============================================================

document.getElementById('start-btn').addEventListener('click', () => {
    // Sesleri başlat
    const bgMusic = document.getElementById('bg-music');
    const rainSound = document.getElementById('rain-sound');
    if (bgMusic) { bgMusic.volume = 0.3; bgMusic.play().catch(e=>console.log(e)); }
    if (rainSound) { rainSound.volume = 0.5; rainSound.play().catch(e=>console.log(e)); }

    triggerTransition(() => {
        splashScreen.classList.add('hidden');
        storyIntroScreen.classList.remove('hidden');
        startTypewriter();
    });
});

const STORY_TEXT = 'Yağmurlu bir sonbahar gecesi... Kasabanın meydanında bir ceset bulundu. Kurban, herkesin tanıdığı tüccar Osman Bey\'di. Parke taşların üzerinde yatan cansız beden, yağmurun altında solgun bir ışıkla aydınlanıyordu. Polis şeridinin arkasında toplanan kalabalık, birbirlerine şüpheyle bakıyordu. Kasabanın en deneyimli dedektifi olarak bu davayı çözmek için buraya çağrıldınız. Beş şüpheli, beş bina, sayısız sır... Gerçeği ortaya çıkarabilecek misiniz?';

function startTypewriter() {
    const el = document.getElementById('story-typewriter');
    const continueBtn = document.getElementById('story-continue-btn');
    const typeSound = document.getElementById('typewriter-sound');
    if (typeSound) { typeSound.volume = 0.4; typeSound.play().catch(e=>console.log(e)); }

    el.textContent = '';
    let i = 0;
    const speed = 40;
    
    function type() {
        if (i < STORY_TEXT.length) {
            el.textContent += STORY_TEXT.charAt(i);
            i++;
            setTimeout(type, speed);
        } else {
            if (typeSound) typeSound.pause();
            continueBtn.classList.remove('hidden');
        }
    }
    type();
}

// Story → Town Map
document.getElementById('story-continue-btn').addEventListener('click', () => {
    triggerTransition(() => {
        storyIntroScreen.classList.add('hidden');
        townMapScreen.classList.remove('hidden');
    });
});

// =============================================================
// 2. TOWN MAP — BUILDING CLICKS
// =============================================================

document.querySelectorAll('.map-building').forEach(b => {
    b.addEventListener('click', () => {
        if (b.classList.contains('visited')) return;
        const npcId = parseInt(b.getAttribute('data-npc-id'));
        openBuilding(npcId);
    });
});

function openBuilding(npcId) {
    activeNpcId = npcId;
    const npc = NPC_DATA[npcId];
    if (!npc) return;
    
    interiorScreen.style.backgroundImage = `url('${npc.bg}')`;
    document.getElementById('talk-npc-name').innerText = npc.name + ' ile Konuş';
    
    // Load Hotspots
    const container = document.getElementById('hotspots-container');
    container.innerHTML = '';
    const objects = SCENE_OBJECTS[npcId] || [];
    objects.forEach(obj => {
        const wrapper = document.createElement('div');
        wrapper.className = 'hotspot-wrapper';
        wrapper.style.position = 'absolute';
        wrapper.style.top = obj.top;
        wrapper.style.left = obj.left;
        
        const img = document.createElement('img');
        img.src = obj.img;
        img.className = 'hotspot-img';
        img.alt = obj.name;
        
        const label = document.createElement('span');
        label.className = 'hotspot-label';
        label.textContent = obj.name;
        
        wrapper.appendChild(img);
        wrapper.appendChild(label);
        wrapper.addEventListener('click', () => openClueInspect(obj, npcId));
        container.appendChild(wrapper);
    });
    
    // Kapı sesi
    const doorCreak = document.getElementById('door-creak');
    if (doorCreak) { doorCreak.volume = 0.7; doorCreak.currentTime = 0; doorCreak.play().catch(e=>console.log(e)); }

    triggerTransition(() => {
        townMapScreen.classList.add('hidden');
        interiorScreen.classList.remove('hidden');
    });
}

// =============================================================
// 3. CLUE INSPECTION (YENİ TASARIM)
// =============================================================

function openClueInspect(obj, npcId) {
    currentPendingObject = obj;
    const npc = NPC_DATA[npcId];
    
    document.getElementById('clue-inspect-title').textContent = obj.name;
    document.getElementById('clue-inspect-desc').textContent = obj.desc;
    document.getElementById('clue-inspect-img').src = obj.img;
    document.getElementById('clue-inspect-bg').style.backgroundImage = `url('${npc.bg}')`;
    
    clueInspectModal.classList.remove('hidden');
}

document.getElementById('clue-take-btn').addEventListener('click', () => {
    if (currentBag.length >= MAX_BAG_SIZE) {
        alert('Çantanız doldu!');
    } else if (currentPendingObject && !currentBag.find(b => b.id === currentPendingObject.id)) {
        currentBag.push(currentPendingObject);
    }
    clueInspectModal.classList.add('hidden');
});

document.getElementById('clue-leave-btn').addEventListener('click', () => {
    clueInspectModal.classList.add('hidden');
});

// =============================================================
// 4. LEAVE BUILDING (WARNING)
// =============================================================

document.getElementById('leave-building-btn').addEventListener('click', () => {
    exitWarningModal.classList.remove('hidden');
});

document.getElementById('exit-cancel-btn').addEventListener('click', () => {
    exitWarningModal.classList.add('hidden');
});

document.getElementById('exit-confirm-btn').addEventListener('click', () => {
    exitWarningModal.classList.add('hidden');
    // Binayı kilitle
    if (activeNpcId) {
        visitedBuildings.add(activeNpcId);
        const buildingEl = document.querySelector(`.map-building[data-npc-id="${activeNpcId}"]`);
        if (buildingEl) buildingEl.classList.add('visited');
    }
    triggerTransition(() => {
        interiorScreen.classList.add('hidden');
        townMapScreen.classList.remove('hidden');
    });
});

// =============================================================
// 5. NPC TALK (5 KADEMELİ SİSTEM)
// =============================================================

document.getElementById('talk-npc-btn').addEventListener('click', () => {
    if (!activeNpcId) return;
    openNpcTalk(activeNpcId);
});

function openNpcTalk(npcId) {
    const npc = NPC_DATA[npcId];
    currentTalkStage = 1;
    
    // NPC görselini arka plan olarak ayarla
    document.getElementById('npc-talk-bg').style.backgroundImage = `url('${npc.img}')`;
    
    // Chat alanını temizle
    document.getElementById('npc-talk-chat').innerHTML = '';
    
    // Stage göstergesini güncelle
    updateStageIndicator();
    
    // Diyalog butonlarını yükle
    loadTalkButtons(npcId, currentTalkStage);
    
    npcTalkModal.classList.remove('hidden');
}

function updateStageIndicator() {
    document.getElementById('npc-talk-stage').textContent = `Kademe ${currentTalkStage}/5`;
}

function loadTalkButtons(npcId, stage) {
    const container = document.getElementById('npc-talk-buttons');
    container.innerHTML = '';
    
    if (stage > 5) {
        // Konuşma bitti
        container.innerHTML = '<div class="npc-talk-end-msg"><i class="fa-solid fa-check-circle"></i> Sorgu tamamlandı. Geri dönebilirsiniz.</div>';
        npcTalkCompleted[npcId] = true;
        return;
    }
    
    const dialogues = NPC_DIALOGUES[npcId]?.[stage];
    if (!dialogues) return;
    
    dialogues.forEach((d, idx) => {
        const btn = document.createElement('button');
        btn.className = 'npc-talk-btn';
        btn.innerHTML = `<i class="fa-solid fa-comment-dots"></i> ${d.q}`;
        btn.addEventListener('click', () => handleTalkChoice(npcId, stage, idx));
        container.appendChild(btn);
    });
}

function handleTalkChoice(npcId, stage, choiceIndex) {
    const npc = NPC_DATA[npcId];
    const dialogue = NPC_DIALOGUES[npcId]?.[stage]?.[choiceIndex];
    if (!dialogue) return;
    
    const chatArea = document.getElementById('npc-talk-chat');
    
    // Oyuncu mesajı
    const playerMsg = document.createElement('div');
    playerMsg.className = 'npc-talk-message player';
    playerMsg.innerHTML = `<div class="speaker">Dedektif</div><div class="msg-text">${dialogue.q}</div>`;
    chatArea.appendChild(playerMsg);
    
    // Butonları geçici devre dışı bırak
    document.querySelectorAll('.npc-talk-btn').forEach(b => b.disabled = true);
    
    // NPC cevabı (gecikmeli)
    setTimeout(() => {
        const npcMsg = document.createElement('div');
        npcMsg.className = 'npc-talk-message';
        npcMsg.innerHTML = `<div class="speaker">${npc.name}</div><div class="msg-text">${dialogue.a}</div>`;
        chatArea.appendChild(npcMsg);
        chatArea.scrollTop = chatArea.scrollHeight;
        
        // Konuşma geçmişine kaydet
        if (!dialogHistory[npcId]) dialogHistory[npcId] = [];
        dialogHistory[npcId].push({
            player: dialogue.q,
            npc: dialogue.a,
            stage: stage,
            npcName: npc.name
        });
        
        // Sonraki kademeye geç
        currentTalkStage = stage + 1;
        updateStageIndicator();
        
        setTimeout(() => {
            loadTalkButtons(npcId, currentTalkStage);
        }, 500);
        
    }, 800);
}

document.getElementById('npc-talk-close').addEventListener('click', () => {
    npcTalkModal.classList.add('hidden');
});

// =============================================================
// 6. BAG (ÇANTA)
// =============================================================

function openBag() {
    const bagList = document.getElementById('bag-items-list');
    if (currentBag.length === 0) {
        bagList.innerHTML = '<p style="color: var(--text-muted); text-align:center; padding:30px;">Çanta boş. Binalardaki ipuçlarını toplayın.</p>';
    } else {
        bagList.innerHTML = currentBag.map(b => `
            <div class="bag-item">
                <img src="${b.img}" alt="${b.name}">
                <span>${b.name}</span>
            </div>
        `).join('');
    }
    
    // Not Defterini Yükle
    const notebook = document.getElementById('detective-notes');
    if (notebook) {
        notebook.value = localStorage.getItem('detectiveNotes') || '';
    }

    bagModal.classList.remove('hidden');
}

// Notları kaydetme
document.getElementById('detective-notes')?.addEventListener('input', (e) => {
    localStorage.setItem('detectiveNotes', e.target.value);
});

document.getElementById('open-bag-btn')?.addEventListener('click', openBag);
document.getElementById('interior-bag-btn')?.addEventListener('click', openBag);
document.getElementById('close-bag-btn').addEventListener('click', () => bagModal.classList.add('hidden'));

// =============================================================
// 7. BULDUM! (SUÇLAMA SİSTEMİ)
// =============================================================

document.getElementById('found-btn').addEventListener('click', () => {
    renderFoundScreen();
    foundModal.classList.remove('hidden');
});

document.getElementById('found-close-btn').addEventListener('click', () => {
    foundModal.classList.add('hidden');
});

function renderFoundScreen() {
    const grid = document.getElementById('found-npc-cards');
    grid.innerHTML = '';
    
    for (let id = 1; id <= 5; id++) {
        const npc = NPC_DATA[id];
        const hasHistory = dialogHistory[id] && dialogHistory[id].length > 0;
        
        const card = document.createElement('div');
        card.className = 'found-npc-card';
        card.innerHTML = `
            <img src="${npc.img}" alt="${npc.name}" class="found-npc-img" data-npc-id="${id}" title="Konuşma geçmişini görüntüle">
            <div class="found-npc-name">${npc.name}</div>
            <div class="found-npc-role">${npc.building}</div>
            ${hasHistory ? `<div style="font-size:0.75rem; color:var(--accent);">${dialogHistory[id].length} konuşma kaydı</div>` : '<div style="font-size:0.75rem; color:var(--text-muted);">Henüz konuşulmadı</div>'}
            <button class="btn btn-outline" style="font-size:0.75rem; padding: 6px 12px; width:100%;" onclick="window.showNpcHistory(${id})"><i class="fa-solid fa-comments"></i> Konuşma Geçmişi</button>
            <div class="found-npc-actions">
                <button class="btn btn-danger" onclick="accuseNpc(${id})"><i class="fa-solid fa-handcuffs"></i> Suçlu</button>
                <button class="btn btn-success" onclick="innocentNpc(${id})"><i class="fa-solid fa-shield-halved"></i> Masum</button>
            </div>
        `;
        grid.appendChild(card);
    }
    
    // NPC görsel tıklanınca konuşma geçmişi
    grid.querySelectorAll('.found-npc-img').forEach(img => {
        img.addEventListener('click', () => {
            const npcId = parseInt(img.getAttribute('data-npc-id'));
            showNpcHistory(npcId);
        });
    });
}

window.showNpcHistory = function(npcId) {
    const npc = NPC_DATA[npcId];
    const history = dialogHistory[npcId] || [];
    
    document.getElementById('npc-history-title').innerHTML = `<i class="fa-solid fa-comments"></i> ${npc.name} — Konuşma Geçmişi`;
    
    const content = document.getElementById('npc-history-content');
    if (history.length === 0) {
        content.innerHTML = '<p style="color: var(--text-muted); text-align:center; padding:30px;">Bu NPC ile henüz konuşulmadı.</p>';
    } else {
        content.innerHTML = history.map(h => `
            <div class="history-msg player-msg">
                <div class="h-speaker">Dedektif</div>
                <div>${h.player}</div>
            </div>
            <div class="history-msg npc-msg">
                <div class="h-speaker">${h.npcName}</div>
                <div>${h.npc}</div>
            </div>
        `).join('');
    }
    
    npcHistoryModal.classList.remove('hidden');
}

document.getElementById('npc-history-close').addEventListener('click', () => {
    npcHistoryModal.classList.add('hidden');
});

// === ACCUSE NPC (BACKEND CHECK) ===
window.accuseNpc = function(npcId) {
    const npc = NPC_DATA[npcId];
    foundModal.classList.add('hidden');
    
    // Hapis animasyonu göster
    document.getElementById('jail-npc-img').src = npc.img;
    document.getElementById('jail-npc-name').textContent = npc.name;
    jailOverlay.classList.remove('hidden');
    
    // Animasyon sırasında API'ye sor
    fetch('/api/game/accuse', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ NpcId: npcId })
    })
    .then(res => res.json())
    .then(data => {
        setTimeout(() => {
            jailOverlay.classList.add('hidden');
            
            const resultIcon = document.getElementById('result-icon');
            const resultTitle = document.getElementById('result-title');
            const resultMessage = document.getElementById('result-message');
            const retryBtn = document.getElementById('result-retry-btn');
            
            if (data.success) {
                resultIcon.className = 'result-icon success';
                resultIcon.innerHTML = '<i class="fa-solid fa-trophy"></i>';
                resultTitle.textContent = '🎉 TEBRİKLER! KAZANDINIZ!';
                resultTitle.style.color = 'var(--success)';
                resultMessage.textContent = data.message + ' Gizli Bilgi: ' + (data.secret || npc.secret);
                retryBtn.innerHTML = '<i class="fa-solid fa-house"></i> Ana Menüye Dön';
                retryBtn.classList.remove('hidden');
            } else {
                resultIcon.className = 'result-icon fail';
                resultIcon.innerHTML = '<i class="fa-solid fa-skull-crossbones"></i>';
                resultTitle.textContent = '❌ KAYBETTİNİZ!';
                resultTitle.style.color = 'var(--danger)';
                resultMessage.textContent = data.message;
                retryBtn.innerHTML = '<i class="fa-solid fa-rotate-right"></i> Tekrar Oyna';
                retryBtn.classList.remove('hidden');
            }
            
            resultModal.classList.remove('hidden');
        }, 2500);
    })
    .catch(err => {
        console.error('Accuse Error:', err);
        jailOverlay.classList.add('hidden');
        alert("Bağlantı hatası!");
    });
};

window.innocentNpc = function(npcId) {
    // Masum seçilen NPC'yi karttan kaldır veya gri yap
    const card = document.querySelector(`.found-npc-card .found-npc-img[data-npc-id="${npcId}"]`)?.closest('.found-npc-card');
    if (card) {
        card.style.opacity = '0.3';
        card.style.pointerEvents = 'none';
    }
};

// === RETRY ===
document.getElementById('result-retry-btn').addEventListener('click', () => {
    resultModal.classList.add('hidden');
    // Oyunu sıfırla
    initGame();
    triggerTransition(() => {
        // Tüm ekranları gizle
        townMapScreen.classList.add('hidden');
        interiorScreen.classList.add('hidden');
        npcTalkModal.classList.add('hidden');
        foundModal.classList.add('hidden');
        // Splash'a dön
        splashScreen.classList.remove('hidden');
    });
});

// =============================================================
// 8. EXIT TO MAIN MENU
// =============================================================

document.getElementById('exit-game-btn').addEventListener('click', () => {
    triggerTransition(() => {
        townMapScreen.classList.add('hidden');
        initGame();
        splashScreen.classList.remove('hidden');
    });
});

// =============================================================
// UTILITY: TRANSITION
// =============================================================

function triggerTransition(callback) {
    transitionOverlay.classList.add('flash');
    setTimeout(() => {
        callback();
        setTimeout(() => transitionOverlay.classList.remove('flash'), 300);
    }, 500);
}
