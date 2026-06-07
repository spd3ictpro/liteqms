# LiteQMS — Queue Calling System for Clinics

A simple, browser-based queue calling system built with **Klinik Kesihatan** in mind.

Many clinics struggle with expensive or complicated QMS systems — some don't have one at all. LiteQMS is here to help.

**All you need is a computer and a TV.**  
If your TV can open a browser, access it over WiFi. Otherwise, connect the TV to your computer with an HDMI/VGA cable.

---

## How It Works

1. Install LiteQMS on any Windows PC in your clinic
2. Set up your room names (e.g., "Bilik 1", "Bilik 2")
3. Open the calling page on a phone or tablet — one per room
4. Open the display page on your waiting area TV
5. When staff call a patient number, the TV updates instantly with a sound

That's it. No expensive hardware, no monthly fees, no internet required.
The app will open in your browser automatically when you run it.

---

## What You Get

- **Patient calling** — enter a 4-digit number and call. Simple.
- **TV display** — large numbers everyone can see from across the room
- **Notification sound** — patients know when their number is called
- **Mobile-friendly** — staff can call from their phones using the on-screen numpad
- **Call log** — see who was called and when
- **Recall** — call the same patient again with one tap
- **CNA** — mark a patient as "Called Not Answered" if they miss their turn
- **QR code** — patients can scan to open the display page on their phone
- **3 color themes** — pick the look you like
- **Update checker** — LiteQMS will let you know when a new version is available
- **Works offline** — no internet needed once installed
- **Native Android TV app** — dedicated native companion app with SignalR, UDP auto-discovery, D-pad navigation, and auto-sizing UI

---

## LiteQMS-TV (Android Companion App)

A companion Android project for the **native TV display** experience. Two variants:

| Variant | Description |
|---------|-------------|
| **Native app** | Kotlin + SignalR Java client, programmatic UI, auto-sizing text, full D-pad/remote support, UDP auto-discovery |
| WebView app | Lightweight WebView wrapper (planned) |

### Features
- **Real-time SignalR** — connects directly to the LiteQMS server via SignalR
- **UDP discovery** — auto-finds the server on the LAN via UDP broadcast on port 56789
- **Auto-sizing text** — room labels and recent items scale down to fit long names
- **D-pad navigation** — designed for Android TV remotes with focus indicators
- **Settings screen** — manual URL entry + auto-discover + test connection
- **Immersive fullscreen** — hides system bars, keeps screen on

### Build
```bash
cd LiteQMS-TV
./gradlew assembleDebug
```
APK at `LiteQMS-TV/native-app/build/outputs/apk/debug/native-app-debug.apk`

---

## License

Proprietary — see [LICENSE](LICENSE).
