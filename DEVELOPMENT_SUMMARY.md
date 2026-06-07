# LiteQMS — Development Summary
# Date: 7 June 2026
# Project Path: C:\Users\User\liteqms

### Stage 6 — LiteQMS-TV Native Android App (4 June 2026)
- [x] UDP discovery service (`UdpDiscoveryService.cs`) — listens for `LITEQMS_DISCOVER` on port 56789
- [x] Native Android app project scaffolded (Kotlin, minSdk 23, targetSdk 30)
- [x] SignalR client integration with `ReceiveCurrentState` / `NewCall` / `QueueReset` / `CNAUpdated` events
- [x] Full-screen display UI: patient number (180sp), room label, recent calls sidebar, clock
- [x] Ding-dong audio playback via `AudioPlayer`
- [x] UDP discovery on Android: broadcasts to `255.255.255.255:56789`, parses JSON response
- [x] Settings screen: manual URL entry, auto-discover, test connection
- [x] Server URL persistence via SharedPreferences
- [x] Reconnection logic with exponential backoff (1s, 5s, 30s)
- [x] Connection state indicator (green/yellow/red dot + reconnection banner)

### Stage 6b — D-pad Navigation & Remote Support (4 June 2026)
- [x] `onKeyDown` handler — any key press shows settings gear button
- [x] Settings gear auto-hides after 3 seconds of inactivity
- [x] Focus highlight on Settings button (`GradientDrawable` teal background)
- [x] Migrated from deprecated `startActivityForResult` to `registerForActivityResult`

### Stage 6c — Reconnect Bugfix v1.0.0.1 / v1.0.2 (7 June 2026)
- [x] **Bug:** `ReceiveCurrentState` and `NewCall` both used the same `onNewCall` callback
- [x] **Effect:** Every reconnect triggered ding-dong sound + "Just Called" badge + number pulse for old state
- [x] **Fix:** Added separate `onStateSync` callback for `ReceiveCurrentState` — silenty updates UI without audio/animation/badge
- [x] **Files:** `SignalRService.kt`, `DisplayViewModel.kt`, `DisplayActivity.kt`

### Stage 6d — Auto-sizing Text & UI Polish v1.0.2 (7 June 2026)
- [x] Room label below patient number: `60sp` → `115sp` with autosize (24–115sp)
- [x] Recent item patient number: `48sp` → `60sp` with autosize (24–60sp)
- [x] Recent item room label: `24sp` → `32sp` with autosize (16–32sp)
- [x] Uses `TextViewCompat.setAutoSizeTextTypeUniformWithConfiguration` — scales down gracefully for long room names

---


LiteQMS is a Clinic Queue Calling System built with .NET 9 ASP.NET Core Razor Pages + SignalR + SQLite.
It runs locally on one PC, browser-based for all users. No authentication.

### Pages
- `/` (Index) — Room setup: enter room number (e.g., "Bilik 1"), auto-uppercased, stored in session
- `/CallPanel` — Calling interface: 4-digit patient number input, call, recall, CNA toggle, live preview
- `/Display` — TV display: large patient number, room label, recently called list, clock, ding-dong sound
- `/History` — Call log table with date range filter, up to 500 records

### Tech Stack
- **Framework:** .NET 9 ASP.NET Core Razor Pages
- **Real-time:** SignalR (QueueHub at `/queueHub`)
- **Database:** SQLite (EF Core, file: `LiteQMS.db`)
- **Styling:** Bootstrap 5 (bundled locally at `wwwroot/lib/bootstrap/`)
- **Font:** Inter Variable (`wwwroot/fonts/Inter-Variable.ttf`) — no internet dependency
- **SignalR JS:** Bundled locally at `wwwroot/lib/signalr/` — no internet dependency
- **Session:** In-me  ry distributed cache, 8-hour idle timeout
- **Network:** Binds to `0.0.0.0:5000` — accessible via hostname from any LAN device

---

## KEY BEHAVIORS

### Queue Logic
- **Global display** — shows latest call from ANY room, not per-room
- **Sequential queue** — newest call takes center, older ones move to "recently called" sidebar
- **Same-number rule** — if the same patient number is called again, display stays unchanged (record saved to DB for history only). Display only updates when a DIFFERENT number is called or the same number is recalled.
- **CNA (Called Not Answered)** — toggles on DB record only; display unaffected
- **Duplicate/recall detection** — shows call count on doctor page if same number called today
- **Auto-uppercase** — room number on Index page is uppercased client-side (JS) + server-side (`ToUpper()`)
- **Autocomplete off** — room number input has `autocomplete="off"` to prevent browser suggestions
- **Format validation** — patient number must be exactly 4 digits
- **Midnight reset** — background service clears active display state at 00:00, deletes records older than 365 days
- **Auto-reconnect** — SignalR handles display page reconnection automatically

### Audio
- Ding-dong sound on new call (`wwwroot/sounds/ding-dong.mp3`)
- Browser requires user interaction (click) before audio can play
- Audio overlay shown on display page until first click

---

## DISPLAY PAGE — CURRENT LAYOUT

```
┌──────────────────────────┬──────────────────────────┐
│ LiteQMS    23:45:30       │ ┌──────────────────────┐ │
│            Tue, 22 May    │ │       1001           │ │
├──────────────────────────┤ │ 📞 Bilik 1            │ │
│                          │ ├──────────────────────┤ │
│        1234              │ │       1002           │ │
│                          │ │ 📞 Bilik 2            │ │
│      BILIK 3             │ ├──────────────────────┤ │
│                          │ │       1003           │ │
│   [Just Called badge]    │ │ 📞 Bilik 3            │ │
│                          │ ├──────────────────────┤ │
│                          │ │       1004           │ │
│                          │ │ 📞 Bilik 1            │ │
│                          │ └──────────────────────┘ │
└──────────────────────────┴──────────────────────────┘
```

### Display Sizing
- Main patient number: `20rem` (weight 900)
- Room label (below number): `12.8rem` (weight 700)
- Recent boxes: 4 boxes, full height
  - Patient number: `5.4rem` (weight 900)
  - Room number: `2.4rem` (weight 600) with phone icon
- Clock: `2.5rem` with seconds + full date
- Layout: 70% left panel, 30% right panel

### Display Colors (light teal theme)
- Background: `#ccfbf1` → `#99f6e4` radial gradient
- Patient number: `#0f172a` (dark slate)
- Room label: `#0d9488` (teal)
- Recent boxes: white → `#f0fdfa` gradient with teal top accent bar
- First recent item: stronger teal glow/shadow

### Display Scaling
- Viewport set to `width=1920, height=1080`
- All sizes in `rem` units — scales proportionally to any TV resolution
- On 4K TV, browser auto-scales 2x; on smaller TVs, scales down

---

## MOBILE UI — CALL PANEL

### Layout Optimizations (max-width: 768px)
- Navbar: compacted padding, smaller brand/room-badge font; sync indicator hidden
- Card padding: reduced from 3rem to 1rem; form label spacing tightened
- Container padding: halved to save vertical space
- Hint text: hidden (occupies space without adding value on phones)
- Preview card: compacted header/body padding, number font reduced to 2.5rem

### On-Screen Numpad
- 3x4 grid layout positioned between digit inputs and action buttons:
  ```
  1  2  3
  4  5  6
  7  8  9
  ⌫  0  Clear
  ```
- Digit inputs set to `readOnly` on mobile — phone keyboard never appears
- Numpad tapping fills the first empty digit box and auto-advances focus
- Backspace clears the last filled digit; Clear resets all four boxes
- Triggers the same `input` events as keyboard input — validation and auto-advance work identically
- Desktop CSS: `display: none` — numpad invisible on wide screens

---

## FILE STRUCTURE

```
LiteQMS/
├── Program.cs                          # App startup, DI, middleware, SignalR mapping, tray icon
├── appsettings.json                    # Connection string, logging config
├── LiteQMS.csproj                      # Project file, NuGet packages (UseWindowsForms)
├── publish.bat                         # One-liner: dotnet publish self-contained
├── setup.iss                           # Inno Setup 7 installer script
├── Data/
│   ├── AppDbContext.cs                 # EF Core DbContext with indexes on Timestamp + (RoomNumber, Timestamp)
│   └── CallRecord.cs                   # Entity: Id, RoomNumber, PatientNumber, Timestamp, IsCNA
├── Hubs/
│   ├── QueueHub.cs                     # SignalR hub: CallPatient, ToggleCNA, NewCall, CNAUpdated, QueueReset
├── Services/
│   ├── QueueStateService.cs            # Singleton in-memory state with 8s display timer + FIFO queue
│   └── MidnightResetService.cs         # Background service: daily reset at midnight
├── Pages/
│   ├── Index.cshtml / .cs              # Room number entry page (with input validation)
│   ├── CallPanel.cshtml / .cs          # Patient calling interface with live preview + SignalR CNA
│   ├── Display.cshtml / .cs            # TV/public display page
│   ├── History.cshtml / .cs            # Call history with date range filter (max 90 days)
│   ├── Updates.cshtml / .cs            # Update checker with GitHub API (1h cache)
│   ├── Error.cshtml / .cs              # User-friendly error page with branding
│   ├── _ViewImports.cshtml             # Tag helpers, namespace imports
│   └── _ViewStart.cshtml               # Default layout
├── LiteQMS-TV/
│   ├── build.gradle.kts                 # Root Gradle config (AGP 7.4.2, Kotlin 1.9.0)
│   ├── settings.gradle.kts              # Project settings, includes :native-app
│   ├── gradlew / gradlew.bat            # Gradle wrapper
│   └── native-app/
│       ├── build.gradle.kts             # Android app config (minSdk 23, targetSdk 30)
│       └── src/main/
│           ├── AndroidManifest.xml
│           ├── res/
│           │   ├── raw/ding_dong.mp3
│           │   ├── values/themes.xml
│           │   └── mipmap-*/ic_launcher.png
│           └── java/com/liteqms/tv/
│               ├── signalr/
│               │   ├── Models.kt            # CallState, RecentCall data classes
│               │   └── SignalRService.kt    # HubConnection, events, reconnect logic
│               ├── display/
│               │   ├── DisplayActivity.kt   # Main TV UI (programmatic views, clock, animation)
│               │   └── DisplayViewModel.kt  # StateFlow-based UI state management
│               ├── audio/
│               │   └── AudioPlayer.kt       # MediaPlayer with audio focus handling
│               ├── discovery/
│               │   ├── DiscoveryService.kt  # UDP broadcast scan for LiteQMS servers
│               │   └── DiscoveryActivity.kt # Server list UI
│               └── settings/
│                   └── SettingsActivity.kt  # URL entry, test connection, save
└── wwwroot/
    ├── css/
    │   ├── site.css                    # Global styles (3 themes: teal/blue/dark)
    │   └── display.css                 # Display-specific styles (large typography, animations)
    ├── js/
    │   ├── display-client.js           # Display SignalR client, clock, audio, wake lock, fullscreen
    │   ├── call-panel-client.js        # Call Panel SignalR client, digit input, preview, recall/CNA
    │   ├── site.js                     # (unused / placeholder)
    │   └── theme.js                    # Theme switcher (teal/blue/dark) with localStorage
    ├── lib/
    │   ├── bootstrap/dist/             # Bootstrap 5.3 (CSS + JS, bundled locally)
    │   └── signalr/dist/browser/       # SignalR JS v8 (bundled locally)
    ├── fonts/
    │   └── Inter-Variable.ttf          # Inter variable font (bundled locally)
    └── sounds/
        └── ding-dong.mp3               # Ding-dong notification sound
```

---

## IMPORTANT IMPLEMENTATION DETAILS

### CallPanel OnPost Logic (CallPanel.cshtml.cs lines 56-126)
```
1. Validate room session exists
2. Validate 4-digit numeric patient number
3. Save call record to DB
4. Check if PatientNumber == CurrentState.PatientNumber
5. If SAME and not a recall → log only, DO NOT broadcast display update
6. If DIFFERENT or recall → build recent list (excluding current number), broadcast via SignalR
```

### SignalR Events (QueueHub)
- `NewCall` — broadcasts `CallState` (RoomNumber, PatientNumber, Timestamp, RecentCalls[], CallCount, IsRecall)
- `CNAUpdated` — broadcasts (callRecordId, isCNA)
- `QueueReset` — no payload, clears display to empty state
- `ReceiveCurrentState` — sent to caller on reconnection

### Session Usage
- `HttpContext.Session.SetString("RoomNumber", value)` — set on Index page
- `HttpContext.Session.GetString("RoomNumber")` — read on Call Panel page
- Requires: `AddDistributedMemoryCache()` + `AddSession()` + `UseSession()`

### Database
- SQLite, auto-created on startup via `db.Database.EnsureCreated()`
- Table: `CallRecords` with index on `Timestamp`
- Midnight reset deletes records older than 365 days (supports audit trail)

---

## COLOR PALETTE REFERENCE

```css
--bg-primary: #ccfbf1          /* Light teal background */
--bg-secondary: #99f6e4        /* Darker teal gradient end */
--white: #ffffff
--teal-primary: #0d9488        /* Main teal */
--teal-light: #14b8a6          /* Light teal accent */
--teal-glow: rgba(13, 148, 136, 0.12)
--teal-glow-strong: rgba(13, 148, 136, 0.2)
--text-primary: #0f172a        /* Dark slate for main text */
--text-secondary: #475569      /* Medium gray */
--text-muted: #94a3b8          /* Light gray */
```

---

## FONT REFERENCE

- **Font:** Inter Variable (`wwwroot/fonts/Inter-Variable.ttf`)
- **Weights:** 100-900 (variable)
- **Usage:** `font-family: 'Inter', 'Segoe UI', system-ui, sans-serif;`
- **No internet dependency** — served from `wwwroot/fonts/`

---

## HOW TO RUN

### Development
```bash
cd D:\Programs\liteqms
dotnet run --urls "http://localhost:5000"
```

### Production (self-contained .exe)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -o .\dist
.\dist\LiteQMS.exe --urls "http://localhost:5000"
```

Then open:
- Room setup: http://localhost:5000/
- Calling interface: http://localhost:5000/CallPanel (after entering room)
- TV display: http://localhost:5000/Display
- History: http://localhost:5000/History

---

## COMPLETED FEATURES

- [x] Project scaffolded with .NET 9 Razor Pages
- [x] Session middleware configured (AddDistributedMemoryCache + AddSession + UseSession)
- [x] SignalR real-time communication (QueueHub with 3 event types)
- [x] SQLite persistence with EF Core (auto-create on startup)
- [x] All 4 pages built (Index, Call Panel, Display, History)
- [x] Ding-dong sound with click-to-enable audio overlay
- [x] Patient number format validation (exactly 4 digits)
- [x] Recall detection and duplicate call count display
- [x] CNA toggle (marks DB only, display unaffected)
- [x] Midnight reset background service (clears display, deletes records older than 365 days)
- [x] 3 color themes (teal, medical blue, dark) with localStorage persistence
- [x] Inter font bundled locally (no internet dependency)
- [x] Display page: light teal background, split layout (70/30), large typography
- [x] Same-number-doesn't-move-to-recent logic
- [x] Display scaling for any TV resolution (viewport + rem units)
- [x] Animations: pulse on new call, slide-in for recent calls, "Just Called" badge
- [x] Call Panel page: live preview card with pulse, validation icons, loading spinner
- [x] History page: date filter, clear button, empty state, hover highlights
- [x] Index page: gradient background, building SVG logo, slide-up animation
- [x] Room number auto-uppercase (client-side JS + server-side `ToUpper()`)
- [x] Autocomplete disabled on room number input
- [x] Display fullscreen toggle button with auto-hide
- [x] Wake Lock API integration to prevent screen sleep on display page
- [x] SignalR automatic reconnect with connection status indicator
- [x] Recall button on both Call Panel preview and recent call items
- [x] Session expiry redirect (OnGet returns IActionResult instead of void)
- [x] Post-Redirect-Get pattern (no F5 duplicate submissions)
- [x] Display requests current state on SignalR reconnect
- [x] 8-second minimum display time with FIFO queue for rapid calls
- [x] Same-number recall ignored during active display timer (prevents abuse)
- [x] Thread-safe queue management with lock()

### Stage 4 — Hardening (23 May 2026)
- [x] Error resilience: try-catch on all DB/SignalR operations + user-friendly error page
- [x] Room number input validation (character whitelist + 50 char max)
- [x] Date range clamping on History page (max 90 days)
- [x] CNA toggle via SignalR hub method (replaced form POST, fixed anti-forgery issue)
- [x] Database performance: AsNoTracking() on all read-only queries
- [x] Composite index (RoomNumber, Timestamp) for faster queries
- [x] SQLite CommandTimeout(30) to prevent hung queries
- [x] Thread safety: lock() on all _currentState access in QueueStateService

### Stage 5 — Production Readiness (23 May 2026)
- [x] Bootstrap 5 bundled locally (removed CDN dependency)
- [x] SignalR JS v8 bundled locally (removed CDN dependency)
- [x] Zero internet dependency for all pages

### Stage 5b — Auto-open browser on startup (24 May 2026)
- [x] `app.Run()` replaced with `StartAsync()` + `WaitForShutdownAsync()`
- [x] Browser auto-opens to server URL in production (try-catch wrapped)
- [x] Production DB path resolved to `%APPDATA%\LiteQMS\` (safe from Program Files)

### Stage 5f — publish.bat (24 May 2026)
- [x] One-liner: `dotnet publish -c Release -r win-x64 --self-contained true -o .\dist`

### Stage 5e — Inno Setup installer (24 May 2026)
- [x] `setup.iss` created for Inno Setup 7
- [x] Installs to `{autopf}\LiteQMS`
- [x] DB stored in `%APPDATA%\LiteQMS\LiteQMS.db` (preserved on reinstall/uninstall)
- [x] Desktop + Start Menu shortcuts
- [x] "Launch after install" option
- [x] Uninstall: removes app files, leaves DB

### Stage 5c — System tray icon (24 May 2026)
- [x] Program.cs restructured to `[STAThread] Main` for WinForms
- [x] NotifyIcon with "Open LiteQMS" / "Quit" right-click menu
- [x] Double-click opens browser
- [x] Balloon tip on startup: "LiteQMS — Server is running"
- [x] Quit → graceful server shutdown via `app.StopAsync()`
- [x] Falls back silently if tray unavailable (no GUI)
- [x] Built-in icon used (custom .ico can be added later)

### Mobile & Retention (26 May 2026)
- [x] Call records retained for 365 days (midnight cleanup threshold updated)
- [x] QR code on Index page for one-tap mobile access (QRCoder NuGet)
- [x] Notification sound changed from .wav to .mp3
- [x] Mobile-responsive CallPanel layout with media query breakpoint at 768px
- [x] On-screen numpad with backspace/clear (no phone keyboard needed)
- [x] Digit inputs set read-only on mobile to suppress keyboard
- [x] Tighter mobile spacing (padding, margins, hidden hint text)

### Stage 5d — In-app update checker (24 May 2026)
- [x] `Pages/Updates.cshtml` + `Updates.cshtml.cs` created
- [x] Fetches latest release from `api.github.com/repos/spd3ictpro/liteqms`
- [x] 1-hour cache via `IMemoryCache` (respects GitHub rate limits)
- [x] Three states: up-to-date, update available, check failed
- [x] "Download" button pointing to GitHub releases page
- [x] "Check for updates" link in shared `_Layout.cshtml` footer
- [x] Display page (standalone, no layout) — unaffected
- [x] Assembly version: `1.0.0` from `.csproj`

### Network & LAN Access (24 May 2026)
- [x] `appsettings.json` binds to `0.0.0.0:5000` — accessible from any LAN device
- [x] Hostname URL displayed on Index page banner + tray tooltip + startup log
- [x] No console window (`WinExe`) — silent background operation
- [x] Removed `UseHttpsRedirection` — eliminates confusing HTTPS warning
- [x] Custom `liteqms.ico` for tray icon, exe, installer, shortcuts
- [x] Firewall rule auto-added during install (port 5000)
- [x] Wi-Fi auto-switched from Public → Private during install
- [x] Port auto-fallback: if 5000 taken, tries 5001, 5002, etc.
- [x] All URLs dynamically reflect the actual port (tray, Index page, log)

---


Here's the workflow for iterating:
1. Edit code — make your changes in the source files (Pages/, Services/, Data/, Hubs/, etc.)
2. Test — run dotnet build to check for compile errors, then .\dist\LiteQMS.exe to test live
3. Publish — run .\publish.bat to regenerate dist\
4. Installer — run iscc setup.iss to repack dist\LiteQMS-Setup.exe
Basically: code → build → test → publish → package. Rinse and repeat.