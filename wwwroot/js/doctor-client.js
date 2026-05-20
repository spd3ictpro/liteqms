const connection = new signalR.HubConnectionBuilder()
    .withUrl("/queueHub")
    .withAutomaticReconnect()
    .build();

const digitInputs = [
    document.getElementById("digit1"),
    document.getElementById("digit2"),
    document.getElementById("digit3"),
    document.getElementById("digit4")
];
const hiddenField = document.getElementById("patientNumberHidden");
const validIcon = document.getElementById("validIcon");
const invalidIcon = document.getElementById("invalidIcon");
const syncDot = document.getElementById("syncDot");
const syncLabel = document.getElementById("syncLabel");

function getCombinedValue() {
    return digitInputs.map(i => i.value).join("");
}

function setCombinedValue(val) {
    const digits = val.padEnd(4, "").slice(0, 4).split("");
    digitInputs.forEach((input, idx) => {
        input.value = digits[idx] || "";
    });
    updateHiddenAndValidation();
}

function updateHiddenAndValidation() {
    const combined = getCombinedValue();
    hiddenField.value = combined;

    if (combined.length === 4 && /^\d{4}$/.test(combined)) {
        digitInputs.forEach(i => { i.classList.remove("is-invalid"); i.classList.add("is-valid"); });
        validIcon.style.display = "inline";
        invalidIcon.style.display = "none";
    } else if (combined.length === 4) {
        digitInputs.forEach(i => { i.classList.remove("is-valid"); i.classList.add("is-invalid"); });
        validIcon.style.display = "none";
        invalidIcon.style.display = "inline";
    } else {
        digitInputs.forEach(i => { i.classList.remove("is-valid", "is-invalid"); });
        validIcon.style.display = "none";
        invalidIcon.style.display = "none";
    }
}

function focusNextEmpty() {
    const idx = digitInputs.findIndex(i => !i.value);
    if (idx >= 0) digitInputs[idx].focus();
    else digitInputs[3].focus();
}

function clearAllDigits() {
    digitInputs.forEach(i => { i.value = ""; });
    updateHiddenAndValidation();
    digitInputs[0].focus();
}

digitInputs.forEach((input, idx) => {
    input.addEventListener("input", (e) => {
        const val = e.target.value.replace(/\D/g, "");
        e.target.value = val;
        if (val && idx < 3) {
            digitInputs[idx + 1].focus();
        }
        updateHiddenAndValidation();
    });

    input.addEventListener("keydown", (e) => {
        if (e.key === "Backspace" && !e.target.value && idx > 0) {
            digitInputs[idx - 1].focus();
            digitInputs[idx - 1].value = "";
            updateHiddenAndValidation();
        }
    });

    input.addEventListener("paste", (e) => {
        e.preventDefault();
        const paste = (e.clipboardData || window.clipboardData).getData("text").replace(/\D/g, "").slice(0, 4);
        paste.split("").forEach((char, i) => {
            if (digitInputs[i]) digitInputs[i].value = char;
        });
        updateHiddenAndValidation();
        focusNextEmpty();
    });

    input.addEventListener("focus", (e) => {
        e.target.select();
    });
});

document.getElementById("callForm").addEventListener("submit", () => {
    const btn = document.getElementById("callBtn");
    btn.disabled = true;
    btn.innerHTML = `
        <span class="spinner-border spinner-border-sm me-2" role="status"></span>
        Calling...
    `;
});

document.getElementById("clearBtn").addEventListener("click", () => {
    clearAllDigits();
});

document.addEventListener("click", (e) => {
    const recallBtn = e.target.closest(".recall-btn");
    if (recallBtn) {
        const patient = recallBtn.dataset.patient;
        setCombinedValue(patient);
        digitInputs[3].focus();
    }
});

function updatePreview(state) {
    document.getElementById("previewRoom").textContent = state.roomNumber;
    const previewNumber = document.getElementById("previewNumber");
    previewNumber.textContent = state.patientNumber;

    previewNumber.classList.remove("pulse");
    void previewNumber.offsetWidth;
    previewNumber.classList.add("pulse");
}

function renderRecentCalls(recentCalls) {
    const container = document.getElementById("previewRecentCalls");
    if (!recentCalls || recentCalls.length === 0) {
        container.innerHTML = '<div class="recent-empty">No recent calls</div>';
        return;
    }

    container.innerHTML = recentCalls.map(call => {
        const time = new Date(call.timestamp).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit" });
        const cnaClass = call.isCNA ? "cna" : "";
        const cnaBadge = call.isCNA ? '<span class="badge bg-danger ms-1" style="font-size: 0.65rem;">CNA</span>' : "";
        const btnClass = call.isCNA ? "cna-btn undo" : "cna-btn mark";
        const btnText = call.isCNA ? "Undo" : "CNA";

        return `<div class="recent-item ${cnaClass}" id="recent-${call.id}">
            <div class="recent-item-info">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color: ${call.isCNA ? '#6c757d' : '#198754'}">
                    <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>
                </svg>
                <span class="recent-item-number">${call.patientNumber}</span>
                <span class="recent-item-room">${call.roomNumber}</span>
                <span class="recent-item-time">${time}</span>
                ${cnaBadge}
            </div>
            <div class="d-flex gap-1">
                <button type="button" class="recall-btn" data-patient="${call.patientNumber}">Recall</button>
                <form method="post" asp-page-handler="CNA" class="d-inline" action="/Doctor?handler=CNA">
                    <input type="hidden" name="id" value="${call.id}" />
                    <button type="submit" class="${btnClass}">${btnText}</button>
                </form>
            </div>
        </div>`;
    }).join("");
}

connection.on("NewCall", (state) => {
    updatePreview(state);
    renderRecentCalls(state.recentCalls);
    clearAllDigits();
});

connection.on("ReceiveCurrentState", (state) => {
    updatePreview(state);
    renderRecentCalls(state.recentCalls);
});

connection.on("CNAUpdated", (callRecordId, isCNA) => {
    const item = document.getElementById(`recent-${callRecordId}`);
    if (item) {
        if (isCNA) {
            item.classList.add("cna");
            const badge = item.querySelector(".badge");
            if (!badge) {
                const info = item.querySelector(".recent-item-info");
                const timeEl = item.querySelector(".recent-item-time");
                const newBadge = document.createElement("span");
                newBadge.className = "badge bg-danger ms-1";
                newBadge.style.fontSize = "0.65rem";
                newBadge.textContent = "CNA";
                timeEl.after(newBadge);
            }
            const btn = item.querySelector("button");
            if (btn) {
                btn.className = "cna-btn undo";
                btn.textContent = "Undo";
            }
        } else {
            item.classList.remove("cna");
            const badge = item.querySelector(".badge");
            if (badge) badge.remove();
            const btn = item.querySelector("button");
            if (btn) {
                btn.className = "cna-btn mark";
                btn.textContent = "CNA";
            }
        }
    }
});

function setSyncStatus(connected) {
    if (connected) {
        syncDot.className = "sync-dot connected";
        syncLabel.textContent = "Connected";
    } else {
        syncDot.className = "sync-dot disconnected";
        syncLabel.textContent = "Disconnected";
    }
}

connection.onclose(() => {
    setSyncStatus(false);
});

connection.onreconnecting(() => {
    setSyncStatus(false);
});

connection.onreconnected(() => {
    setSyncStatus(true);
});

async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR connected");
        setSyncStatus(true);
        await connection.invoke("RequestCurrentState");
    } catch (err) {
        console.error("SignalR connection error:", err);
        setSyncStatus(false);
        setTimeout(() => startConnection(), 5000);
    }
}

startConnection();
