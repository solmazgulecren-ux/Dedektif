const splashScreen = document.getElementById('splash-screen');
const townMapScreen = document.getElementById('town-map-screen');
const interiorScreen = document.getElementById('interior-screen');
const interrogationModal = document.getElementById('interrogation-modal');
const bagModal = document.getElementById('bag-modal');
const objDescModal = document.getElementById('object-desc-modal');
const transitionOverlay = document.getElementById('transition-overlay');

// Variables
let currentNpcs = [];
let currentBag = [];
const MAX_BAG_SIZE = 3;
let activeNpcId = null;
let currentPendingObject = null;

// Mock Data
const MOCK_NPCS = {
    1: { id: 1, name: 'Kasap Hasan', bg: 'images/butcher_interior.png' },
    2: { id: 2, name: 'Eczacı Selma', bg: 'images/apothecary_interior.png' },
    3: { id: 3, name: 'Muhtar Kemal', bg: 'images/town_hall_interior.png' }
};

const MOCK_OBJECTS = {
    1: [
        { id: 1, name: 'Kanlı Satır', desc: 'Tezgaha sertçe saplanmış, üzerinde taze lekeler olan paslı bir satır.', top: '40%', left: '30%', img: 'images/bloody_cleaver.png' },
        { id: 2, name: 'Kara Kaplı Defter', desc: 'Veresiye listesinde kurbanın isminin üzeri kırmızı kalemle çizilmiş.', top: '60%', left: '70%', img: 'images/black_notebook.png' },
        { id: 3, name: 'Yırtık Önlük', desc: 'Kavga izleri taşıyan, yakası kopmuş bir kasap önlüğü.', top: '80%', left: '20%', img: 'images/torn_apron.png' }
    ],
    2: [
        { id: 4, name: 'Boş İlaç Şişesi', desc: 'Zehirli olduğu bilinen, reçetesiz satılmayan ağır bir ilacın boş şişesi.', top: '50%', left: '20%', img: 'images/empty_medicine_bottle.png' },
        { id: 5, name: 'Reçete Defteri', desc: 'Kurbanın adının geçtiği, son sayfaları aceleyle yırtılmış defter.', top: '70%', left: '80%', img: 'images/prescription_notebook.png' },
        { id: 6, name: 'Zehirli Sarmaşık', desc: 'Tezgah altında kurumaya bırakılmış zehirli bir bitki türü.', top: '30%', left: '60%', img: 'images/poison_ivy.png' }
    ],
    3: [
        { id: 7, name: 'Tehdit Mektubu', desc: 'Muhtarın çekmecesinde kurbana yazılmış, henüz gönderilmemiş bir tehdit mektubu.', top: '60%', left: '40%', img: 'images/threat_letter.png' },
        { id: 8, name: 'Kırık Gözlük', desc: 'Kurbana ait olduğu düşünülen, camı kırık bir okuma gözlüğü.', top: '30%', left: '70%', img: 'images/broken_glasses.png' },
        { id: 9, name: 'Gizli Kasa', desc: 'Tablonun arkasında şifresi açık unutulmuş para dolu kasa.', top: '80%', left: '30%', img: 'images/default_clue.png' }
    ]
};

// Start Game
document.getElementById('start-btn').addEventListener('click', () => {
    triggerTransition(() => {
        splashScreen.classList.add('hidden');
        townMapScreen.classList.remove('hidden');
    });
});

// Exit to main menu
document.querySelectorAll('.global-exit-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
        if(e.target.id === 'leave-building-btn') {
            triggerTransition(() => {
                interiorScreen.classList.add('hidden');
                townMapScreen.classList.remove('hidden');
            });
        } else {
            triggerTransition(() => {
                townMapScreen.classList.add('hidden');
                splashScreen.classList.remove('hidden');
            });
        }
    });
});

// Open Bag
document.querySelectorAll('.global-bag-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        const bagList = document.getElementById('bag-items-list');
        bagList.innerHTML = currentBag.length === 0 ? '<p>Çanta boş.</p>' : currentBag.map(b => `<div style="border:1px solid #555; padding:10px; margin:5px; display:flex; align-items:center; gap:10px;"><img src="${b.img}" style="width:30px;"> ${b.name}</div>`).join('');
        bagModal.classList.remove('hidden');
    });
});
document.getElementById('close-bag-btn').addEventListener('click', () => {
    bagModal.classList.add('hidden');
});

// Click Building
document.querySelectorAll('.map-building:not(.inactive)').forEach(b => {
    b.addEventListener('click', () => {
        const npcId = parseInt(b.getAttribute('data-npc-id'));
        openBuilding(npcId);
    });
});

function openBuilding(npcId) {
    activeNpcId = npcId;
    const npc = MOCK_NPCS[npcId];
    if(!npc) return;
    
    // Set Background
    interiorScreen.style.backgroundImage = `url('${npc.bg}')`;
    document.getElementById('talk-npc-name').innerText = npc.name + " ile Konuş";
    
    // Load Hotspots with Images
    const container = document.getElementById('hotspots-container');
    container.innerHTML = '';
    const objects = MOCK_OBJECTS[npcId] || [];
    objects.forEach(obj => {
        const spotContainer = document.createElement('div');
        spotContainer.style.position = 'absolute';
        spotContainer.style.top = obj.top;
        spotContainer.style.left = obj.left;
        
        const img = document.createElement('img');
        img.src = obj.img ? obj.img : 'images/default_clue.png';
        img.className = 'hotspot-img';
        
        spotContainer.appendChild(img);
        spotContainer.addEventListener('click', () => {
            currentPendingObject = obj;
            // Dinamik Modal İçeriği
            objDescModal.innerHTML = `
            <div class="clue-decide-card" style="margin: auto; margin-top: 10%;">
                <div class="text-section">
                    <h3 style="color:var(--text-bright); margin-bottom:10px;">${obj.name}</h3>
                    <p style="color:var(--text-muted); font-size:1.1rem;">${obj.desc}</p>
                    
                    <div style="margin-top:20px; display:flex; gap:10px; flex-direction:column;">
                        <button onclick="takeItem()" class="btn btn-primary">Çantaya Al</button>
                        <button onclick="leaveItem()" class="btn btn-outline">Olay Yerinde Bırak</button>
                    </div>
                </div>
                <div class="image-section">
                    <img src="${obj.img}" alt="${obj.name}">
                </div>
            </div>`;
            objDescModal.classList.remove('hidden');
        });
        container.appendChild(spotContainer);
    });

    triggerTransition(() => {
        townMapScreen.classList.add('hidden');
        interiorScreen.classList.remove('hidden');
    }, 'open');
}

window.takeItem = function() {
    if(currentBag.length >= MAX_BAG_SIZE) {
        alert("Çantanız doldu! Yeni bir eşya alabilmek için mevcut eşyalarla soruşturmayı ilerletin.");
        objDescModal.classList.add('hidden');
        return;
    }
    if(!currentBag.find(b => b.id === currentPendingObject.id)) {
        currentBag.push(currentPendingObject);
    }
    objDescModal.classList.add('hidden');
};

window.leaveItem = function() {
    objDescModal.classList.add('hidden');
};

// Interrogation
document.getElementById('talk-npc-btn').addEventListener('click', () => {
    document.getElementById('int-npc-name').innerText = MOCK_NPCS[activeNpcId].name;
    document.getElementById('chat-history-box').innerHTML = '<div class="system-message">Sorgulama başladı. Seçeneklerden birine tıklayın.</div>';
    loadDialogOptions(activeNpcId);
    interrogationModal.classList.remove('hidden');
});
document.getElementById('close-int-btn').addEventListener('click', () => interrogationModal.classList.add('hidden'));

function loadDialogOptions(npcId) {
    const container = document.getElementById('dialog-options-container');
    container.innerHTML = '';
    
    // Hikayeli seçenekler veritabanı mock'u
    let options = [];
    if(npcId === 1) { // Kasap
        options = [
            { text: 'Cinayet gecesi tam olarak neredeydin?', req: null },
            { text: 'Kurbanla aranızdaki husumeti herkes biliyor...', req: null },
            { text: 'Bu kanlı satır senin tezgahından çıktı!', req: 1 }, 
            { text: 'Kara kaplı defterinde kurbanın üstü neden çizili?', req: 2 } 
        ];
    } else if (npcId === 2) { // Eczacı
        options = [
            { text: 'Kasabadaki zehirlenme vakalarından haberin var mı?', req: null },
            { text: 'Cinayet saati dükkanın açıktı, kimi gördün?', req: null },
            { text: 'Bu boş ilaç şişesindeki zehri kime sattın?', req: 4 }, 
            { text: 'Reçete defterinin son sayfasını neden yırttın?', req: 5 } 
        ];
    } else if (npcId === 3) { // Muhtar
        options = [
            { text: 'Kasabadaki gerginliğin sebebi nedir muhtar?', req: null },
            { text: 'Kurbanın ölümü sana yaradı diyorlar.', req: null },
            { text: 'Kurbana yazılan bu tehdit mektubunun senin çekmecende ne işi var?!', req: 7 }, 
            { text: 'Kırık gözlük olay yerinde bulundu, bu sana mı ait?', req: 8 } 
        ];
    }

    let count = 0;
    options.forEach(opt => {
        if(opt.req !== null && !currentBag.find(b => b.id === opt.req)) return;
        count++;

        const btn = document.createElement('button');
        btn.className = 'dialog-option-btn';
        btn.innerHTML = `<i class="fa-solid fa-comment"></i> ${opt.text}`;
        btn.onclick = () => askQuestion(npcId, opt.text, opt.req);
        container.appendChild(btn);
    });

    while(count < 4) {
        const btn = document.createElement('button');
        btn.className = 'dialog-option-btn inactive';
        btn.style.opacity = '0.5';
        btn.innerHTML = `<i class="fa-solid fa-lock"></i> Gizli Seçenek (İpucu Gerektirir)`;
        container.appendChild(btn);
        count++;
    }
}

function askQuestion(npcId, q, reqId) {
    const box = document.getElementById('chat-history-box');
    box.innerHTML += `<div class="chat-bubble player"><div class="speaker-name">Dedektif</div><div class="bubble-content">${q}</div></div>`;
    
    setTimeout(() => {
        let answer = "Bu konuda konuşmak istemiyorum.";
        
        // Custom Answers
        if(npcId === 1) {
            if(reqId === 1) answer = "O satırı çalındığını polise söylemiştim, benimle ilgisi yok! Terleyerek ve titreyerek...";
            else if (reqId === 2) answer = "Veresiye borcu vardı, ödemeyince üstünü çizdim. Hepsi bu kadar.";
            else answer = "Bütün gece dükkandaydım, et doğruyordum. Kimseyi görmedim.";
        }
        else if (npcId === 2) {
            if(reqId === 4) answer = "O... o ilacı ben kimseye satmadım. Belki biri tezgahtan çalmıştır.";
            else if(reqId === 5) answer = "Orada önemli bir not vardı, sadece yanlış yazdım ve kopardım!";
            else answer = "Ben sadece ilaç satarım dedektif bey, insanların ne yaptığıyla ilgilenmem.";
        }
        else if (npcId === 3) {
            if(reqId === 7) answer = "Bu... bunu ona göndermeyecektim! Sadece sinirle yazılmış bir şeydi.";
            else if(reqId === 8) answer = "Benim gözlüğüm bende duruyor! Görmüyor musun gözümde işte.";
            else answer = "Kasabanın huzurunu sağlamak benim görevim. Sizin gibi dışarıdan gelenler suyu bulandırıyor.";
        }
        
        box.innerHTML += `<div class="chat-bubble npc"><div class="speaker-name">${MOCK_NPCS[npcId].name}</div><div class="bubble-content">${answer}</div></div>`;
        box.scrollTop = box.scrollHeight;
    }, 800);
}

let audioCtx = null;
function triggerTransition(callback, type='open') {
    transitionOverlay.classList.add('flash');
    setTimeout(() => {
        callback();
        setTimeout(() => transitionOverlay.classList.remove('flash'), 300);
    }, 500);
}
