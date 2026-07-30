/* ==========================================================================
   🔍 AKILLI DEDEKTİFLİK RPG - OYUN MANTIĞI & API ENTEGRASYONU (JS)
   ========================================================================== */

const API_BASE = '/api/game';

// DOM Elementleri
const splashScreen = document.getElementById('splash-screen');
const gameScreen = document.getElementById('game-screen');
const startBtn = document.getElementById('start-btn');
const resetBtn = document.getElementById('reset-btn');
const restartBtn = document.getElementById('restart-btn');

// Şüpheliler ve İpuçları Alanları
const suspectsList = document.getElementById('suspects-list');
const sceneCluesList = document.getElementById('scene-clues-list');
const bagCluesList = document.getElementById('bag-clues-list');

// Sorgulama Modu
const interrogationModal = document.getElementById('interrogation-modal');
const closeIntBtn = document.getElementById('close-int-btn');
const intNpcName = document.getElementById('int-npc-name');
const intNpcRole = document.getElementById('int-npc-role');
const intNpcImg = document.getElementById('int-npc-img');
const intNpcEmotionBadge = document.getElementById('int-npc-emotion-badge');
const intTrustBar = document.getElementById('int-trust-bar');
const intTrustText = document.getElementById('int-trust-text');
const intFearBar = document.getElementById('int-fear-bar');
const intFearText = document.getElementById('int-fear-text');
const chatHistoryBox = document.getElementById('chat-history-box');
const typingIndicator = document.getElementById('typing-indicator');
const askForm = document.getElementById('ask-form');
const questionInput = document.getElementById('question-input');
const presentCluesContainer = document.getElementById('present-clues-container');
const accuseBtn = document.getElementById('accuse-btn');

// İpucu Karar Modalı
const clueModal = document.getElementById('clue-modal');
const clueDecideTitle = document.getElementById('clue-decide-title');
const clueDecideDesc = document.getElementById('clue-decide-desc');
const keepClueBtn = document.getElementById('keep-clue-btn');
const ignoreClueBtn = document.getElementById('ignore-clue-btn');

// Sonuç Ekranı
const resultModal = document.getElementById('result-modal');
const resultIcon = document.getElementById('result-icon');
const resultTitle = document.getElementById('result-title');
const resultMessage = document.getElementById('result-message');
const secretInfoBox = document.getElementById('secret-info-box');
const resultSecretText = document.getElementById('result-secret-text');

// Aktif Sorgulanan NPC
let activeNpcId = null;
let currentDecidingClueId = null;

// =============================================
// UYGULAMA BAŞLANGICI VE OLAY YÖNETİCİLERİ
// =============================================

document.addEventListener('DOMContentLoaded', () => {
    // Giriş Butonu
    startBtn.addEventListener('click', () => {
        splashScreen.classList.add('hidden');
        gameScreen.classList.remove('hidden');
        loadGameState();
    });

    // Kapat Sorgulama
    closeIntBtn.addEventListener('click', () => {
        interrogationModal.classList.add('hidden');
        activeNpcId = null;
        loadGameState();
    });

    // Soru Sorma Formu
    askForm.addEventListener('submit', (e) => {
        e.preventDefault();
        const question = questionInput.value.trim();
        if (!question) return;
        askQuestion(question);
    });

    // Sıfırlama Butonları
    resetBtn.addEventListener('click', resetGame);
    restartBtn.addEventListener('click', () => {
        resultModal.classList.add('hidden');
        resetGame();
    });

    // Suçlama Butonu
    accuseBtn.addEventListener('click', accuseSuspect);
});

// =============================================
// OYUN DURUMUNU YÜKLE
// =============================================

async function loadGameState() {
    try {
        await Promise.all([
            loadNPCs(),
            loadClues()
        ]);
    } catch (err) {
        console.error("Hata oluştu:", err);
    }
}

// 1. Şüphelileri API'den Getir
async function loadNPCs() {
    const res = await fetch(`${API_BASE}/npcs`);
    const npcs = await res.json();
    
    suspectsList.innerHTML = '';
    
    npcs.forEach(npc => {
        const imagePath = `images/${npc.npcId === 1 ? 'hasan' : npc.npcId === 2 ? 'selma' : 'kemal'}.png`;
        
        const card = document.createElement('div');
        card.className = 'suspect-card animate-fade-in';
        card.innerHTML = `
            <img src="${imagePath}" alt="${npc.name}">
            <div class="suspect-info">
                <h3>${npc.name}</h3>
                <span class="role">${npc.role}</span>
                <div class="suspect-stats">
                    <div class="stat-mini">
                        <label>🤝 Güven:</label>
                        <span>${npc.trustLevel}%</span>
                    </div>
                    <div class="stat-mini">
                        <label>😰 Korku:</label>
                        <span>${npc.fearLevel}%</span>
                    </div>
                </div>
            </div>
        `;
        card.addEventListener('click', () => openInterrogation(npc));
        suspectsList.appendChild(card);
    });
}

// 2. İpuçlarını API'den Getir
async function loadClues() {
    const res = await fetch(`${API_BASE}/clues`);
    const clues = await res.json();

    sceneCluesList.innerHTML = '';
    bagCluesList.innerHTML = '';
    presentCluesContainer.innerHTML = '';

    let bagCount = 0;

    clues.forEach(clue => {
        if (clue.status === 'Pending') {
            // Olay Yeri İpucu
            const row = document.createElement('div');
            row.className = 'clue-item-row';
            row.innerHTML = `
                <div class="clue-meta">
                    <i class="fa-solid fa-magnifying-glass"></i>
                    <span class="clue-title-mini">${clue.title}</span>
                </div>
                <div class="clue-item-actions">
                    <button class="btn btn-primary btn-outline" onclick="openClueDecision(${clue.clueId}, '${clue.title.replace(/'/g, "\\'")}', '${clue.description.replace(/'/g, "\\'")}')">İncele</button>
                </div>
            `;
            sceneCluesList.appendChild(row);
        } else if (clue.status === 'KeptInBag') {
            bagCount++;
            // Çantadaki İpucu
            const card = document.createElement('div');
            card.className = 'inventory-item';
            card.innerHTML = `
                <i class="fa-solid fa-folder-closed"></i>
                <span>${clue.title}</span>
            `;
            card.addEventListener('click', () => alert(`📌 ${clue.title}\n\n${clue.description}`));
            bagCluesList.appendChild(card);

            // Sorgulama sırasında sunulabilecek hap butonu
            const pill = document.createElement('button');
            pill.className = 'evidence-pill';
            pill.innerHTML = `<i class="fa-solid fa-briefcase"></i> ${clue.title}`;
            pill.addEventListener('click', () => {
                questionInput.value = `Elindeki '${clue.title}' kanıtı hakkında ne diyeceksin? (${clue.description})`;
                questionInput.focus();
            });
            presentCluesContainer.appendChild(pill);
        }
    });

    if (bagCount === 0) {
        bagCluesList.innerHTML = `
            <div class="empty-inventory">
                <i class="fa-solid fa-box-open"></i>
                <p>Çantanız şu an boş. Olay yerindeki ipuçlarından saklamak istediklerinizi çantaya ekleyin.</p>
            </div>
        `;
        presentCluesContainer.innerHTML = `<span class="text-muted" style="font-size:0.8rem;">(Çantada sunulacak kanıt yok)</span>`;
    }
}

// =============================================
// İPUCU KARAR AKIŞI (SAKLA / BIRAK)
// =============================================

function openClueDecision(clueId, title, desc) {
    currentDecidingClueId = clueId;
    clueDecideTitle.innerText = title;
    clueDecideDesc.innerText = desc;
    clueModal.classList.remove('hidden');

    keepClueBtn.onclick = () => decideClue('KeptInBag');
    ignoreClueBtn.onclick = () => decideClue('IgnoredAtScene');
}

async function decideClue(action) {
    if (!currentDecidingClueId) return;

    await fetch(`${API_BASE}/clues/${currentDecidingClueId}/action`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status: action })
    });

    clueModal.classList.add('hidden');
    currentDecidingClueId = null;
    loadGameState();
}

// =============================================
// ODAKLI SORGULAMA SİSTEMİ (BLUR MODAL)
// =============================================

function openInterrogation(npc) {
    activeNpcId = npc.npcId;
    intNpcName.innerText = npc.name;
    intNpcRole.innerText = npc.role;
    intNpcImg.src = `images/${npc.npcId === 1 ? 'hasan' : npc.npcId === 2 ? 'selma' : 'kemal'}.png`;
    
    updateNpcStatsUI(npc);
    
    // Geçmiş konuşmaları temizle ve başlangıç mesajını ekle
    chatHistoryBox.innerHTML = `
        <div class="system-message">Sorgulama başladı. ${npc.name} karşınızda duruyor.</div>
    `;
    
    // Envanter haplarını hazırla
    loadClues();

    interrogationModal.classList.remove('hidden');
}

function updateNpcStatsUI(npc) {
    intTrustBar.style.width = `${npc.trustLevel}%`;
    intTrustText.innerText = `${npc.trustLevel}%`;
    intFearBar.style.width = `${npc.fearLevel}%`;
    intFearText.innerText = `${npc.fearLevel}%`;
}

// NPC Duygu Emoji & Rozeti
function getEmotionBadge(emotion) {
    const em = emotion.toLowerCase();
    if (em.includes("sinirli") || em.includes("saldırgan")) return "😡 Sinirli";
    if (em.includes("korkmuş") || em.includes("tedirgin")) return "😰 Tedirgin";
    if (em.includes("sakin")) return "😐 Sakin";
    if (em.includes("samimi")) return "😊 Samimi";
    if (em.includes("pişman")) return "😢 Pişman";
    if (em.includes("şüpheli")) return "🤨 Şüpheli";
    if (em.includes("sessiz") || em.includes("dalgın")) return "😶 Sessiz";
    return `🗣️ ${emotion}`;
}

// 1. Soru Gönder ve Yanıtı Çiz
async function askQuestion(question) {
    if (!activeNpcId) return;

    // Oyuncu Balonu
    appendChatBubble('DEDEKTİF', question, 'player');
    questionInput.value = '';
    
    // Typing indicator
    typingIndicator.classList.remove('hidden');
    chatHistoryBox.scrollTop = chatHistoryBox.scrollHeight;

    try {
        const res = await fetch(`${API_BASE}/interrogate`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ npcId: activeNpcId, question: question })
        });
        const data = await res.json();

        typingIndicator.classList.add('hidden');

        // NPC Balonu
        appendChatBubble(data.updatedNpc.name, data.dialogue, 'npc');

        // Canlı Stat Güncelle
        updateNpcStatsUI(data.updatedNpc);
        
        // Rozet Güncelle
        intNpcEmotionBadge.innerText = getEmotionBadge(data.emotion);

        // Güven seviyesine göre renkli bildirim
        const diffText = data.trustChange > 0 ? `+${data.trustChange}` : `${data.trustChange}`;
        const changeMsg = document.createElement('div');
        changeMsg.className = 'system-message';
        changeMsg.style.borderColor = data.trustChange >= 0 ? 'var(--success)' : 'var(--danger)';
        changeMsg.innerHTML = `📊 Güven Değişimi: <strong>${diffText}</strong>`;
        chatHistoryBox.appendChild(changeMsg);

        // Sır açığa çıkarsa
        if (data.revealedSecret) {
            const secretMsg = document.createElement('div');
            secretMsg.className = 'system-message';
            secretMsg.style.backgroundColor = 'rgba(207, 34, 46, 0.15)';
            secretMsg.style.borderColor = 'var(--danger)';
            secretMsg.innerHTML = `🔑 <strong>Açığa Çıkan Sır:</strong> ${data.revealedSecret}`;
            chatHistoryBox.appendChild(secretMsg);
        }

        chatHistoryBox.scrollTop = chatHistoryBox.scrollHeight;

    } catch (err) {
        typingIndicator.classList.add('hidden');
        appendChatBubble('SİSTEM', 'Bir API hatası oluştu.', 'player');
    }
}

function appendChatBubble(speaker, text, type) {
    const bubble = document.createElement('div');
    bubble.className = `chat-bubble ${type}`;
    bubble.innerHTML = `
        <div class="speaker-name">${speaker}</div>
        <div class="bubble-content">${text}</div>
    `;
    chatHistoryBox.appendChild(bubble);
    chatHistoryBox.scrollTop = chatHistoryBox.scrollHeight;
}

// =============================================
// KATİLİ SUÇLAMA MEKANİZMASI
// =============================================

async function accuseSuspect() {
    if (!activeNpcId) return;

    if (!confirm("Bu şüpheliyi resmen katil ilan etmek istediğinizden emin misiniz?")) {
        return;
    }

    try {
        const res = await fetch(`${API_BASE}/accuse`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ npcId: activeNpcId })
        });
        const result = await res.json();

        interrogationModal.classList.add('hidden');
        activeNpcId = null;

        // Sonuç modalını göster
        resultTitle.innerText = result.success ? "DAVA ÇÖZÜLDÜ!" : "BAŞARISIZ SORUŞTURMA";
        resultMessage.innerText = result.message;
        resultIcon.className = `result-icon ${result.success ? 'success' : 'fail'}`;
        resultIcon.innerHTML = result.success 
            ? `<i class="fa-solid fa-circle-check"></i>`
            : `<i class="fa-solid fa-circle-xmark"></i>`;

        if (result.success && result.secret) {
            secretInfoBox.classList.remove('hidden');
            resultSecretText.innerText = result.secret;
        } else {
            secretInfoBox.classList.add('hidden');
        }

        resultModal.classList.remove('hidden');

    } catch (err) {
        alert("Suçlama işlemi sırasında bir hata oluştu.");
    }
}

// =============================================
// OYUNU SIFIRLA
// =============================================

async function resetGame() {
    try {
        const res = await fetch(`${API_BASE}/reset`, { method: 'POST' });
        const data = await res.json();
        alert(data.message);
        loadGameState();
    } catch (err) {
        alert("Oyun sıfırlanırken hata oluştu.");
    }
}
