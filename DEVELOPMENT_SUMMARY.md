# LiteQMS — Development Summary
# Date: 22 May 2026
# Project Path: D:\Programs\liteqms

---

## PROJECT OVERVIEW

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
- **Styling:** Bootstrap 5 + custom CSS with Inter font (bundled locally)
- **Font:** Inter Variable (`wwwroot/fonts/Inter-Variable.ttf`) — no internet dependency
- **Session:** In-memory distributed cache, 8-hour idle timeout

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
- **Midnight reset** — background service clears active display state at 00:00, deletes previous day's DB records
- **Auto-reconnect** — SignalR handles display page reconnection automatically

### Audio
- Ding-dong sound on new call (`wwwroot/sounds/ding-dong.wav`)
- Browser requires user interaction (click) before audio can play
- Audio overlay shown on display page until first click
- Sound file is swappable — just replace the .wav file

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

## FILE STRUCTURE

```
LiteQMS/
├── Program.cs                          # App startup, DI, middleware, SignalR mapping
├── appsettings.json                    # Connection string, logging config
├── LiteQMS.csproj                      # Project file, NuGet packages
├── Data/
│   ├── AppDbContext.cs                 # EF Core DbContext with Timestamp index
│   └── CallRecord.cs                   # Entity: Id, RoomNumber, PatientNumber, Timestamp, IsCNA
├── Hubs/
│   └── QueueHub.cs                     # SignalR hub: CallPatient, NewCall, CNAUpdated, QueueReset
├── Services/
│   ├── QueueStateService.cs            # Singleton in-memory state with SignalR broadcast
│   └── MidnightResetService.cs         # Background service: daily reset at midnight
├── Pages/
│   ├── Index.cshtml / .cs              # Room number entry page
│   ├── CallPanel.cshtml / .cs             # Patient calling interface with live preview
│   ├── Display.cshtml / .cs            # TV/public display page
│   ├── History.cshtml / .cs            # Call history with date filtering
│   ├── Error.cshtml / .cs              # Error page
│   ├── _ViewImports.cshtml             # Tag helpers, namespace imports
│   └── _ViewStart.cshtml               # Default layout
└── wwwroot/
    ├── css/
    │   ├── site.css                    # Global styles (3 themes: teal/blue/dark)
    │   └── display.css                 # Display-specific styles (large typography, animations)
    ├── js/
    │   ├── display-client.js           # Display SignalR client, clock, audio, wake lock, fullscreen
    │   ├── call-panel-client.js            # Call Panel SignalR client, digit input, preview, recall/CNA
    │   ├── site.js                     # (unused / placeholder)
    │   └── theme.js                    # Theme switcher (teal/blue/dark) with localStorage
    ├── fonts/
    │   └── Inter-Variable.ttf          # Inter variable font (bundled locally)
    └── sounds/
        └── ding-dong.wav               # Ding-dong sound (swappable)
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
- Midnight reset deletes records from previous days (keeps only today's data)

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

```bash
cd D:\Programs\liteqms
dotnet run --urls "http://localhost:5000"
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
- [x] Midnight reset background service (clears display, deletes yesterday's records)
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

---

## POTENTIAL FUTURE WORK

- [ ] Sound settings page (volume, custom sound upload)
- [ ] Multi-display support (different displays for different rooms)
- [ ] Queue management features (skip, reorder)
- [ ] Export history to CSV
- [ ] Production deployment config (HTTPS, reverse proxy)
- [ ] Unit/integration tests
- [ ] Error handling improvements (DB connection failures, etc.)
- [ ] Pagination on History page for large datasets
