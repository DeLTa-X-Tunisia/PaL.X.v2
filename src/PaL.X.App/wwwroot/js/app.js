// State
let currentUser = { id: 0, name: "Moi" };
let currentRecipient = { id: 0, name: "Contact" };
let smileyData = {};

// DOM Elements
const chatHistory = document.getElementById('chat-history');
const messageInput = document.getElementById('message-input');
const sendBtn = document.getElementById('send-btn');
const smileyBtn = document.getElementById('smiley-btn');
const smileyPicker = document.getElementById('smiley-picker');
const smileyTabs = document.getElementById('smiley-tabs');
const smileyGrid = document.getElementById('smiley-grid');

// --- Initialization ---

window.chrome.webview.addEventListener('message', event => {
    const data = event.data;
    
    switch (data.type) {
        case 'init':
            console.log("Core connected");
            break;
        case 'setUserInfo':
            currentUser = data.payload.user;
            currentRecipient = data.payload.recipient;
            updateHeader();
            break;
        case 'addMessage':
            addMessageToUI(data.payload);
            break;
        case 'loadSmileys':
            loadSmileys(data.payload);
            break;
    }
});

// Notify Core we are ready
window.chrome.webview.postMessage({ type: 'ready' });

// --- UI Logic ---

function updateHeader() {
    document.getElementById('recipient-name').innerText = currentRecipient.name;
    // Update avatar/status if provided
}

function scrollToBottom() {
    chatHistory.scrollTop = chatHistory.scrollHeight;
}

function addMessageToUI(msg) {
    // msg: { id, content, timestamp, isMe, senderName }
    
    const row = document.createElement('div');
    row.className = `message-row ${msg.isMe ? 'sent' : 'received'}`;
    
    const bubble = document.createElement('div');
    bubble.className = 'message-bubble';
    bubble.innerHTML = msg.content; // Warning: Ensure content is sanitized in C# or here
    
    const meta = document.createElement('div');
    meta.className = 'message-meta';
    meta.innerText = msg.timestamp;
    
    bubble.appendChild(meta);
    row.appendChild(bubble);
    
    chatHistory.appendChild(row);
    scrollToBottom();
}

function sendMessage() {
    const content = messageInput.innerHTML;
    if (!content.trim() && !content.includes('<img')) return;
    
    // Optimistic UI update (optional, can wait for server echo)
    // addMessageToUI({ 
    //     id: 'temp-' + Date.now(), 
    //     content: content, 
    //     timestamp: new Date().toLocaleTimeString(), 
    //     isMe: true 
    // });

    window.chrome.webview.postMessage({
        type: 'sendMessage',
        content: content
    });
    
    messageInput.innerHTML = '';
    smileyPicker.classList.add('hidden');
}

// --- Smiley Logic ---

function toggleSmileyPicker() {
    smileyPicker.classList.toggle('hidden');
    if (!smileyPicker.classList.contains('hidden') && Object.keys(smileyData).length === 0) {
        smileyTabs.innerHTML = '<div style="padding:10px; color:#666;">Chargement...</div>';
        window.chrome.webview.postMessage({ type: 'requestSmileys' });
    }
}

function loadSmileys(data) {
    smileyData = data;
    renderSmileyTabs();
}

function renderSmileyTabs() {
    smileyTabs.innerHTML = '';
    const categories = Object.keys(smileyData);
    
    if (categories.length === 0) {
        smileyTabs.innerHTML = '<div>Aucun smiley</div>';
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

    renderSmileyGrid(categories[0]);
}

function renderSmileyGrid(category) {
    smileyGrid.innerHTML = '';
    const files = smileyData[category];
    
    files.forEach(file => {
        const item = document.createElement('div');
        item.className = 'smiley-item';
        
        const img = document.createElement('img');
        // Assuming virtual host mapping: https://smileys.pal.x/Category/File.png
        // Or flat if mapped to flat folder. Let's assume flat for now based on previous context, 
        // OR structured if we map the root assets folder.
        // Let's assume the C# maps "smileys.pal.x" to the root "Smiley" folder.
        // So src is https://smileys.pal.x/Category/File.png
        
        // If category is "Défaut" (flat), path is just file.
        const src = category === "Défaut" 
            ? `https://smileys.pal.x/${file}`
            : `https://smileys.pal.x/${category}/${file}`;

        img.src = src;
        img.onclick = () => insertSmiley(src);
        
        item.appendChild(img);
        smileyGrid.appendChild(item);
    });
}

function insertSmiley(url) {
    const imgHtml = `<img src="${url}" style="vertical-align: middle; width: 24px; height: 24px;">`;
    messageInput.focus();
    document.execCommand('insertHTML', false, imgHtml + '&nbsp;');
}

// --- Event Listeners ---

sendBtn.addEventListener('click', sendMessage);

messageInput.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

smileyBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    toggleSmileyPicker();
});

document.addEventListener('click', (e) => {
    if (!smileyPicker.contains(e.target) && e.target !== smileyBtn) {
        smileyPicker.classList.add('hidden');
    }
});

// --- Drag & Drop Images ---

const dropZone = document.body;

dropZone.addEventListener('dragover', (e) => {
    e.preventDefault();
    e.stopPropagation();
    dropZone.style.backgroundColor = "rgba(0,0,0,0.05)";
});

dropZone.addEventListener('dragleave', (e) => {
    e.preventDefault();
    e.stopPropagation();
    dropZone.style.backgroundColor = "";
});

dropZone.addEventListener('drop', (e) => {
    e.preventDefault();
    e.stopPropagation();
    dropZone.style.backgroundColor = "";

    const files = e.dataTransfer.files;
    if (files.length > 0) {
        handleFiles(files);
    }
});

document.getElementById('image-btn').addEventListener('click', () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';
    input.onchange = (e) => {
        if (e.target.files.length > 0) {
            handleFiles(e.target.files);
        }
    };
    input.click();
});

function handleFiles(files) {
    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        if (file.type.startsWith('image/')) {
            const reader = new FileReader();
            reader.onload = (e) => {
                const base64 = e.target.result;
                // Send image to C#
                window.chrome.webview.postMessage({
                    type: 'sendImage',
                    payload: {
                        name: file.name,
                        data: base64
                    }
                });
            };
            reader.readAsDataURL(file);
        }
    }
}
