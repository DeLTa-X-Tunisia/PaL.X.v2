const chatHistory = document.getElementById('chat-history');
const messageInput = document.getElementById('message-input');
const sendBtn = document.getElementById('send-btn');
const smileyBtn = document.getElementById('smiley-btn');
const smileyPicker = document.getElementById('smiley-picker');
const smileyTabs = document.getElementById('smiley-tabs');
const smileyGrid = document.getElementById('smiley-grid');

let smileyData = {}; // { "Category": ["file1.png", "file2.gif"] }

// --- Gestion de l'interface ---

function scrollToBottom() {
    chatHistory.scrollTop = chatHistory.scrollHeight;
}

// Fonction pour ajouter un message (appelée par C# ou localement pour test)
function addMessage(message) {
    // message attendu : { id, content, timestamp, isMe, status }
    
    const rowDiv = document.createElement('div');
    rowDiv.className = `message-row ${message.isMe ? 'sent' : 'received'}`;
    rowDiv.dataset.id = message.id;

    const bubbleDiv = document.createElement('div');
    bubbleDiv.className = 'message-bubble';
    
    // TODO: Intégrer DOMPurify ici : bubbleDiv.innerHTML = DOMPurify.sanitize(message.content);
    // Pour l'instant, on insère tel quel (ATTENTION XSS si non sécurisé en amont)
    bubbleDiv.innerHTML = message.content;

    const metaDiv = document.createElement('div');
    metaDiv.className = 'message-meta';
    metaDiv.innerText = message.timestamp;

    bubbleDiv.appendChild(metaDiv);
    rowDiv.appendChild(bubbleDiv);
    
    chatHistory.appendChild(rowDiv);
    scrollToBottom();
}

// --- Gestion des Smileys ---

function toggleSmileyPicker() {
    smileyPicker.classList.toggle('hidden');
    if (!smileyPicker.classList.contains('hidden')) {
        
        // Si vide, on affiche un message de chargement
        if (Object.keys(smileyData).length === 0) {
            smileyTabs.innerHTML = '<div style="padding:10px; font-size:12px; color:#666;">Chargement des smileys...</div>';
            
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ type: 'requestSmileys' });
            } else {
                console.warn("WebView2 context missing");
                smileyTabs.innerHTML = '<div style="padding:10px; font-size:12px; color:red;">Erreur: WebView2 non détecté</div>';
            }
        }
    }
}

function loadSmileys(data) {
    // alert("Smileys reçus: " + JSON.stringify(data)); // Debug brutal si besoin
    smileyData = data || {};
    renderSmileyTabs();
}

function renderSmileyTabs() {
    smileyTabs.innerHTML = '';
    const categories = Object.keys(smileyData);
    
    if (categories.length === 0) {
        smileyTabs.innerHTML = '<div style="padding:10px; font-size:12px; color:#666;">Aucun smiley trouvé</div>';
        return;
    }

    categories.forEach((cat, index) => {
        const tab = document.createElement('div');
        tab.className = `smiley-tab ${index === 0 ? 'active' : ''}`;
        tab.innerText = cat;
        tab.onclick = () => {
            document.querySelectorAll('.smiley-tab').forEach(t => t.classList.remove('active'));
            tab.classList.add('active');
            renderSmileyGrid(cat);
        };
        smileyTabs.appendChild(tab);
    });

    // Select first category by default
    const activeTab = document.querySelector('.smiley-tab.active');
    if (activeTab) {
        renderSmileyGrid(categories[0]);
    } else if (categories.length > 0) {
        // Fallback if no active class set (shouldn't happen with index===0 logic)
        renderSmileyGrid(categories[0]);
    }
}

function renderSmileyGrid(category) {
    smileyGrid.innerHTML = '';
    const files = smileyData[category];
    
    if (!files) return;

    files.forEach(file => {
        const item = document.createElement('div');
        item.className = 'smiley-item';
        
        const img = document.createElement('img');
        // URL virtuelle mappée (Note: le dossier mappé est plat maintenant, donc pas de sous-dossier category dans l'URL)
        // Si on a mappé "smileys.pal.x" -> "PaL.X.Assets\Smiley", alors l'URL est "https://smileys.pal.x/file.png"
        // Mais attention, si on ajoute des catégories plus tard, il faudra adapter.
        // Pour l'instant, comme on charge tout dans "Défaut" depuis un dossier plat, on enlève la catégorie de l'URL.
        
        img.src = `https://smileys.pal.x/${file}`;
        img.title = file;
        
        item.onclick = () => insertSmiley(category, file);
        item.appendChild(img);
        smileyGrid.appendChild(item);
    });
}

function insertSmiley(category, file) {
    // Même logique pour l'insertion
    const imgHtml = `<img src="https://smileys.pal.x/${file}" style="vertical-align: middle; width: 24px; height: 24px;">`;
    
    messageInput.focus();
    document.execCommand('insertHTML', false, imgHtml + '&nbsp;');
}

// Fermer le picker si on clique ailleurs
document.addEventListener('click', (e) => {
    if (!smileyPicker.contains(e.target) && !smileyBtn.contains(e.target)) {
        smileyPicker.classList.add('hidden');
    }
});

// --- Communication vers C# ---

function sendMessage() {
    // On récupère le HTML pour garder le formatage (gras, smileys...)
    // On devra le nettoyer côté C# aussi
    const content = messageInput.innerHTML; 
    
    // Vérification basique si vide (en tenant compte des balises vides)
    if (!messageInput.innerText.trim() && !content.includes('<img')) return;

    const messagePayload = {
        type: 'sendMessage',
        content: content
    };

    // Envoi à C# via WebView2
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(messagePayload);
    } else {
        console.log('Mode hors WebView2 (Debug):', messagePayload);
    }
    
    // Reset de l'input
    messageInput.innerHTML = '';
    smileyPicker.classList.add('hidden');
}

// --- Événements ---

sendBtn.addEventListener('click', sendMessage);
smileyBtn.addEventListener('click', toggleSmileyPicker);

messageInput.addEventListener('keydown', (e) => {
    // Entrée pour envoyer, Shift+Entrée pour saut de ligne
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

// --- Réception depuis C# ---

if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', event => {
        const data = event.data;
        
        switch (data.type) {
            case 'newMessage':
                addMessage(data.payload);
                break;
            case 'loadHistory':
                chatHistory.innerHTML = '';
                data.payload.forEach(msg => addMessage(msg));
                break;
            case 'loadSmileys':
                loadSmileys(data.payload);
                break;
            case 'setTheme':
                // TODO: Appliquer des couleurs personnalisées si besoin
                break;
        }
    });
}
