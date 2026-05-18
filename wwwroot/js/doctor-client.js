const connection = new signalR.HubConnectionBuilder()
    .withUrl("/queueHub")
    .withAutomaticReconnect()
    .build();

connection.on("NewCall", (state) => {
    document.getElementById("previewRoom").textContent = state.roomNumber;
    const previewNumber = document.getElementById("previewNumber");
    previewNumber.textContent = state.patientNumber;

    previewNumber.classList.remove("pulse");
    void previewNumber.offsetWidth;
    previewNumber.classList.add("pulse");
});

connection.on("CNAUpdated", (callRecordId, isCNA) => {
    location.reload();
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

document.getElementById("callForm").addEventListener("submit", () => {
    const btn = document.getElementById("callBtn");
    btn.disabled = true;
    btn.innerHTML = `
        <span class="spinner-border spinner-border-sm me-2" role="status"></span>
        Calling...
    `;
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
