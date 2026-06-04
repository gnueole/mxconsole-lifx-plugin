# LIFX Smart Lights Plugin for Logitech / Loupedeck

A C# plugin for Logitech MX Creative Console and Loupedeck consoles that allows you to control your LIFX smart lights via the LIFX Cloud API.

## Features

- **Global & Room Control:** Supports toggling power and adjusting brightness for all lights globally, or for individual rooms/groups (e.g. Living Room, Bedroom) discovered dynamically from your LIFX account.
- **Active Room Selector Model:** Map selector keys to make a specific room "Active". The active room is dynamically highlighted (purple background). Once selected, general buttons and dials apply directly to that room:
  - *Active Brightness:* Adjust active room brightness (press dial to reset to 100%).
  - *Active Power Toggle:* Toggle power of the active room.
  - *Reset Color:* Instantly reset the active room's color back to standard white.
- **Premium Visual Aesthetics:** Buttons are custom-rendered with a premium black background and vibrant purple (`#8200FF`) text. Selected active selectors invert to purple background and black text.
- **Asynchronous UI:** Operates on background threads to ensure your console dials and displays stay responsive and latency-free.
- **Local Settings:** Securely reads your Personal Access Token from your local Windows user profile.

---

## Getting Started

### 1. Configure your LIFX Token
To enable the plugin to talk to your lights, you need to provide your Personal Access Token:
1. Log in to **[LIFX Cloud Settings](https://cloud.lifx.com/settings)**.
2. Under **"Personal Access Tokens"**, click **"Generate New Token"**.
3. Copy the token immediately.
4. Save it into a file named `.lifx_token` inside your Windows user directory (e.g., `C:\Users\YOUR_USERNAME\.lifx_token`).
   * *Alternatively*, in WSL you can run:
     ```bash
     echo "your_lifx_token_here" > "/mnt/c/Users/YOUR_USERNAME/.lifx_token"
     ```

### 2. Build the Plugin
Compile the C# solution using .NET 8.0 SDK:
```bash
# Run this inside your workspace
/home/eole/.dotnet/dotnet build LifxPlugin/LifxPlugin.sln \
  -p:PluginApiDir="/mnt/c/Program Files/Logi/LogiPluginService/" \
  -p:PluginDir="/home/eole/projects/actions-sdk/LifxPlugin/build_links/"
```

### 3. Deploy and Run
To deploy the plugin locally on Windows:
1. Copy the compiled `bin/` and `metadata/` directories into a new plugin directory in Options+:
   ```bash
   # Remove any old version and create the target folder
   rm -rf "/mnt/c/Users/YOUR_USERNAME/AppData/Local/Logi/LogiPluginService/Plugins/Lifx"
   mkdir -p "/mnt/c/Users/YOUR_USERNAME/AppData/Local/Logi/LogiPluginService/Plugins/Lifx"
   
   # Copy build directories
   cp -r LifxPlugin/LifxPlugin/Debug/bin "/mnt/c/Users/YOUR_USERNAME/AppData/Local/Logi/LogiPluginService/Plugins/Lifx/"
   cp -r LifxPlugin/LifxPlugin/Debug/metadata "/mnt/c/Users/YOUR_USERNAME/AppData/Local/Logi/LogiPluginService/Plugins/Lifx/"
   ```
2. Restart the **LogiPluginService** on Windows to apply the plugin:
   ```bash
   powershell.exe -Command "Stop-Process -Name LogiPluginService -Force"
   ```

### 4. Assign Actions
1. Open **Logi Options+** on Windows and select the **MX Creative Keypad**.
2. Click **Customize Keys** to open the customization panel.
3. Close the Plugins Manager to return to the **Action Picker** on the right side.
4. Click **`ALL ACTIONS`** at the top of the picker, locate the **LIFX** category, and drag and drop actions (Toggle and Brightness) onto your dial or button keys.

---

## Disclaimer

This is an unofficial, independent plugin developed using Google DeepMind's **Antigravity** AI coding assistant. It is not affiliated with, authorized, maintained, sponsored, or endorsed by Logitech, Loupedeck, or LIFX. All product and company names are trademarks™ or registered® trademarks of their respective holders.

