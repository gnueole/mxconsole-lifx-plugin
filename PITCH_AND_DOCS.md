# Pitch Brief & Product Documentation: LIFX for Logitech MX Creative Console

This document outlines the product pitch, presentation brief, and technical setup guide for the **LIFX Smart Lights Plugin** for the Logitech MX Creative Console (powered by Loupedeck).

> [!NOTE]
> **Initial Release Scope:** For this initial release, the plugin exclusively handles light groups (rooms), hue, brightness, on/off, and warmness adjustments.

---

## Part 1: Product Presentation Pitch Brief

### 💡 The Vision
> "Create the ultimate workspace flow by aligning physical atmosphere with digital focus."

For creators, developers, and designers, lighting is not just utility—it is part of the workspace. Transitioning from editing video under cool, bright light to writing code in warm, dim lighting, or testing colors under daylight, requires absolute control. The **LIFX Plugin for Logitech MX Creative Console** brings seamless, tactile lighting orchestration directly to the creator's fingertips without breaking their creative flow.

---

### ⚙️ Core Technical Features

1. **Direct Tactile Control Mapping**
   * Translates dial rotation events, key presses, and custom screen drawing directly to corresponding API operations.
   * Maps physical dials to granular increments for brightness, kelvin temperature, and hue angles.

2. **Stateful "Active Target" Routing**
   * Employs an active room/group pointer within the plugin lifecycle to route general adjustments (e.g. brightness dial, color reset) dynamically.
   * Saves hardware layout real estate by allowing a single set of dial controls to apply to any dynamically selected room.
   * Selected target status is reflected visually on screen keys via dynamic color fill (`#8200FF` background).

3. **Dynamic Canvas Rendering Engine**
   * Draws vector shapes (hue wheel arcs, symmetrical brightness sweeps, warmth gradients) directly onto a drawing context using the SDK's rendering primitives.
   * Renders real-time visual state adjustments without relying on static, pre-rendered asset files.

4. **Non-Blocking Asynchronous Execution**
   * Offloads all HTTP communication with the LIFX Cloud API to background tasks.
   * Isolates physical input events from network roundtrip latency, preventing input lock-ups or console UI thread hangs.

---

## Part 2: Slide-by-Slide Pitch Presentation Deck

### Slide 1: Title & Hook
* **Title:** Tactile Light: LIFX Smart Control for Logitech MX Creative Console
* **Subtitle:** Seamless atmospheric control for modern workspaces.
* **Visual:** Photo of the MX Creative Console glowing with the custom purple active room selection ring, alongside LIFX light strips.

### Slide 2: The Friction
* **Header:** The Flow Disruption
* **Points:**
  * Adjusting office lights during a work session requires picking up a phone, opening a slow smart-home app, and hunting for room groups.
  * Adjusting individual bulb properties (color, temperature) breaks focus.
  * Creative professionals need instant, muscle-memory control.

### Slide 3: The Solution
* **Header:** Tactile Environment Control
* **Points:**
  * A native C# integration leveraging the Loupedeck/Logitech SDK.
  * Direct access to LIFX Cloud API.
  * Fully custom-designed dynamic dial adjustments and commands.

### Slide 4: Hero Feature: Active Room Selector
* **Header:** Less Clutter, More Control
* **Points:**
  * **Dynamic Selection:** Press a room key to target it. The selected key lights up in purple.
  * **Unified Control:** Dials automatically bind to the selected room.
  * **Dynamic Scaling:** Adding a new light or room doesn't require rebuilding console profiles.

### Slide 5: Native & High Performance
* **Header:** Zero Latency, Maximum Polish
* **Points:**
  * Fully asynchronous execution ensures the console interface remains responsive.
  * High-fidelity vector graphics rendered dynamically on-the-fly.
  * Symmetrical dials and black-background command buttons for sleek dark-mode layouts.

### Slide 6: Market Launch & Ease of Use
* **Header:** Low Friction, High Adoption
* **Points:**
  * **Simple Setup:** No command-line tools or hidden developer files. Users simply paste their API token into their standard Windows `Documents/LIFX_Token.txt` file.
  * **Marketplace Ready:** Packaged in the official `.lplug4` plugin format for Loupedeck / Logitech G Marketplace.

---

## Part 3: Technical Documentation & User Setup Guide

### 📂 How it Works
The plugin runs inside the Logitech Options+ service. It loads your personal access token, discovers your LIFX rooms dynamically, and publishes actions to Options+ so they can be assigned via the action picker interface.

### 🔑 Simple Setup (No WSL Required)
We have made configuration trivial for Windows and macOS users:

1. **Generate your Token**:
   * Log in to **[LIFX Cloud Settings](https://cloud.lifx.com/settings)**.
   * Generate a **Personal Access Token** and copy it.

2. **Save the Token**:
   * Open **Notepad** (Windows) or **TextEdit** (Mac).
   * Paste your token.
   * Save the file as **`LIFX_Token.txt`** directly inside your standard **Documents** folder.
     * *Example path:* `C:\Users\YourName\Documents\LIFX_Token.txt`

3. **Reload Options+**:
   * The plugin automatically detects the file and establishes the API connection on startup.

---

## Part 4: Package Architecture & Security Verification

### 🛡️ Security Measures
* **TLS 1.3 / HTTPS Only**: All API requests use SSL/TLS (`https://api.lifx.com/v1/`) to ensure no token or light status is exposed in cleartext over the local network.
* **Token Isolation**: The token is loaded locally on startup and is never written to logs, console traces, or shared with third parties.
* **Safe Parsing**: Employs Microsoft's `System.Text.Json` library for parsing. This prevents arbitrary deserialization/type instantiation attacks.
* **Minimal Scope**: Reads only the necessary user-configured file (`Documents/LIFX_Token.txt`) without sweeping system directories.

### 📦 Staging Archive Structure (`.lplug4`)
To validate the release, the package contains:
* `metadata/LoupedeckPackage.yaml` – Defines metadata, compatibility, and entrypoints.
* `metadata/Icon256x256.png` – The logo shown in the Options+ store.
* `bin/LifxPlugin.dll` – Compiled optimized release binary.
* `bin/[Dependencies].dll` – Libraries required at runtime (e.g. `websocket-sharp.dll`).
* **Note:** The `PluginApi.dll` has been stripped from the package to ensure it inherits the clean, system-provided version in Logitech Options+.
