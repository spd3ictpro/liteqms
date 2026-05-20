const connection = new signalR.HubConnectionBuilder()
    .withUrl("/queueHub")
    .withAutomaticReconnect()
    .build();

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
            <form method="post" asp-page-handler="CNA" class="d-inline" action="/Doctor?handler=CNA">
                <input type="hidden" name="id" value="${call.id}" />
                <button type="submit" class="${btnClass}">${btnText}</button>
            </form>
        </div>`;
    }).join("");
}

connection.on("NewCall", (state) => {
    updatePreview(state);
    renderRecentCalls(state.recentCalls);
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

async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR connected");
        await connection.invoke("RequestCurrentState");
    } catch (err) {
        console.error("SignalR connection error:", err);
        setTimeout(() => startConnection(), 5000);
    }
}

startConnection();

document.getElementById("callForm").addEventListener("submit", () => {
    const btn = document.getElementById("callBtn");
    btn.disabled = true;
    btn.innerHTML = `
        <span class="spinner-border spinner-border-sm me-2" role="status"></span>
        Calling...
    `;
});

document.getElementById("clearBtn").addEventListener("click", () => {
    const input = document.getElementById("patientInput");
    input.value = "";
    input.classList.remove("is-valid", "is-invalid");
    input.focus();
    document.getElementById("validIcon").classList.remove("valid");
    document.getElementById("invalidIcon").classList.remove("invalid");
});

const patientInput = document.getElementById("patientInput");
const validIcon = document.getElementById("validIcon");
const invalidIcon = document.getElementById("invalidIcon");

patientInput.addEventListener("input", function () {
    const val = this.value.trim();
    const validPrefixes = ["1", "3", "5", "7"];

    if (val.length === 4 && validPrefixes.includes(val[0]) && /^\d{4}$/.test(val)) {
        this.classList.remove("is-invalid");
        this.classList.add("is-valid");
        validIcon.classList.add("valid");
        invalidIcon.classList.remove("invalid");
    } else if (val.length === 4 && !validPrefixes.includes(val[0])) {
        this.classList.add("is-invalid");
        this.classList.remove("is-valid");
        validIcon.classList.remove("valid");
        invalidIcon.classList.add("invalid");
    } else if (val.length > 0) {
        this.classList.remove("is-valid", "is-invalid");
        validIcon.classList.remove("valid");
        invalidIcon.classList.remove("invalid");
    } else {
        this.classList.remove("is-valid", "is-invalid");
        validIcon.classList.remove("valid");
        invalidIcon.classList.remove("invalid");
    }
});
