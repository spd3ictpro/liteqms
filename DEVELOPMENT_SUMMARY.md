# LiteQMS — Development Summary
# Date: 19 May 2026
# Project Path: C:\Users\Sam\.liteqms\LiteQMS

---

## PROJECT OVERVIEW

LiteQMS is a Clinic Queue Calling System built with .NET 9 ASP.NET Core Razor Pages + SignalR + SQLite.
It runs locally on one PC, browser-based for all users. No authentication.

### Pages
- `/` (Index) — Doctor setup: enter room number (e.g., "Bilik 1")
- `/Doctor` — Calling interface: input patient number, call, CNA buttons, live preview
- `/Display` — TV display: big patient number, room, recently called list, clock, ding-dong sound
- `/History` — Call log table with date range filter

### Tech Stack
- **Framework:** .NET 9 ASP.NET Core Razor Pages
- **Real-time:** SignalR (QueueHub at `/queueHub`)
- **Database:** SQLite (EF Core, file: `LiteQMS.db`)
- **Styling:** Bootstrap 5 + custom CSS with Inter font (bundled locally)
- **Font:** Inter Variable (wwwroot/fonts/Inter-Variable.ttf) — no internet dependency
- **Session:** In-memory distributed cache, 8-hour idle timeout

---

## KEY BEHAVIORS

### Queue Logic
- **Global display** — shows latest call from ANY room
- **Sequential queue** — newest call takes center, older ones move to "recently called"
- **Same-number rule** — if the same patient number is called again, the display stays unchanged (saved to DB for history only). Display only updates when a DIFFERENT number is called.
- **CNA (Called Not Answered)** — marks database only, display unaffected
- **Duplicate warning** — shown on doctor page if same number called today
- **Format validation** — warns if patient number doesn't match 1xxx, 3xxx, 5xxx, 7xxx
- **Midnight reset** — background service clears active display state at 00:00, keeps DB records
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
│            Tue, 19 May    │ │       1001           │ │
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

### Display Sizing (current)
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
├── Program.cs                          # App startup, DI, middleware
├── appsettings.json                    # Connection string, logging
├── LiteQMS.csproj                      # Project file, NuGet packages
├── Data/
│   ├── AppDbContext.cs                 # EF Core DbContext
│   └── CallRecord.cs                   # Entity: Id, RoomNumber, PatientNumber, Timestamp, IsCNA
├── Hubs/
│   └── QueueHub.cs                     # SignalR hub: NewCall, CNAUpdated, QueueReset
├── Services/
│   ├── QueueStateService.cs            # Singleton state management
│   └── MidnightResetService.cs         # Background service for midnight reset
├── Pages/
│   ├── Index.cshtml / .cs              # Doctor setup page
│   ├── Doctor.cshtml / .cs             # Calling interface
│   ├── Display.cshtml / .cs            # TV display
│   ├── History.cshtml / .cs            # Call history log
│   ├── Error.cshtml / .cs              # Error page
│   ├── _ViewImports.cshtml             # Tag helpers, namespace imports
│   └── _ViewStart.cshtml               # Default layout
└── wwwroot/
    ├── css/
    │   ├── site.css                    # Global styles (teal theme, animations, scrollbar)
    │   └── display.css                 # Display-specific styles (light teal, Inter font)
    ├── js/
    │   ├── display-client.js           # Display SignalR client, clock, audio, animations
    │   └── doctor-client.js            # Doctor SignalR client, preview, validation
    ├── fonts/
    │   └── Inter-Variable.ttf          # Inter variable font (bundled locally)
    └── sounds/
        └── ding-dong.wav               # Ding-dong sound (swappable)
```

---

## IMPORTANT IMPLEMENTATION DETAILS

### Doctor OnPost Logic (Doctor.cshtml.cs lines 92-112)
```
1. Save call record to DB always
2. Check if PatientNumber == CurrentState.PatientNumber
3. If SAME → log only, DO NOT broadcast display update
4. If DIFFERENT → build recent list (exclude current number), broadcast via SignalR
```

### SignalR Events (QueueHub)
- `NewCall` — broadcasts CallState (RoomNumber, PatientNumber, Timestamp, RecentCalls[])
- `CNAUpdated` — broadcasts (callRecordId, isCNA)
- `QueueReset` — no payload, clears display

### Session Usage
- `HttpContext.Session.SetString("RoomNumber", value)` — set on Index page
- `HttpContext.Session.GetString("RoomNumber")` — read on Doctor page
- Requires: AddDistributedMemoryCache() + AddSession() + UseSession()

### Database
- SQLite, auto-created on startup via `db.Database.EnsureCreated()`
- Table: `CallRecords` with index on `Timestamp`
- Records kept forever (midnight reset only clears display state, not DB)

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

- **Font:** Inter Variable (wwwroot/fonts/Inter-Variable.ttf)
- **Weights:** 100-900 (variable)
- **Usage:** `font-family: 'Inter', 'Segoe UI', system-ui, sans-serif;`
- **No internet dependency** — served from wwwroot/fonts/

---

## HOW TO RUN

```bash
cd C:\Users\Sam\.liteqms\LiteQMS
dotnet run --urls "http://localhost:5000"
```

Then open:
- Doctor setup: http://localhost:5000/
- Doctor calling: http://localhost:5000/Doctor (after entering room)
- TV display: http://localhost:5000/Display
- History: http://localhost:5000/History

---

## COMPLETED FEATURES

- [x] Project scaffolded with .NET 9 Razor Pages
- [x] Session middleware configured (AddDistributedMemoryCache + AddSession + UseSession)
- [x] SignalR real-time communication (QueueHub)
- [x] SQLite persistence with EF Core (auto-create on startup)
- [x] All 4 pages built (Index, Doctor, Display, History)
- [x] Ding-dong sound with click-to-enable overlay
- [x] Patient number format validation (1xxx, 3xxx, 5xxx, 7xxx)
- [x] Duplicate call warning
- [x] CNA toggle (marks DB only, display unaffected)
- [x] Midnight reset background service (clears display state, keeps DB records)
- [x] Teal/green color theme across all pages
- [x] Inter font bundled locally (no internet dependency)
- [x] Display page: light teal background, split layout (70/30), bigger sizing
- [x] Same-number-doesn't-move-to-recent logic
- [x] Display scaling for any TV resolution (viewport + rem units)
- [x] Animations: pulse on new call, slide-in for recent calls, "Just Called" badge
- [x] Doctor page: live preview card with pulse, validation icons, loading spinner
- [x] History page: date filter, clear button, better empty state, hover highlights
- [x] Index page: gradient background, SVG icon, slide-up animation

---

## POTENTIAL FUTURE WORK

- [ ] Pagination on History page (if records grow large)
- [ ] Sound settings page (volume, custom sound upload)
- [ ] Multi-display support (different displays for different rooms)
- [ ] Queue management features (skip, reorder)
- [ ] Export history to CSV
- [ ] Production deployment config (HTTPS, reverse proxy)
- [ ] Unit/integration tests
- [ ] Error handling improvements (DB connection failures, etc.)
- [ ] Display page: fullscreen API, screensaver prevention
