// =============================================================
// 🔍 DEDEKTİFLİK RPG - TAM OYUN MOTORU v2.0
// 5 NPC, Kademesiz Karışık Diyalog, Rastgele Suçlu, Ses Sistemi
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

// === AUDIO ELEMENTS ===
const bgMusic = document.getElementById('bg-music');
const rainSound = document.getElementById('rain-sound');
const doorCreak = document.getElementById('door-creak');
const doorClose = document.getElementById('door-close');
const npcMumble = document.getElementById('npc-mumble');
const typewriterSound = document.getElementById('typewriter-sound');

// === GAME STATE ===
let currentBag = [];
const MAX_BAG_SIZE = 10;
let activeNpcId = null;
let currentPendingObject = null;
let visitedBuildings = new Set();
let dialogHistory = {}; // { npcId: [{player, npc}] }
let guiltyNpcId = null;
let npcTalkCompleted = {}; // { npcId: true/false }
let isMuted = localStorage.getItem('gameMuted') === 'true';
let npcQuestionPools = {}; // { npcId: [remaining questions] }
let askedQuestionCount = {}; // { npcId: count }

// === GAME DATA ===
const NPC_DATA = {
    1: { id: 1, name: 'Kasap Hasan', building: 'Kasap', role: 'Kasabadaki eski kasap', img: 'images/hasan.png', bg: 'images/butcher_interior.png',
        secret: 'Cinayet gecesi dükkânında gizlice muhtara et sattı.',
        murderStory: 'Yağmurlu bir sonbahar gecesiydi. Kasap Hasan, dükkânını kapattıktan sonra doğruca Osman Bey\'in evine yürüdü. Yıllardır biriken veresiye borcu artık dayanılmaz bir hal almıştı — Osman Bey her seferinde ödemeyi erteliyordu. O gece Hasan son kez parasını istemeye gitti. Kapıyı Osman Bey açtığında, Hasan\'ın gözlerindeki öfkeyi fark edemedi. "Paran yarın gelecek" dedi alaycı bir gülümsemeyle. "Yarın mı? Yıllardır yarın diyorsun!" diye kükredi Hasan. Tartışma kızıştıkça Hasan\'ın eli yanında getirdiği satıra gitti. Bir anlık öfke krizinde, o ağır satırı kurbanın boyun bölgesine indirdi. Derin, tırtıklı yara — ancak bir kasabın elinden çıkabilecek bir darbeydi. Osman Bey son çırpınışlarında Hasan\'ın siyah deri önlüğünden parçalar kopartmaya çalıştı, tırnakları arasında kalan o küçük parçalar, son nefesinde bile savaştığının kanıtıydı. Hasan panikle satırı dükkânına götürüp tezgaha sapladı. Kanlı önlüğünü bir köşeye fırlattı, kara kaplı veresiye defterindeki Osman\'ın adını kırmızı kalemle çizdi. Ama karanlıkta ne kadar temizlerse temizlesin, kanın izi her yere sinmişti.'
    },
    2: { id: 2, name: 'Eczacı Selma', building: 'Eczane', role: 'Eczane sahibi', img: 'images/selma.png', bg: 'images/apothecary_interior.png',
        secret: 'Kurbanın zehirlendiğini biliyordu ama gizledi.',
        murderStory: 'Eczacı Selma, yıllardır kasabada sessiz sedasız çalışan, herkesin güvendiği bir kadındı. Ama bu sessizliğin ardında derin bir nefret gizliydi. Osman Bey, Selma\'nın geçmişine dair bir sır keşfetmiş ve onu aylardır bununla tehdit ediyordu — sessiz kalmasının karşılığında düzenli para talep ediyordu. O gece Selma, planını uygulamaya koydu. Eczanesinin tezgahı altında yetiştirdiği ölümcül bir sarmaşık türünün özünü, son derece dikkatli bir şekilde Osman Bey\'in her gün kullandığı kalp ilacına karıştırdı. Dozajı mükemmel hesaplamıştı — ne çok az, ne çok fazla. İlacı o gün Osman\'ın eline bizzat verdi, gülümseyerek. "Geçmiş olsun Osman Bey, bu ilacı düzenli alın" dedi. Gece yarısı, Osman Bey yatmadan önce ilacını içti. Birkaç dakika içinde kalbinde keskin bir ağrı hissetti. Kalp krizi geçiriyormuş gibi kıvrandı, nefes almaya çalıştı ama zehir çoktan damarlarına yayılmıştı. Selma, o sırada eczanesinin karanlık köşesinde, yağmurun sesini dinleyerek bekledi. Boş ilaç şişesini masanın altına sakladı, parmak izlerini titizlikle sildi. Reçete defterinin son sayfalarını — Osman\'ın gerçek teşhisini ve zehirlenme belirtilerini içeren notları — aceleyle yırtıp attı. Ama her ne kadar profesyonel davranmış olsa da, zehirli sarmaşık tezgahın altında kurumaya bırakılmış halde duruyordu.'
    },
    3: { id: 3, name: 'Muhtar Kemal', building: 'Muhtarlık', role: 'Kasabanın muhtarı', img: 'images/kemal.png', bg: 'images/town_hall_interior.png',
        secret: 'Kurbanla arazi anlaşmazlığı vardı.',
        murderStory: 'Muhtar Kemal, kasabanın en güçlü adamıydı — herkesin sırrını biliyor, her kapıyı açıyordu. Ama yaklaşan belediye seçimleri için büyük bir arazi projesine ihtiyacı vardı ve o arazinin sahibi Osman Bey\'di. Haftalardır Osman\'ı arazisini satması için ikna etmeye çalışmıştı ama Osman direndi. "Bu arazi babamdan kalma, satmam" dedi her seferinde. O gece Kemal, tüm diplomatik maskesini çıkardı. Gece yarısı Osman\'ın evine sızdı — muhtarlık kasasındaki yedek anahtarlarla kapıyı açmak çocuk oyuncağıydı. İçeri girdiğinde Osman masasında oturmuş, belgelerini inceliyordu. "Sen de mi Kemal?" dedi Osman, şaşkınlıkla. Kemal sahte tapu belgelerini masaya fırlattı. "Bunu imzalayacaksın, ya da..." Osman belgeleri yırtmaya başladı. Kemal kontrolünü kaybetti. Masadaki ağır bronz mühürü kaptığı gibi Osman\'ın yüzüne indirdi. Şiddetli bir boğuşma başladı — mobilyalar devrildi, Osman\'ın gözlüğü yere düşüp kırıldı. Kemal, son darbeyi kurbanın şakağına indirdiğinde, Osman\'ın gözleri kararıp yere yığıldı. Ölüm sebebi: ağır darbe sonucu beyin kanaması. Kemal panikle evi terk etti ama aceleyle çıkarken yırtılmış tapu belgelerini ve kırık gözlüğü olduğu yerde bıraktı. Ofisine döndüğünde, titreyerek gizli kasasını açıp sahte belgelerin kopyalarını içine kilitledi.'
    },
    4: { id: 4, name: 'Komiser Güneş', building: 'Karakol', role: 'Kadın polis komiseri', img: 'images/gunes.png', bg: 'images/police_interior.png',
        secret: 'Olay yerindeki delilleri sakladı.',
        murderStory: 'Komiser Güneş, kasabanın adalet sembolüydü — ya da öyle görünüyordu. Gerçekte yıllardır Osman Bey\'den düzenli rüşvet alıyordu. Osman, kasabadaki yasadışı arazi işlemlerini ve kaçak ticaret yollarını biliyordu; Güneş ise bu bilgilerin gün yüzüne çıkmaması için olayları kapatıyor, dosyaları kaybediyordu. Ama Osman artık bu düzenden bıkmıştı ve Güneş\'i ihbar etmekle tehdit etti. "Yarın sabah savcılığa gidiyorum" dedi telefonda, sesi kararlıydı. Güneş o gece üniformasını giydi, polis copunu beline taktı ve Osman\'ın evine gitti. Kapıyı açan Osman, komiserin yüzündeki soğuk ifadeyi gördüğünde anladı ama çok geçti. Güneş ilk darbeyi polis copuyla Osman\'ın karnına indirdi. Osman ikiye katlanırken, Güneş onu yere devirdi. Boğuşma sırasında Osman savunma yaraları aldı — kollarında, ellerinde darbe izleri oluştu. Güneş yakın mesafeden copla art arda vurdu. Son darbe şakağına geldiğinde Osman hareketsiz kaldı. Havasız kalma ve travmatik darbeler — bir polisin eğitimli şiddetiyle uyumlu izler. Güneş, bir polis olarak olay yerini profesyonelce temizlemeye çalıştı — parmak izlerini sildi, kan lekelerini temizledi. Ama boğuşma sırasında paltosunun pirinç düğmesi kopmuş, rozeti yere düşmüştü. Karanlıkta bunları fark edemedi. Karakola döndüğünde, "GİZLİ" damgalı dosyaya Osman\'ın ihbar dilekçesini kilitledi ve anahtarı çekmecesinin derinliklerine gömdü.'
    },
    5: { id: 5, name: 'Terzi Yahya', building: 'Terzi', role: 'Kasabanın terzisi', img: 'images/yahya.png', bg: 'images/tailor_interior.png',
        secret: 'Kurbana gizli cepli ceket dikti, son gören kişi.',
        murderStory: 'Terzi Yahya, kasabanın en yaşlı ve en saygın ustasıydı. Ama bu saygın cephenin arkasında karanlık bir ortaklık vardı — Yahya, yıllardır Osman Bey\'in gizli işlerinin sessiz ortağıydı. Para aklama, belge saklama, hatta kaçak mal transferi... Osman\'ın son diktirdiği ceketin astarına gizli bir cep dikmişti ve bu cepte, tüm yasadışı işlemlerin kaydını içeren bir USB bellek saklanıyordu. Ama Osman, ortaklığı bitirmeye ve Yahya\'yı saf dışı bırakmaya karar vermişti. O gece Yahya, payını almak için Osman\'ın evine gitti. "Param nerede Osman?" diye sordu titreyen bir sesle. Osman güldü. "Senin paran mı? Bu işte sen artık yoksun yaşlı adam. O USB\'yi de sana vermeyeceğim." Yılların birikimi bir anda patladı. Yahya, meslek hayatının en sadık aleti olan iplik makarasını cebinden çıkardı. Kalın, dayanıklı, kopması imkansız terzi ipliğini Osman\'ın boynuna doladı ve tüm gücüyle sıktı. Osman çırpındı, direndi — bu sırada Yahya\'nın diktiği ceketinden kumaş parçaları yırtıldı. Ama Yahya bırakmadı. İplik boyun bölgesinde derin izler bırakarak, Osman\'ın son nefesini de aldı. Yahya titreyerek ayağa kalktı. Kanlı iplik makarasını cebine koydu, yırtılan kumaş parçalarını toplamaya çalıştı ama hepsini bulamadı. Dükkânına döndüğünde, o gece diktiği son ceketin gizli cebindeki not hâlâ duruyordu: "Bu gece gel, konuşalım."'
    }
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

// =============================================================
// KADEMESIZ DIYALOG HAVUZU — 20 SORU HER NPC İÇİN (KARIŞIK)
// Her soru difficulty (1-5) ve category ile etiketli
// Suçlu NPC'ye göre dinamik cevaplar: guiltyResponse
// =============================================================

const NPC_ALL_QUESTIONS = {
    1: [ // Kasap Hasan — TÜM 20 SORU TEK HAVUZDA
        // Eski Kademe 1 (Tanışma - kolay)
        { q: 'Cinayet gecesi neredeydin Hasan?', a: 'Buradaydım, dükkânımda. Gece geç saate kadar et doğruyordum. Kimsecikler yoktu ortalıkta, yağmur bardaktan boşalırcasına yağıyordu.', difficulty: 1, category: 'tanisma', relatedClues: [] },
        { q: 'Kurbanı ne kadar iyi tanıyordun?', a: 'Osman Bey mi? Herkes tanır onu. İyi müşterimdi, her hafta gelirdi. Ama son zamanlarda arası bazılarıyla açılmıştı...', difficulty: 1, category: 'tanisma', relatedClues: [] },
        { q: 'Kasabada düşmanı olan var mıydı?', a: 'Düşman mı? Ha, bir sürü... Muhtar Kemal\'le arazi meselesinden dolayı birbirlerine giriyorlardı. Eczacı Selma da ondan pek hazzetmezdi.', difficulty: 1, category: 'tanisma', relatedClues: [7, 8] },
        { q: 'Dükkânında şüpheli bir şey gördün mü?', a: 'Şüpheli mi? Ben sadece kasabım dedektif bey. Ama... o gece garip sesler duydum sokaktan.', difficulty: 1, category: 'tanisma', relatedClues: [] },
        // Eski Kademe 2 (Derinleşme)
        { q: 'O gece duyduğun garip sesler neydi?', a: 'Bağrışma gibiydi... Ama yağmurdan net duyamadım. Saat gece yarısı civarıydı. Sonra bir araba kapısı çarpma sesi... Sonra sessizlik.', difficulty: 2, category: 'derinlesme', relatedClues: [] },
        { q: 'Dükkânına gelen şüpheli biri oldu mu?', a: 'Cinayet gecesi muhtar Kemal geldi aslında. Gece vakti et istedi. Aceleyle aldı gitti. Garip buldum ama sormadım.', difficulty: 2, category: 'derinlesme', relatedClues: [7] },
        { q: 'Kurbanla son ne zaman konuştun?', a: 'Cinayet gününden bir gün önce geldi. "Yarın büyük bir para gelecek" dedi. Bir daha göremedim...', difficulty: 2, category: 'derinlesme', relatedClues: [] },
        { q: 'Seni şüpheli görüyorlar, biliyor musun?', a: 'Ha! Beni mi? Ben niye öldüreyim müşterimi? Borcunu ödeyecekti, öldürsem para gider! Aklını kullan dedektif...', difficulty: 2, category: 'derinlesme', relatedClues: [1, 2] },
        // Eski Kademe 3 (Yüzleştirme - orta)
        { q: 'Bu kanlı satır senin tezgahından çıktı!', a: 'O... o satır çalınmıştı! Bir hafta önce kayboldu, polise söyledim ama kimse ciddiye almadı! Birisi beni suçlu göstermek istiyor!', difficulty: 3, category: 'yuzlestirme', relatedClues: [1], guiltyResponse: { 1: '*Yüzü bembeyaz olur* O... o satır... Evet, benim ama çalınmıştı! İnanın bana, birisi... birisi çerçeveliyor!', 2: 'Eczacıya sorun! O kadın her şeyi biliyor, zehirler, ilaçlar... Satırı çalan da o olabilir!', 3: 'Muhtar\'ın adamları çalmış olmalı! O gece dükkâna gelen Kemal\'di, belki de satırı o aldı!', 4: 'Komiser Güneş bunu neden araştırmadı? Kayıp bildirimi yaptım ama dosya kayboldu!', 5: 'Terzi Yahya bir bıçak kılıfı diktirdi benden... Acaba o kılıf bu satır için miydi?' } },
        { q: 'Kara defterdeki kurbanın ismi neden çizili?', a: 'Veresiye borcunu ödeyeceğini söyledi diye çizdim! O kadar! Herkes veresiye defteri tutar!', difficulty: 3, category: 'yuzlestirme', relatedClues: [2] },
        { q: 'Yırtık önlüğündeki anahtar neyin anahtarı?', a: '*Terler* O... o anahtar arka odanın anahtarı. Soğuk hava deposu. İçinde sadece etler var...', difficulty: 3, category: 'yuzlestirme', relatedClues: [3] },
        { q: 'Muhtar cinayet gecesi sana geldiğini inkâr ediyor.', a: 'Yalancı! O gece buraya geldi, gözleri dönmüştü! Eğer inkâr ediyorsa gizleyecek bir şeyi var demektir!', difficulty: 3, category: 'yuzlestirme', relatedClues: [7] },
        // Eski Kademe 4 (Baskı - zor)
        { q: 'Soğuk hava deposunda sadece et mi var?', a: '... İyi tamam. Orada eski belgeler de var. Kurbanın bazı evrakları... O bana emanet bırakmıştı.', difficulty: 4, category: 'baski', relatedClues: [3, 9], guiltyResponse: { 1: '*Çok terler* Tamam... orada... sadece belgeler değil... Kurbanla son tartışmamızda... İşler kontrolden çıktı. Ama yemin ederim kaza oldu!', 2: 'O belgeler Selma\'nın zehir siparişlerini gösteriyor! Korktum, sakladım!', 3: 'Muhtar\'ın sahte tapuları orada! Kurban bana emanet bıraktı!', 4: 'Komiser\'in kapatmaya çalıştığı dosyanın kopyası orada... Delil karartıyor!', 5: 'Yahya\'nın diktiği ceketteki USB\'nin kopyası orada...' } },
        { q: 'Kurbanın sana emanet bıraktığı şey neydi?', a: 'Bir zarf... İçinde arazi tapuları vardı. Muhtarın üzerine kayıtlı arazilerin aslında kurbana ait olduğunu gösteren belgeler.', difficulty: 4, category: 'baski', relatedClues: [9, 7] },
        { q: 'Neden bu belgeleri polise vermedin?', a: 'Korktum! Muhtar bu kasabada herkesin efendisi! Komiser Güneş zaten muhtarın adamı, kime güveneyim?', difficulty: 4, category: 'baski', relatedClues: [10, 11] },
        { q: 'Terziden kurbanın ceketini aldığını biliyoruz.', a: 'Hayır! Ben terziye hiç gitmedim! ... Tamam, Yahya\'dan bir bıçak kılıfı diktirdim ama kurbanla alakası yok!', difficulty: 4, category: 'baski', relatedClues: [13, 14] },
        // Eski Kademe 5 (Son - çok zor)
        { q: 'Son sözün nedir Hasan?', a: 'Ben masum bir kasabım! Evet, korkağım, belgeleri sakladım, ama kimseyi öldürmedim! Gerçek katili bulun!', difficulty: 5, category: 'son', relatedClues: [], guiltyResponse: { 1: '*Gözyaşları* Ben... ben sadece kızdım. O gece Osman borcunu ödemeyeceğini söyledi, tartıştık... Satır elimdeydi... Bir anlık öfke... Ama ben katil değilim, bu bir kazaydı!', 2: 'Selma\'nın elindeki zehir şişesini sorun! Ben değilim!', 3: 'Muhtar\'ın tapuları sahte! O öldürdü!', 4: 'Komiser delilleri saklıyor, gerçek katil o!', 5: 'Yahya\'daki USB\'yi açın, her şey orada!' } },
        { q: 'Katil kim sence?', a: 'Muhtar Kemal! Arazi meselesi yüzünden... Ama komiser de bu işin içinde olabilir. O gece karanlıkta bir kadın silueti gördüm...', difficulty: 5, category: 'son', relatedClues: [7, 10] },
        { q: 'Söylemediklerin var mı hâlâ?', a: '*Uzun sessizlik* Eczacı Selma... O gece dükkânını geç kapattı. Pencereden ışık gördüm. Elinde bir şişe vardı...', difficulty: 5, category: 'son', relatedClues: [4, 6] },
        { q: 'Masum olduğunu kanıtlayamazsan...', a: 'Emanet zarfı açın! İçindeki belgeler her şeyi anlatır! Ben sadece bir kasabım, korkak bir kasap...', difficulty: 5, category: 'son', relatedClues: [9] }
    ],
    2: [ // Eczacı Selma — TÜM 20 SORU
        { q: 'Cinayet gecesi eczaneniz açık mıydı?', a: 'Gece yarısına kadar açıktı. Envanter sayımı yapıyordum... Dışarıda yağmur yağıyordu, içeri müşteri gelmedi.', difficulty: 1, category: 'tanisma', relatedClues: [] },
        { q: 'Kurbanla ilişkiniz nasıldı?', a: 'Sadece müşterimdi. Düzenli ilaç alırdı, kronik bir rahatsızlığı vardı. Son zamanlarda daha sık geliyordu...', difficulty: 1, category: 'tanisma', relatedClues: [4] },
        { q: 'Kasabada zehirlenme vakaları olduğunu duydunuz mu?', a: 'Ne zehirlenmesi? Ben eczacıyım, ilaç satarım! Zehir değil! Böyle iftiralar atılmasına tahammülüm yok!', difficulty: 1, category: 'tanisma', relatedClues: [6] },
        { q: 'Kurbanın sağlık durumu hakkında bilginiz var mı?', a: 'Hasta bir adamdı. Kalp ilacı kullanıyordu. Ama son haftalarda reçetesiz bir ilaç daha istemeye başladı...', difficulty: 1, category: 'tanisma', relatedClues: [4, 5] },
        { q: 'Kurban reçetesiz hangi ilacı istedi?', a: 'Güçlü bir uyku ilacı. Uykusuzluk çektiğini söyledi ama... O dozda kalp hastası için çok tehlikeli olurdu.', difficulty: 2, category: 'derinlesme', relatedClues: [4] },
        { q: 'Gece yarısına kadar neden açıktınız?', a: '... Birini bekliyordum tamam mı? Muhtar Kemal aradı, "Acil ilaç lazım, geç geleceğim" dedi. Ama gelmedi.', difficulty: 2, category: 'derinlesme', relatedClues: [7], guiltyResponse: { 1: 'Kasap Hasan o gece geldi, çok gergindi. Ellerinde kan lekesi vardı, "Kesildim" dedi ama inanmadım...', 2: '*Titrer* Tamam... Muhtar\'ı beklemiyordum. Ben... kurbanı bekliyordum. Son ilaç dozunu verecektim ama... dozajı yanlış hesaplamış olabilirim.', 3: 'Muhtar aradı evet, ama sesinde panik vardı. "Selma, beni örtbas et" dedi. Ne demek istediğini anlayamadım...', 4: 'Komiser o gece geldi dükkâna. Defterimi istedi, "Bunu bana ver" dedi. Sayfaları o yırttı!', 5: 'Yahya\'nın o gece paketini gördüm... Kurbanın evine gidiyordu. İçinde ne vardı bilmiyorum ama ağırdı.' } },
        { q: 'Komiser Güneş sizi cinayet gecesi gördüğünü söylüyor.', a: 'Nerede görmüş? Ben dükkânımdan çıkmadım! Eğer öyle diyorsa yalan söylüyor...', difficulty: 2, category: 'derinlesme', relatedClues: [10, 11] },
        { q: 'Kurbanın ölüm sebebi zehirlenme olabilir mi?', a: '*Yüzü solar* Zehirlenme mi? Bu... bu çok kötü. Ben hiçbir şey satmadım, yemin ederim!', difficulty: 2, category: 'derinlesme', relatedClues: [4, 6] },
        { q: 'Bu boş ilaç şişesindeki zehri kime sattın?', a: 'O... o ilacı ben kimseye satmadım! Şişe çalınmış olmalı! Belki biri gece dükkâna girdi ve aldı...', difficulty: 3, category: 'yuzlestirme', relatedClues: [4], guiltyResponse: { 1: 'Kasap\'a sorun! O gece dükkânıma girip çıkan birini gördüm, iri yarı biriydi!', 2: '*Yıkılır* O şişe... benim. Kurbanın ilacına karıştırdım ama öldürmek için değil! Acısını dindirmek için! O çok acı çekiyordu, yalvardı bana!', 3: 'Muhtar istedi o ilacı! "Birisi için lazım" dedi. Kim için olduğunu sormadım...', 4: 'O şişeyi komiser aldı! Benden zorla aldı, "Delil" dedi ama rapora yazmadı!', 5: 'Yahya\'nın gizli cepte sakladığı not... O notta bu ilaçtan bahsediliyor olabilir.' } },
        { q: 'Reçete defterinin son sayfasını neden yırttın?', a: '*Titreyerek* Orada önemli bir not vardı. Kurbanın gerçek teşhisi... Mesleki sorumluluğum...', difficulty: 3, category: 'yuzlestirme', relatedClues: [5] },
        { q: 'Tezgah altındaki zehirli sarmaşık ne için?', a: 'Tıbbi araştırma! Geleneksel tıpta kullanılır! Ben onu ilaç yapmak için yetiştiriyorum, zehir olarak değil!', difficulty: 3, category: 'yuzlestirme', relatedClues: [6] },
        { q: 'Kurbanın gerçek teşhisi neydi?', a: '*Uzun sessizlik* Osman Bey zehirleniyordu... Yavaş yavaş. Ama ben yapmadım! Birisi ona düzenli olarak küçük dozlarda zehir veriyordu.', difficulty: 3, category: 'yuzlestirme', relatedClues: [4, 5, 6] },
        { q: 'Neden polise söylemedin zehirlenme şüpheni?', a: 'Komiser Güneş\'e söyledim! Ama ciddiye almadı. Defteri gösterdim. Sonra... bazı sayfaları kayboldu.', difficulty: 4, category: 'baski', relatedClues: [5, 10, 11] },
        { q: 'Komiser defterin sayfalarını mı aldı?', a: 'Bilmiyorum! Ama o gün komiserin gelişinden sonra sayfalar yoktu. Belki tesadüftür... belki değildir.', difficulty: 4, category: 'baski', relatedClues: [11] },
        { q: 'Muhtar neden seni arayıp ilaç istedi o gece?', a: 'Stres ilacı istedi. "Çok gerginim, uyuyamıyorum" dedi. Ama sesinde korku vardı... Normal değildi.', difficulty: 4, category: 'baski', relatedClues: [7] },
        { q: 'Terzi Yahya\'yla ilişkin nedir?', a: 'Yahya mı? Komşuyuz, bazen çay içeriz. Ama... Yahya kurbanın son günlerinde onu çok sık ziyaret etti.', difficulty: 4, category: 'baski', relatedClues: [13, 14, 15] },
        { q: 'Son sözün nedir Selma?', a: 'Ben eczacıyım, insanları iyileştirmek için çalışıyorum! Evet, şüphemi sakladım ama korktum!', difficulty: 5, category: 'son', relatedClues: [] },
        { q: 'Katil kim sence?', a: 'Muhtar ve komiser arasında bir bağ var. Cinayet gecesi muhtar et almaya gitti, komiser olay yerini geç inceledi...', difficulty: 5, category: 'son', relatedClues: [7, 10] },
        { q: 'Sakladığın başka bir şey var mı?', a: 'Cinayet gecesi... Pencereden Terzi Yahya\'yı gördüm. Elinde bir paket vardı ve kurbanın evine doğru gidiyordu.', difficulty: 5, category: 'son', relatedClues: [13, 15] },
        { q: 'Bu zehirli bitkiyi kurban için mi kullandın?', a: 'HAYIR! O bitki deneysel ilaç çalışmam için! Ben birini zehirlemek istesem... şey, yani teorik olarak...', difficulty: 5, category: 'son', relatedClues: [6] }
    ],
    3: [ // Muhtar Kemal — TÜM 20 SORU
        { q: 'Muhtar bey, cinayet gecesi neredeydiniz?', a: 'Evimdeydim tabii ki. Televizyon izledim, sonra uyudum. Muhtarın gece sokakta ne işi olur?', difficulty: 1, category: 'tanisma', relatedClues: [] },
        { q: 'Kurbanla aranızdaki ilişki nasıldı?', a: 'Normal komşuluk. Bazen anlaşamadığımız konular oldu ama siyasette düşman olmak cinayet sebebi değildir.', difficulty: 1, category: 'tanisma', relatedClues: [7] },
        { q: 'Kasabada gerginliğin sebebi nedir?', a: 'Arazi meseleleri... Belediye yeni yol geçirecek, bazı araziler kamulaştırılacak. Herkes pay kapmaya çalışıyor.', difficulty: 1, category: 'tanisma', relatedClues: [9] },
        { q: 'Kurbanın ölümü sana yaradı diyorlar.', a: 'Kim diyor? Kimin diline düşmüşüm? Osman\'ın ölümü bana hiçbir şey kazandırmadı!', difficulty: 1, category: 'tanisma', relatedClues: [7, 9] },
        { q: 'Arazi meselesi hakkında detay ver.', a: 'Kurbanın arazisi yolun tam üzerinde. Kamulaştırma bedeli çok yüksek olacaktı. Osman satmak istemiyordu...', difficulty: 2, category: 'derinlesme', relatedClues: [9] },
        { q: 'Cinayet gecesi kasaba gidip et aldığın doğru mu?', a: '*Duraksır* Kim söyledi? Kasap Hasan mı? Sadece kısa bir yürüyüşe çıktım. Kasabın önünden geçtim ama et almadım!', difficulty: 2, category: 'derinlesme', relatedClues: [1, 2], guiltyResponse: { 1: 'Hasan yalancı! O gece kasabın önünden bile geçmedim! ... Tamam, geçtim ama et almak için değil.', 2: 'Selma\'yı aradığımı itiraf ediyorum. Stres ilacı lazımdı. Ama gitmediğimi yemin ederim!', 3: '*Sinirlenir* Tamam! Et aldım! Ama cinayet saatinden ÇOK önce! Saat 10\'da gidip geldim. Sonra eve döndüm. Sonra... yürüyüşe çıktım tekrar. Kurbanın evinin önünden geçtim. Ama ona dokunmadım! ...En azından öyle hatırlıyorum.', 4: 'Komiser beni uyardı, "Evinde kal" dedi. Ama dinlemedim, merak ettim ne oluyor diye...', 5: 'Yahya\'yı o gece sokakta gördüm! O da dışarıdaydı!' } },
        { q: 'Eczacı Selma seni aradığını söylüyor o gece.', a: 'Hayır! Ben kimseyi aramadım! Selma yanlış hatırlıyor... Ya da bilerek yalan söylüyor.', difficulty: 2, category: 'derinlesme', relatedClues: [4, 5] },
        { q: 'Kurbanla son görüşmeniz ne zamandı?', a: 'Cinayet gününden iki gün önce. Bağırdı çağırdı. "Arazimi vermeyeceğim, mahkemeye giderim!" dedi.', difficulty: 2, category: 'derinlesme', relatedClues: [7] },
        { q: 'Bu tehdit mektubunu sen yazdın, çekmecende bulduk!', a: '*Yüzü kızarır* Bunu sinirle yazdım! Göndermeyecektim! Herkes kızgınken bir şeyler yazar!', difficulty: 3, category: 'yuzlestirme', relatedClues: [7] },
        { q: 'Kurbanın kırık gözlüğü senin odanda ne işi var?', a: 'Kavga ettiğimizde düştü! Kırdım evet, ama sonra pişman oldum. Geri verecektim...', difficulty: 3, category: 'yuzlestirme', relatedClues: [8] },
        { q: 'Kasadaki sahte belgeler neyin nesi?', a: '*Terler* Onlar... eski belediye evrakları. Bazen prosedürler hızlansın diye bazı belgeler... düzenlenir.', difficulty: 3, category: 'yuzlestirme', relatedClues: [9] },
        { q: 'Kasap Hasan cinayet gecesi geldiğini kanıtlayabilir.', a: 'Tamam! Evet, kasaba gittim! Et aldım! Ama bu beni katil yapmaz!', difficulty: 3, category: 'yuzlestirme', relatedClues: [1, 2] },
        { q: 'Gece yarısı et almak için mi çıktın gerçekten?', a: '... Tamam, et bahaneydi. Kurbanın evinin önünden geçmek istedim. Tehdit mektubu yazdığım için vicdan azabı çekiyordum.', difficulty: 4, category: 'baski', relatedClues: [7], guiltyResponse: { 1: 'Kasap\'ın söylediklerine inanmayın! O adam yalancının teki!', 2: 'Selma beni çerçevelemeye çalışıyor! O zehirci kadın!', 3: '*Masayı yumruklar* TAMAM! Kurbanın evine gittim! Konuşmak istedim, barışmak... Ama o beni kovdu, aşağıladı. "Sahte tapularını biliyorum" dedi. Tartıştık... ve ben... kontrol... *susar*', 4: 'Komiser Güneş her şeyi biliyor! O gece beni aradı, "Muhtar sakin ol" dedi. Neden böyle dedi?', 5: 'Yahya\'nın elinde bir paket vardı o gece, onu araştırın!' } },
        { q: 'Kurbanın evinde ne gördün?', a: 'Işıklar yanıyordu. Bir gölge gördüm pencerede... Kurban değildi. Başka birisi vardı orada.', difficulty: 4, category: 'baski', relatedClues: [] },
        { q: 'Komiser Güneş\'le ilişkin nedir?', a: 'Resmi ilişkimiz var... *duraksır* Bazen bazı dosyaların kapanması konusunda ortak çalışırız. Hepsi bu.', difficulty: 4, category: 'baski', relatedClues: [10, 11] },
        { q: 'Arazi tapuları aslında kurbanın üzerineymiş.', a: '*Şok olur* Ne?! Tapular... Kim söyledi bunu?! O araziler yasal olarak belediyeye aittir!', difficulty: 4, category: 'baski', relatedClues: [9] },
        { q: 'Son sözün nedir Kemal?', a: 'Ben bu kasabanın muhtarıyım! 20 yıldır hizmet ediyorum. Evet hatalarım oldu ama kimseyi öldürmedim!', difficulty: 5, category: 'son', relatedClues: [] },
        { q: 'Katil kim sence?', a: 'Kasap Hasan! O satırla... Ama belki eczacı. O kadının ne zehirler bildiğini düşünün! Ya da terzi...', difficulty: 5, category: 'son', relatedClues: [1, 4, 13] },
        { q: 'Söylemediklerin var mı?', a: '*İç çeker* Komiser Güneş... O gece beni aradı. "Muhtar, evinde kal" dedi. Neden böyle dediğini hiç sormadım...', difficulty: 5, category: 'son', relatedClues: [10, 11] },
        { q: 'Kurbanın evindeki gölge kim olabilir?', a: 'Uzun boylu biriydi... Terzi Yahya\'nın boyu uzundur... O gece herkes bir yerlere gidiyordu bu kasabada.', difficulty: 5, category: 'son', relatedClues: [13, 14, 15] }
    ],
    4: [ // Komiser Güneş — TÜM 20 SORU
        { q: 'Komiser, olay yerine ilk gelen siz miydiniz?', a: 'Evet, saat 02:30 civarında ihbar aldık. 10 dakika içinde oradaydım. Ceset meydanda yatıyordu, yağmur izleri siliyordu.', difficulty: 1, category: 'tanisma', relatedClues: [] },
        { q: 'Kurban hakkında ne biliyorsunuz?', a: 'Osman Bey, 58 yaşında, tüccar. Kasabada tanınan bir isim. Arazi anlaşmazlıkları dışında bilinen düşmanı yoktu...', difficulty: 1, category: 'tanisma', relatedClues: [] },
        { q: 'Cinayet gecesi siz neredeydiniz?', a: 'Karakolda nöbetteydim. Evrak işleriyle uğraşıyordum. Yağmurlu gecelerde genelde sakin olur kasaba...', difficulty: 1, category: 'tanisma', relatedClues: [10] },
        { q: 'İlk bulgular neler?', a: 'Kafa travması. Sert bir cisimle vurulmuş. Ölüm saati 23:00-01:00 arası. Olay yerinde az sayıda fiziksel delil vardı.', difficulty: 1, category: 'tanisma', relatedClues: [1, 12] },
        { q: 'Olay yerinde hangi delilleri buldunuz?', a: 'Bir düğme, bazı ayak izleri... Yağmur çoğunu sildi. Standart prosedür uyguladık.', difficulty: 2, category: 'derinlesme', relatedClues: [12] },
        { q: 'Neden dış dedektif çağrıldı?', a: '*Rahatsız olur* Üst makamın kararı. "Tarafsız göz lazım" dediler. Kasabada herkes birbirini tanıyor.', difficulty: 2, category: 'derinlesme', relatedClues: [] },
        { q: 'Muhtar Kemal\'le ilişkiniz profesyonel mi?', a: 'Tabii ki profesyonel! Muhtar resmi makam, ben de polis. Başka bir ilişki yok!', difficulty: 2, category: 'derinlesme', relatedClues: [7, 9], guiltyResponse: { 1: 'Kasap Hasan\'ın dükkânında şüpheli evraklar bulundu, onu soruşturuyorum!', 2: 'Eczacı Selma\'nın zehir stoku var, bunu raporladım ama üst makam ilgilenmedi.', 3: 'Muhtar Kemal... *duraksır* Evet, bazen dosyaları birlikte kapattık. Ama bu normal prosedür!', 4: '*Sertleşir* Ben 15 yıllık polisim! Muhtar\'la profesyonel ilişkimiz var, ama cinayet gecesi... tamam, onu aradım. Uyardım. Çünkü bilgi aldım, ihbar vardı. Ama bu bilgiyi... resmi kanaldan almadım. Bir kaynak... söyleyemem.', 5: 'Terzinin dosyasında adı geçiyor ama bu gizli bir soruşturma!' } },
        { q: 'Eczacı Selma zehirlenme şüphesini bildirmiş miydi?', a: '*Duraksır* Selma mı söyledi? Bir keresinde "kan değerleri garip" gibi bir şey demişti ama somut kanıt yoktu.', difficulty: 2, category: 'derinlesme', relatedClues: [4, 5] },
        { q: 'Bu polis rozeti olay yerinde bulundu!', a: '*Yüzü değişir* Kayıp olarak rapor edilmişti. Bir ay önce karakoldan çalındı. Kim aldıysa olay yerine bırakmış.', difficulty: 3, category: 'yuzlestirme', relatedClues: [10] },
        { q: 'Gizli dosyadaki bilgiler neden rapor edilmedi?', a: 'O dosya devam eden bir soruşturmanın parçası! Her şeyi kamuoyuyla paylaşamam. Prosedür gereği gizli!', difficulty: 3, category: 'yuzlestirme', relatedClues: [11] },
        { q: 'Bu düğme terzi Yahya\'nın diktiği paltoya ait.', a: 'İlginç... Yahya\'dan düğmenin kime ait olduğunu sorduk ama net cevap vermedi.', difficulty: 3, category: 'yuzlestirme', relatedClues: [12, 14] },
        { q: 'Eczacının defterinden sayfaları siz mi aldınız?', a: 'NE?! Ben kimsenin defterinden sayfa almadım! Bu çok ciddi bir itham! Kanıtınız var mı?', difficulty: 3, category: 'yuzlestirme', relatedClues: [5] },
        { q: 'O gece muhtarı neden arayıp evinde kalmasını söylediniz?', a: '*Şaşırır* Muhtar bunu mu söyledi? Ben onu güvenlik için uyardım! İhbar gelmişti!', difficulty: 4, category: 'baski', relatedClues: [7] },
        { q: 'İhbar gelmeden ÖNCE muhtarı aradığınız kanıtlandı.', a: '*Uzun sessizlik* ... Birisi beni aradı. Tanımadığım numara. "Meydanda bir şey olacak" dedi. Ciddiye aldım.', difficulty: 4, category: 'baski', relatedClues: [10, 11], guiltyResponse: { 1: 'Kasap\'ın soğuk hava deposunu araştırın!', 2: 'Eczacının tezgah altındaki bitkileri inceleyin!', 3: 'Muhtar\'ın kasasındaki sahte belgeleri bulun!', 4: '*Masaya yumruğunu vurur* TAMAM! Evet, muhtarı ihbardan önce aradım. Çünkü BEN o ihbarı aldım... kendi kendime. Anonim aramanın kaynağını biliyordum çünkü... o ses benim tanıdığımdı. Ve ben... olay yerine gittiğimde delilleri... düzenledim. Ama öldürmedim! Birisi bana talimat verdi!', 5: 'Yahya\'nın USB belleğinde her şey var!' } },
        { q: 'Delilleri karartma şüpheniz var.', a: 'Karartma mı?! 15 yıllık polisim! Evet, bazı bilgileri gizli tuttum ama prosedür gereği!', difficulty: 4, category: 'baski', relatedClues: [10, 11] },
        { q: 'Olay yerinden rapora yazmadığınız neler var?', a: '*Masaya bakar* Bir mektup... Kurbanın cebinden. "Beni takip ediyorlar, bu gece her şeyi açıklayacağım" yazıyordu...', difficulty: 4, category: 'baski', relatedClues: [15] },
        { q: 'Son sözünüz nedir Komiser?', a: 'Ben görevimi yaptım! Prosedür hataları oldu ama tarafsız kalmak zor... Adaletin yanındayım.', difficulty: 5, category: 'son', relatedClues: [] },
        { q: 'Katil kim sizce?', a: 'Kanıtlar muhtarı gösteriyor... Arazi meselesi, tehdit mektubu, o gece dışarı çıkması. Ama kasap da şüpheli.', difficulty: 5, category: 'son', relatedClues: [1, 7] },
        { q: 'Sakladığınız başka bilgi var mı?', a: 'Kurbanın cebindeki mektupta: "Yahya her şeyi biliyor" yazıyordu. Ne anlama geliyor bilmiyorum.', difficulty: 5, category: 'son', relatedClues: [15] },
        { q: 'Sizi de şüpheli listesine ekliyorum.', a: '*Sertleşir* Bu sizin hakkınız. Ama gerçek katili bulun, o zaman masumiyetimi görürsünüz.', difficulty: 5, category: 'son', relatedClues: [10, 11] }
    ],
    5: [ // Terzi Yahya — TÜM 20 SORU
        { q: 'Yahya usta, cinayet gecesi neredeydin?', a: 'Dükkânımdaydım, gece geç saate kadar dikiş dikiyordum. Sipariş yetişmesi lazımdı. Makinenin sesinden başka bir şey duymadım.', difficulty: 1, category: 'tanisma', relatedClues: [] },
        { q: 'Kurbanı tanıyor muydun?', a: 'Eski müşterimdi. Son birkaç haftadır sık geliyordu. Özel bir ceket sipariş etmişti... Teslim edemedim.', difficulty: 1, category: 'tanisma', relatedClues: [14, 15] },
        { q: 'Kasabadaki gerginliklerden haberin var mı?', a: 'Ben terziyim, kumaşla uğraşırım. İnsanların kavgalarına karışmam. Ama... son zamanlarda herkes gergindi.', difficulty: 1, category: 'tanisma', relatedClues: [] },
        { q: 'Dükkânında ilginç bir şey fark ettin mi?', a: 'İlginç mi? Hayır, her şey normal... Dikişler, kumaşlar, müşteriler. *Gözlerini kaçırır*', difficulty: 1, category: 'tanisma', relatedClues: [13] },
        { q: 'Kurbanın sipariş ettiği ceket nasıl bir ceketti?', a: 'Özel bir ceket... Gizli cepleri olan, astarı kalın. "İçine belgeler koyacağım" demişti.', difficulty: 2, category: 'derinlesme', relatedClues: [14, 15] },
        { q: 'Kurban sana belge saklatmak mı istedi?', a: 'Hayır! Sadece cekete cep dikeyim dedi. Belgeleri kendisi koyacaktı. Ben sadece terziyim!', difficulty: 2, category: 'derinlesme', relatedClues: [15] },
        { q: 'Cinayet gecesi dükkânından çıktın mı?', a: '*Duraksır* Bir kez çıktım. Sigara içmeye. Ama hemen döndüm. 10 dakika bile sürmedi.', difficulty: 2, category: 'derinlesme', relatedClues: [], guiltyResponse: { 1: 'Kasap\'ın dükkânında ışık yanıyordu ama girmedim oraya!', 2: 'Eczane\'nin ışığı da açıktı. Selma pencereden baktı ama beni görmemiş olmalı... *yutkunur*', 3: 'Muhtar\'ın sokakta olduğunu gördüm, ama yüzünü seçemedim.', 4: 'Komiser\'in devriye aracı geçti sokaktan... Beni görmüş olabilir.', 5: '*Elleri titrer* Tamam... 10 dakikadan fazla dışarıdaydım. Kurbanın evine gittim. Notu ben yazmıştım, "Bu gece gel konuşalım" diye. Gittim. Kapı açıktı. İçeri girdim. Osman... Osman yerde yatıyordu. Ama ben varmadan ölmüştü! ...Yoksa ölmemiş miydi?' } },
        { q: 'Eczacı Selma seni kurbanın evine giderken gördü.', a: 'Selma mı?! Yanılmış olmalı! Sadece sigara içmeye çıktım! *Elleri titrer*', difficulty: 2, category: 'derinlesme', relatedClues: [] },
        { q: 'Bu kanlı iplik makarası senin dükkânından!', a: 'Kan mı?! İmkânsız! Kumaş keserken elimi keserim, o benim kanım olabilir!', difficulty: 3, category: 'yuzlestirme', relatedClues: [13] },
        { q: 'Bu yırtık kumaş kurbanın ceketinden kopmuş.', a: '*Yutkunur* O kumaş bende vardı evet. Ceketi dikerken artan parça. Her terzi artık kumaş saklar!', difficulty: 3, category: 'yuzlestirme', relatedClues: [14] },
        { q: 'Gizli cepteki not "Bu gece gel, konuşalım" yazıyor. Senin el yazın!', a: '*Terler* Ben yazdım evet. Kurban benimle konuşmak istedi! Gizli bir şey anlatacaktı ama... varamadım!', difficulty: 3, category: 'yuzlestirme', relatedClues: [15], guiltyResponse: { 1: 'Kasap Hasan\'ın o satırla bir ilgisi olabilir! Benden bıçak kılıfı istedi!', 2: 'Selma\'nın zehirleri var, onu araştırın! Ben sadece not yazdım!', 3: 'Muhtar\'ın tehdit mektubu daha tehlikeli bir delil!', 4: 'Komiser bu notu biliyordu! Dosyasında yazıyor!', 5: '*Titrer* Evet, o notu ben yazdım. Kurbanla buluşacaktık. Ama gittiğimde... kapı açıktı. Girdim. Tartıştık. O belgeler yüzünden... USB\'deki bilgiler... Ben sadece korumak istiyordum ama Osman vermek istemedi. Çekiştirdik ve...' } },
        { q: 'Neden varamadın? Yola çıktığını biliyoruz.', a: 'Çıktım evet! Ama yarı yolda döndüm! Korktum... Gölge gibi bir figür gördüm. Korkup geri döndüm!', difficulty: 3, category: 'yuzlestirme', relatedClues: [] },
        { q: 'Gördüğün gölge kim olabilir?', a: 'Bilmiyorum! Karanlıktı, yağmur yağıyordu... Uzun bir palto giyiyordu. Belki muhtarın paltosu.', difficulty: 4, category: 'baski', relatedClues: [12] },
        { q: 'Kasap Hasan\'a bıçak kılıfı diktin mi?', a: '*Gözleri büyür* Kim söyledi?! Evet diktim. Normal bir sipariş! Kasap bıçak kılıfı kullanır!', difficulty: 4, category: 'baski', relatedClues: [1, 3] },
        { q: 'Kurbanın sana anlattığı gizli şey neydi?', a: 'Tam anlatmadı. "Bu kasabada herkes bir şeyler gizliyor, dikkat et Yahya" dedi. Tapulardan, belgelerden bahsetti.', difficulty: 4, category: 'baski', relatedClues: [9, 15] },
        { q: 'Komiserin gizli dosyasında senin adın geçiyor.', a: 'BENİM ADIM MI?! Ne yazıyor?! Ben hiçbir suç işlemedim! Sadece elbise dikerim! *Panik yapar*', difficulty: 4, category: 'baski', relatedClues: [11] },
        { q: 'Son sözün nedir Yahya?', a: 'Ben masum bir terziyim! Kurbanla görüşmeye çalıştım ama varamadım! O gece herkes sokaktaydı...', difficulty: 5, category: 'son', relatedClues: [] },
        { q: 'Katil kim sence?', a: 'Muhtar Kemal! Arazi meselesi yüzünden... Ya da komiser. O kadın bir şeyler saklıyor, gözlerinden belli.', difficulty: 5, category: 'son', relatedClues: [7, 10] },
        { q: 'Sakladığın son bir şey var mı?', a: '*Gözyaşları* Kurbanın ceketi... Bitmiş haliyle duvarda asılı. İçindeki gizli cepte bir USB bellek var. Kimseye vermedim.', difficulty: 5, category: 'son', relatedClues: [15] },
        { q: 'USB bellekte ne var?', a: 'Bilmiyorum! Açmadım! Kurban "Başıma bir şey gelirse bunu doğru kişiye ver" demişti. Doğru kişi kim?', difficulty: 5, category: 'son', relatedClues: [15, 9] }
    ]
};

// =============================================================
// SES SİSTEMİ
// =============================================================

function playSound(audioEl, volume = 0.5) {
    if (!audioEl || isMuted) return;
    try {
        audioEl.volume = Math.min(1, Math.max(0, volume));
        audioEl.currentTime = 0;
        audioEl.play().catch(e => console.log('Ses hatası:', e));
    } catch(e) { console.log('Ses hatası:', e); }
}

function playLoopSound(audioEl, volume = 0.3) {
    if (!audioEl || isMuted) return;
    try {
        audioEl.volume = Math.min(1, Math.max(0, volume));
        audioEl.loop = true;
        audioEl.play().catch(e => console.log('Ses hatası:', e));
    } catch(e) { console.log('Ses hatası:', e); }
}

function stopSound(audioEl) {
    if (!audioEl) return;
    try {
        audioEl.pause();
        audioEl.currentTime = 0;
    } catch(e) {}
}

function stopAllSounds() {
    [bgMusic, rainSound, doorCreak, doorClose, npcMumble, typewriterSound].forEach(a => {
        if (a) { a.pause(); a.currentTime = 0; }
    });
}

function toggleMute() {
    isMuted = !isMuted;
    localStorage.setItem('gameMuted', isMuted);
    
    const btn = document.getElementById('mute-toggle-btn');
    if (!btn) return;
    
    if (isMuted) {
        btn.classList.add('muted');
        btn.innerHTML = '<i class="fa-solid fa-volume-xmark"></i>';
        stopAllSounds();
    } else {
        btn.classList.remove('muted');
        btn.innerHTML = '<i class="fa-solid fa-volume-high"></i>';
        // Arka plan müziğini ve yağmuru tekrar başlat (eğer oyun başladıysa)
        if (!splashScreen || splashScreen.classList.contains('hidden')) {
            playLoopSound(bgMusic, 0.3);
            playLoopSound(rainSound, 0.5);
        }
    }
}

// YENİ OTOPSİ ZAMANLAYICISI SİSTEMİ
let autopsyTimer = null;
let autopsyTimeLeft = 240; // 4 dakika süre verildi (240 saniye)
let isAutopsyReady = false;

function startAutopsyTimer() {
    autopsyTimeLeft = 240;
    isAutopsyReady = false;
    const container = document.getElementById('autopsy-timer-container');
    const timerSpan = document.getElementById('autopsy-timer');
    
    container.classList.remove('hidden');
    container.classList.remove('ready');
    
    if (autopsyTimer) clearInterval(autopsyTimer);
    
    autopsyTimer = setInterval(() => {
        autopsyTimeLeft--;
        
        let m = Math.floor(autopsyTimeLeft / 60).toString().padStart(2, '0');
        let s = (autopsyTimeLeft % 60).toString().padStart(2, '0');
        timerSpan.textContent = `${m}:${s}`;
        
        if (autopsyTimeLeft <= 0) {
            clearInterval(autopsyTimer);
            isAutopsyReady = true;
            
            // UI Güncelle
            container.classList.add('ready');
            container.innerHTML = '<i class="fa-solid fa-file-signature"></i> Otopsi Raporu Geldi! (Tıkla)';
            
            // Mesaj Bildirimi (Custom Modal)
            const notifModal = document.getElementById('global-notification-modal');
            if (notifModal) {
                notifModal.classList.remove('hidden');
            }
        }
    }, 1000);
}

// Global notification modal close event
document.getElementById('notification-ok-btn')?.addEventListener('click', () => {
    document.getElementById('global-notification-modal').classList.add('hidden');
});

// Otopsi Tıklama Olayı
document.getElementById('autopsy-timer-container').addEventListener('click', () => {
    if (isAutopsyReady) {
        // Backend'den otopsi raporunu çek
        fetch('/api/game/autopsy')
            .then(async res => {
                if (!res.ok) {
                    const errText = await res.text();
                    throw new Error(errText);
                }
                return res.json();
            })
            .then(data => {
                if (data.success) {
                    document.getElementById('autopsy-text').textContent = data.report;
                    document.getElementById('autopsy-modal').classList.remove('hidden');
                }
            })
            .catch(err => {
                console.error("Otopsi hatası:", err);
                document.getElementById('autopsy-text').textContent = "Otopsi raporu alınamadı: " + err.message;
                document.getElementById('autopsy-modal').classList.remove('hidden');
            });
    }
});

document.getElementById('close-autopsy').addEventListener('click', () => {
    document.getElementById('autopsy-modal').classList.add('hidden');
});

// Mute butonunu başlat
function initMuteButton() {
    const btn = document.getElementById('mute-toggle-btn');
    if (!btn) return;
    
    btn.addEventListener('click', toggleMute);
    
    // Kayıtlı durumu uygula
    if (isMuted) {
        btn.classList.add('muted');
        btn.innerHTML = '<i class="fa-solid fa-volume-xmark"></i>';
    }
}

initMuteButton();

// =============================================================
// GAME INITIALIZATION
// =============================================================

function initGame() {
    currentBag = [];
    visitedBuildings = new Set();
    dialogHistory = {};
    npcTalkCompleted = {};
    activeNpcId = null;
    npcQuestionPools = {};
    askedQuestionCount = {};
    
    // Her NPC için soru havuzunu sıfırla
    for (let id = 1; id <= 5; id++) {
        npcQuestionPools[id] = [...(NPC_ALL_QUESTIONS[id] || [])];
        askedQuestionCount[id] = 0;
    }
    
    // Suçluyu backend API'den sıfırla
    fetch('/api/game/reset', { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            console.log('\uD83D\uDD0D API:', data.message);
            // Suçlu NPC ID'sini al
            if (data.guiltyNpcId) {
                guiltyNpcId = data.guiltyNpcId;
                console.log('\uD83D\uDD0D Su\u00E7lu NPC:', guiltyNpcId);
            }
        })
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
    playLoopSound(bgMusic, 0.3);
    playLoopSound(rainSound, 0.5);

    triggerTransition(() => {
        splashScreen.classList.add('hidden');
        storyIntroScreen.classList.remove('hidden');
        startTypewriter();
    });
});

const STORY_TEXT = 'Ya\u011Fmurlu bir sonbahar gecesi... Kasaban\u0131n meydanında bir ceset bulundu. Kurban, herkesin tanıdığı t\u00FCccar Osman Bey\'di. Parke taşların üzerinde yatan cansız beden, yağmurun altında solgun bir ışıkla aydınlanıyordu. Polis şeridinin arkasında toplanan kalabalık, birbirlerine şüpheyle bakıyordu. Kasabanın en deneyimli dedektifi olarak bu davayı çözmek için buraya çağrıldınız. Beş şüpheli, beş bina, sayısız sır... Gerçeği ortaya çıkarabilecek misiniz?';

function startTypewriter() {
    const el = document.getElementById('story-typewriter');
    const continueBtn = document.getElementById('story-continue-btn');
    
    // Typewriter sesini başlat
    playLoopSound(typewriterSound, 0.4);

    el.textContent = '';
    let i = 0;
    const speed = 40;
    
    function type() {
        if (i < STORY_TEXT.length) {
            el.textContent += STORY_TEXT.charAt(i);
            i++;
            setTimeout(type, speed);
        } else {
            stopSound(typewriterSound);
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
        startAutopsyTimer(); // Oyuna (Haritaya) geçildiğinde sayacı başlat
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
    document.getElementById('talk-npc-name').innerText = npc.name + ' ile Konu\u015F';
    
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
    
    // Kapı gıcırtısı + kapanma sesi
    playSound(doorCreak, 0.7);
    setTimeout(() => {
        playSound(doorClose, 0.5);
    }, 600);

    triggerTransition(() => {
        townMapScreen.classList.add('hidden');
        interiorScreen.classList.remove('hidden');
    });
}

// =============================================================
// 3. CLUE INSPECTION
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
// 5. NPC TALK — KADEMESİZ KARIŞIK SİSTEM
// =============================================================

document.getElementById('talk-npc-btn').addEventListener('click', () => {
    if (!activeNpcId) return;
    openNpcTalk(activeNpcId);
});

function openNpcTalk(npcId) {
    const npc = NPC_DATA[npcId];
    
    // NPC görselini arka plan olarak ayarla (COVER ile tam ekran)
    document.getElementById('npc-talk-bg').style.backgroundImage = `url('${npc.img}')`;
    
    // Chat alanını temizle
    document.getElementById('npc-talk-chat').innerHTML = '';
    
    // NPC mırıltı sesi çal
    if (npcMumble) {
        npcMumble.currentTime = 0;
        playSound(npcMumble, 0.3);
        setTimeout(() => stopSound(npcMumble), 2000);
    }
    
    // Kalan soru sayısını güncelle
    updateQuestionIndicator(npcId);
    
    // Sıradaki mantıksal soruları yükle
    loadContextualQuestions(npcId);
    
    npcTalkModal.classList.remove('hidden');
}

function updateQuestionIndicator(npcId) {
    const asked = askedQuestionCount[npcId] || 0;
    const remainingLimit = 5 - asked;
    document.getElementById('npc-talk-stage').textContent = `Kalan Soru Hakkı: ${remainingLimit}/5`;
}

function loadContextualQuestions(npcId) {
    const container = document.getElementById('npc-talk-buttons');
    container.innerHTML = '<div style="color:var(--text-muted); text-align:center;">Diyaloglar yükleniyor...</div>';
    
    const askedCount = askedQuestionCount[npcId] || 0;
    const categories = ['tanisma', 'derinlesme', 'yuzlestirme', 'baski', 'son'];
    const currentCategory = categories[askedCount] || 'son';
    
    // C# API'den diyalogları çek
    fetch(`/api/game/dialogues?npcId=${npcId}&category=${currentCategory}`)
        .then(res => res.json())
        .then(data => {
            container.innerHTML = '';
            
            if (!data.success || data.dialogues.length === 0 || askedCount >= 5) {
                container.innerHTML = '<div class="npc-talk-end-msg"><i class="fa-solid fa-check-circle"></i> Sorgu tamamlandı. NPC artık konuşmak istemiyor. Geri dönebilirsiniz.</div>';
                npcTalkCompleted[npcId] = true;
                return;
            }
            
            const questionsToShow = data.dialogues;
            questionsToShow.forEach((q, index) => {
                const btn = document.createElement('button');
                btn.className = 'npc-talk-btn';
                btn.innerHTML = `<i class="fa-regular fa-comment-dots"></i> ${q.q}`;
                
                btn.dataset.question = JSON.stringify(q);
                btn.onclick = () => { askQuestionBackend(npcId, q); };
                container.appendChild(btn);
            });
        })
        .catch(err => {
            console.error("Diyalog yükleme hatası:", err);
            container.innerHTML = '<div style="color:red;">Diyaloglar sunucudan alınamadı!</div>';
        });
}

function askQuestionBackend(npcId, question) {
    const npc = NPC_DATA[npcId];
    const chatArea = document.getElementById('npc-talk-chat');
    
    askedQuestionCount[npcId] = (askedQuestionCount[npcId] || 0) + 1;
    
    // Oyuncu mesajı
    const playerMsg = document.createElement('div');
    playerMsg.className = 'npc-talk-message player';
    playerMsg.innerHTML = `<div class="speaker">Dedektif</div><div class="msg-text">${question.q}</div>`;
    chatArea.appendChild(playerMsg);
    
    // Butonları geçici devre dışı bırak
    document.querySelectorAll('.npc-talk-btn').forEach(b => b.disabled = true);
    
    // NPC mırıltı sesi
    if (npcMumble) {
        npcMumble.currentTime = 0;
        playSound(npcMumble, 0.25);
        setTimeout(() => stopSound(npcMumble), 1500);
    }
    
    // NPC cevabını belirle (suçlu NPC'ye göre dinamik cevap)
    let answer = question.a;
    if (question.guiltyResponse && guiltyNpcId && question.guiltyResponse[guiltyNpcId]) {
        answer = question.guiltyResponse[guiltyNpcId];
    }
    
    // NPC cevabı (gecikmeli)
    setTimeout(() => {
        const npcMsg = document.createElement('div');
        npcMsg.className = 'npc-talk-message';
        npcMsg.innerHTML = `<div class="speaker">${npc.name}</div><div class="msg-text">${answer}</div>`;
        chatArea.appendChild(npcMsg);
        chatArea.scrollTop = chatArea.scrollHeight;
        
        // Konuşma geçmişine kaydet
        if (!dialogHistory[npcId]) dialogHistory[npcId] = [];
        dialogHistory[npcId].push({
            player: question.q,
            npc: answer,
            npcName: npc.name,
            difficulty: question.difficulty
        });
        
        // Kalan soru sayısını güncelle
        updateQuestionIndicator(npcId);
        
        setTimeout(() => {
            loadContextualQuestions(npcId);
        }, 500);
        
    }, 800);
}

document.getElementById('npc-talk-close').addEventListener('click', () => {
    npcTalkModal.classList.add('hidden');
    stopSound(npcMumble);
});

// =============================================================
// 6. BAG (ÇANTA) VE DETAYLI İNCELEME
// =============================================================

const detailedClueModal = document.getElementById('detailed-clue-modal');

function inspectClue(clueId) {
    const clue = currentBag.find(c => c.id === clueId);
    if (!clue) return;
    
    // İşaretle: İncelendi
    clue.inspected = true;
    
    // Arayüzü güncelle (Çıkar butonunu disable yap)
    openBag(); 
    
    // Detaylı ekranı aç
    document.getElementById('detailed-clue-title').textContent = clue.name;
    document.getElementById('detailed-clue-img').src = clue.img;
    const detailedText = document.getElementById('detailed-clue-text');
    detailedText.innerHTML = '<span style="opacity:0.5;">Yükleniyor...</span>';
    
    detailedClueModal.classList.remove('hidden');

    // Backend'den dinamik ipucu detayını çek
    fetch(`/api/game/clue-detail/${clue.id}`)
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                // Typewriter effect for detailed text
                detailedText.innerHTML = '';
                let i = 0;
                let text = data.text;
                function type() {
                    if (i < text.length) {
                        detailedText.innerHTML += text.charAt(i);
                        i++;
                        setTimeout(type, 30); // 30ms yazma hızı
                    }
                }
                type();
            }
        })
        .catch(err => {
            detailedText.textContent = clue.desc; // Fallback
        });
}

function removeClue(clueId) {
    const idx = currentBag.findIndex(c => c.id === clueId);
    if (idx > -1) {
        if (currentBag[idx].inspected) {
            alert("İncelenen deliller çantadan çıkarılamaz!");
            return;
        }
        currentBag.splice(idx, 1);
        openBag();
    }
}

document.getElementById('close-detailed-clue-btn')?.addEventListener('click', () => {
    detailedClueModal.classList.add('hidden');
});

function openBag() {
    const bagList = document.getElementById('bag-items-list');
    if (currentBag.length === 0) {
        bagList.innerHTML = '<p style="color: var(--text-muted); text-align:center; padding:30px;">Çanta boş. Binalardaki ipuçlarını toplayın.</p>';
    } else {
        bagList.innerHTML = currentBag.map(b => `
            <div class="bag-item">
                <div class="bag-item-left">
                    <img src="${b.img}" alt="${b.name}">
                    <span>${b.name}</span>
                </div>
                <div class="bag-item-actions">
                    <button class="btn btn-primary" onclick="inspectClue(${b.id})">
                        <i class="fa-solid fa-magnifying-glass"></i> İncele
                    </button>
                    <button class="btn btn-danger" onclick="removeClue(${b.id})" ${b.inspected ? 'disabled style="opacity:0.5;" title="İncelendi, çıkarılamaz"' : ''}>
                        <i class="fa-solid fa-trash"></i> ${b.inspected ? 'Çıkarılamaz' : 'Çıkar'}
                    </button>
                </div>
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
// 7. BULDUM! (SUÇLAMA SİSTEMİ) — KART TAŞMA DÜZELTMESİ
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
        const askedCount = askedQuestionCount[id] || 0;
        
        const card = document.createElement('div');
        card.className = 'found-npc-card';
        card.innerHTML = `
            <img src="${npc.img}" alt="${npc.name}" class="found-npc-img" data-npc-id="${id}" title="Konuşma geçmişini görüntüle">
            <div class="found-npc-name">${npc.name}</div>
            <div class="found-npc-role">${npc.building}</div>
            ${hasHistory ? `<div style="font-size:0.7rem; color:var(--accent);">${askedCount} soru soruldu</div>` : '<div style="font-size:0.7rem; color:var(--text-muted);">Hen\u00FCz konu\u015Fulmad\u0131</div>'}
            <button class="btn btn-outline" onclick="window.showNpcHistory(${id})"><i class="fa-solid fa-comments"></i> Ge\u00E7mi\u015F</button>
            <div class="found-npc-actions">
                <button class="btn btn-danger" onclick="accuseNpc(${id})"><i class="fa-solid fa-handcuffs"></i> Su\u00E7lu</button>
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
    
    document.getElementById('npc-history-title').innerHTML = `<i class="fa-solid fa-comments"></i> ${npc.name} \u2014 Konu\u015Fma Ge\u00E7mi\u015Fi`;
    
    const content = document.getElementById('npc-history-content');
    if (history.length === 0) {
        content.innerHTML = '<p style="color: var(--text-muted); text-align:center; padding:30px;">Bu NPC ile hen\u00FCz konu\u015Fulmad\u0131.</p>';
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

// === ACCUSE NPC (BACKEND CHECK) — YENİ HAPİS ANİMASYONU ===
window.accuseNpc = function(accusedId) {
    const npc = NPC_DATA[accusedId];
    foundModal.classList.add('hidden');
    
    // YENİ HAPİS GÖRSELLERİNİ KULLAN
    const jailImages = {
        1: 'images/jail_hasan.png',
        2: 'images/jail_selma.png',
        3: 'images/jail_kemal.png',
        4: 'images/jail_gunes.png',
        5: 'images/jail_yahya.png'
    };
    
    // Yeni hapis animasyonu — NPC tam boy parmaklıkların arkasında
    const jailNpcFull = document.getElementById('jail-npc-full');
    const jailNpcName = document.getElementById('jail-npc-name');
    const jailArrestedText = document.getElementById('jail-arrested-text');
    
    if (jailNpcFull) jailNpcFull.src = jailImages[npc.id] || npc.img;
    if (jailNpcName) jailNpcName.textContent = npc.name;
    
    // Animasyonu sıfırla
    if (jailNpcFull) {
        jailNpcFull.style.animation = 'none';
        jailNpcFull.offsetHeight; // Force reflow
        jailNpcFull.style.animation = '';
    }
    if (jailArrestedText) {
        jailArrestedText.style.animation = 'none';
        jailArrestedText.offsetHeight;
        jailArrestedText.style.animation = '';
    }
    
    // Jail bars animasyonu da sıfırla
    const jailBars = document.querySelector('.jail-bars');
    if (jailBars) {
        jailBars.style.animation = 'none';
        jailBars.offsetHeight;
        jailBars.style.animation = '';
    }
    
    jailOverlay.classList.remove('hidden');
    
    // Animasyon sırasında API'ye sor
    fetch('/api/game/accuse', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ NpcId: accusedId }) // HATA BURADAYDI, npcId değil accusedId
    })
    .then(res => res.json())
    .then(data => {
        setTimeout(() => {
            jailOverlay.classList.add('hidden');
            
            const resultIcon = document.getElementById('result-icon');
            const resultTitle = document.getElementById('result-title');
            const resultMessage = document.getElementById('result-message');
            const retryBtn = document.getElementById('result-retry-btn');
            
            // Gerçek katil ve seçilen kişi bilgileri
            const realKiller = NPC_DATA[guiltyNpcId];
            const accusedNpc = NPC_DATA[accusedId];
            
            if (data.success) {
                resultIcon.className = 'result-icon success';
                resultIcon.innerHTML = '<i class="fa-solid fa-trophy"></i>';
                resultTitle.textContent = 'TEBRİKLER! KAZANDINIZ!';
                resultTitle.style.color = 'var(--success)';
                resultMessage.innerHTML = `
                    <div class="result-verdict">
                        <div class="result-verdict-line"><i class="fa-solid fa-user-check" style="color:var(--success)"></i> Sizin Seçiminiz: <strong>${accusedNpc.name}</strong></div>
                        <div class="result-verdict-line"><i class="fa-solid fa-handcuffs" style="color:var(--accent)"></i> Gerçek Katil: <strong>${realKiller.name}</strong></div>
                        <div class="result-verdict-correct"><i class="fa-solid fa-circle-check"></i> DOĞRU TAHMİN!</div>
                    </div>
                    <div class="result-story">
                        <div class="result-story-header"><i class="fa-solid fa-book-skull"></i> O Gecenin Hikayesi</div>
                        <div class="result-story-text">${realKiller.murderStory}</div>
                    </div>`;
                retryBtn.innerHTML = '<i class="fa-solid fa-house"></i> Ana Menüye Dön';
                retryBtn.classList.remove('hidden');
            } else {
                resultIcon.className = 'result-icon fail';
                resultIcon.innerHTML = '<i class="fa-solid fa-skull-crossbones"></i>';
                resultTitle.textContent = 'KAYBETTİNİZ!';
                resultTitle.style.color = 'var(--danger)';
                resultMessage.innerHTML = `
                    <div class="result-verdict">
                        <div class="result-verdict-line"><i class="fa-solid fa-user-xmark" style="color:var(--danger)"></i> Sizin Seçiminiz: <strong>${accusedNpc.name}</strong> <span style="color:var(--danger)">(MASUM)</span></div>
                        <div class="result-verdict-line"><i class="fa-solid fa-handcuffs" style="color:var(--accent)"></i> Gerçek Katil: <strong>${realKiller.name}</strong></div>
                        <div class="result-verdict-wrong"><i class="fa-solid fa-circle-xmark"></i> YANLIŞ TAHMİN!</div>
                    </div>
                    <div class="result-story">
                        <div class="result-story-header"><i class="fa-solid fa-book-skull"></i> O Gecenin Hikayesi</div>
                        <div class="result-story-text">${realKiller.murderStory}</div>
                    </div>`;
                retryBtn.innerHTML = '<i class="fa-solid fa-rotate-right"></i> Tekrar Oyna';
                retryBtn.classList.remove('hidden');
            }
            
            resultModal.classList.remove('hidden');
        }, 3000);
    })
    .catch(err => {
        console.error('Accuse Error:', err);
        jailOverlay.classList.add('hidden');
        alert("Ba\u011Flant\u0131 hatas\u0131!");
    });
};

window.innocentNpc = function(npcId) {
    const card = document.querySelector(`.found-npc-card .found-npc-img[data-npc-id="${npcId}"]`)?.closest('.found-npc-card');
    if (card) {
        card.style.opacity = '0.3';
        card.style.pointerEvents = 'none';
    }
};

// === RETRY ===
document.getElementById('result-retry-btn').addEventListener('click', () => {
    resultModal.classList.add('hidden');
    initGame();
    triggerTransition(() => {
        townMapScreen.classList.add('hidden');
        interiorScreen.classList.add('hidden');
        npcTalkModal.classList.add('hidden');
        foundModal.classList.add('hidden');
        splashScreen.classList.remove('hidden');
    });
    stopAllSounds();
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
    stopAllSounds();
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
