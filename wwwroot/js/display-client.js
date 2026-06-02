const connection = new signalR.HubConnectionBuilder()
    .withUrl("/queueHub")
    .withAutomaticReconnect()
    .build();

let audioEnabled = false;
let wakeLock = null;
let fsTimer = null;

const audio = document.getElementById("dingDong");
const overlay = document.getElementById("audioOverlay");
const patientNumberEl = document.getElementById("patientNumber");
const justCalledBadge = document.getElementById("justCalledBadge");
const syncDot = document.getElementById("syncDot");
const syncLabel = document.getElementById("syncLabel");
const reconnectBanner = document.getElementById("reconnectBanner");
const fullscreenBtn = document.getElementById("fullscreenBtn");
const fsExpandIcon = document.getElementById("fsExpandIcon");
const fsCompressIcon = document.getElementById("fsCompressIcon");

function setSyncStatus(status) {
    syncDot.className = "sync-dot";
    if (status === "connected") {
        syncDot.classList.add("connected");
        syncLabel.textContent = "Connected";
        reconnectBanner.classList.remove("visible", "reconnecting", "offline");
    } else if (status === "reconnecting") {
        syncDot.classList.add("reconnecting");
        syncLabel.textContent = "Reconnecting...";
        reconnectBanner.className = "reconnect-banner visible reconnecting";
        reconnectBanner.textContent = "Reconnecting...";
    } else {
        syncDot.classList.add("disconnected");
        syncLabel.textContent = "Disconnected";
        reconnectBanner.className = "reconnect-banner visible offline";
        reconnectBanner.textContent = "Offline — Check your connection";
    }
}

connection.onreconnecting(() => setSyncStatus("reconnecting"));
connection.onreconnected(() => setSyncStatus("connected"));
connection.onclose(() => setSyncStatus("disconnected"));

async function requestWakeLock() {
    try {
        if ("wakeLock" in navigator) {
            wakeLock = await navigator.wakeLock.request("screen");
            wakeLock.addEventListener("release", () => { wakeLock = null; });
        }
    } catch (err) {
        console.log("Wake Lock unavailable:", err);
    }
}

document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible" && !wakeLock && audioEnabled) {
        requestWakeLock();
    }
});

function enableAudio() {
    if (!audioEnabled) {
        audio.play().then(() => {
            audio.pause();
            audio.currentTime = 0;
            audioEnabled = true;
            if (overlay) {
                overlay.style.opacity = "0";
                setTimeout(() => { if (overlay) overlay.style.display = "none"; }, 300);
            }
            requestWakeLock();
        }).catch(() => {});
    }
}

if (overlay) {
    overlay.addEventListener("click", enableAudio);
}

document.addEventListener("click", enableAudio, { once: true });

document.addEventListener("keydown", function keyHandler(e) {
    if (e.key === "Enter" || e.key === " ") {
        if (overlay && overlay.style.display !== "none") {
            enableAudio();
        }
    }
});

function isCompatMode() {
    return document.documentElement.classList.contains("compat-mode");
}

function animateNewCall() {
    if (isCompatMode()) return;

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
        setSyncStatus("connected");
        await connection.invoke("RequestCurrentState");
    } catch (err) {
        console.error("SignalR connection error:", err);
        setSyncStatus("disconnected");
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

function updateFsIcons(isFullscreen) {
    fsExpandIcon.style.display = isFullscreen ? "none" : "block";
    fsCompressIcon.style.display = isFullscreen ? "block" : "none";
}

fullscreenBtn.addEventListener("click", () => {
    if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen().catch(() => {});
    } else {
        document.exitFullscreen().catch(() => {});
    }
});

document.addEventListener("fullscreenchange", () => {
    updateFsIcons(!!document.fullscreenElement);
    resetFsTimer();
});

function resetFsTimer() {
    fullscreenBtn.classList.remove("hidden");
    clearTimeout(fsTimer);
    fsTimer = setTimeout(() => {
        fullscreenBtn.classList.add("hidden");
    }, 3000);
}

document.addEventListener("mousemove", resetFsTimer);
document.addEventListener("touchstart", resetFsTimer);

resetFsTimer();
