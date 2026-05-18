const connection = new signalR.HubConnectionBuilder()
    .withUrl("/queueHub")
    .withAutomaticReconnect()
    .build();

let audioEnabled = false;
const audio = document.getElementById("dingDong");
const overlay = document.getElementById("audioOverlay");
const patientNumberEl = document.getElementById("patientNumber");
const justCalledBadge = document.getElementById("justCalledBadge");

function enableAudio() {
    if (!audioEnabled) {
        audio.play().then(() => {
            audio.pause();
            audio.currentTime = 0;
            audioEnabled = true;
            if (overlay) overlay.style.opacity = "0";
            setTimeout(() => { if (overlay) overlay.style.display = "none"; }, 300);
        }).catch(() => {});
    }
}

if (overlay) {
    overlay.addEventListener("click", enableAudio);
}

document.addEventListener("click", enableAudio, { once: true });

function animateNewCall() {
    patientNumberEl.classList.remove("pulse");
    void patientNumberEl.offsetWidth;
    patientNumberEl.classList.add("pulse");

    justCalledBadge.classList.remove("visible", "fade-out");
    void justCalledBadge.offsetWidth;
    justCalledBadge.classList.add("visible");

    setTimeout(() => {
        justCalledBadge.classList.remove("visible");
        justCalledBadge.classList.add("fade-out");
    }, 5000);
}

connection.on("NewCall", (state) => {
    document.getElementById("roomLabel").textContent = state.roomNumber;
    document.getElementById("patientNumber").textContent = state.patientNumber;

    if (audioEnabled) {
        audio.currentTime = 0;
        audio.play().catch(() => {});
    }

    animateNewCall();

    const recentList = document.getElementById("recentList");
    recentList.innerHTML = "";

    if (state.recentCalls && state.recentCalls.length > 0) {
        state.recentCalls.slice(0, 4).forEach((call, index) => {
            const item = document.createElement("div");
            item.className = "recent-item";
            item.style.animationDelay = `${index * 0.05}s`;
            item.innerHTML = `
                <span class="recent-patient">${call.patientNumber}</span>
                <div class="recent-room">
                    <svg class="recent-icon" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>
                    </svg>
                    ${call.roomNumber}
                </div>
            `;
            recentList.appendChild(item);
        });
    } else {
        for (let i = 0; i < 4; i++) {
            const item = document.createElement("div");
            item.className = "recent-item empty-state";
            item.innerHTML = `<span>—</span>`;
            recentList.appendChild(item);
        }
    }
});

connection.on("QueueReset", () => {
    document.getElementById("roomLabel").textContent = "—";
    document.getElementById("patientNumber").textContent = "—";
    const recentList = document.getElementById("recentList");
    recentList.innerHTML = "";
    for (let i = 0; i < 4; i++) {
        const item = document.createElement("div");
        item.className = "recent-item empty-state";
        item.innerHTML = `<span>—</span>`;
        recentList.appendChild(item);
    }
    justCalledBadge.classList.remove("visible");
});

connection.on("RequestStateSync", () => {
    connection.send("RequestCurrentState");
});

async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR connected");
    } catch (err) {
        console.error("SignalR connection error:", err);
        setTimeout(() => startConnection(), 5000);
    }
}

startConnection();

function updateClock() {
    const now = new Date();
    const hours = now.getHours().toString().padStart(2, "0");
    const minutes = now.getMinutes().toString().padStart(2, "0");
    const seconds = now.getSeconds().toString().padStart(2, "0");
    document.getElementById("clock").textContent = `${hours}:${minutes}:${seconds}`;

    const options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
    document.getElementById("dateDisplay").textContent = now.toLocaleDateString('en-MY', options);
}

updateClock();
setInterval(updateClock, 1000);
