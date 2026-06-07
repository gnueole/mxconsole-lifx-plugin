/**
 * Logitech Options+ Extension Background Script (plugin.js)
 * Controls LIFX lights via the Cloud API.
 */

const logi = chrome.logiOptions || window.logiOptions;

/**
 * Sends a state or control command to the LIFX API.
 * 
 * @param {string} endpoint The endpoint path (e.g. "lights/all/state")
 * @param {string} method The HTTP method (e.g. "PUT" or "POST")
 * @param {object|null} body Optional JSON payload
 */
async function callLifxAPI(endpoint, method = "POST", body = null) {
  try {
    const config = await logi.settings.getAll();
    const token = config.lifx_token;

    if (!token || token.trim() === "") {
      console.error("[LIFX Plugin] Error: LIFX Access Token (lifx_token) is not configured in settings. Aborting request.");
      return;
    }

    const headers = {
      "Authorization": "Bearer " + token.trim(),
      "Content-Type": "application/json"
    };

    const options = {
      method: method,
      headers: headers
    };

    if (body) {
      options.body = JSON.stringify(body);
    }

    console.log(`[LIFX Plugin] Sending ${method} request to ${endpoint}...`);
    const response = await fetch(`https://api.lifx.com/v1/${endpoint}`, options);

    if (response.ok) {
      console.log(`[LIFX Plugin] Request to ${endpoint} executed successfully.`);
    } else {
      const errorText = await response.text();
      console.error(`[LIFX Plugin] API returned error status ${response.status}: ${errorText}`);
    }
  } catch (error) {
    console.error(`[LIFX Plugin] Network or execution error calling LIFX API:`, error);
  }
}

// 1. Action to Turn On all lights
logi.actions.onTriggered("lifx_on", async () => {
  console.log("[LIFX Plugin] Triggered: Turn On All Lights");
  await callLifxAPI("lights/all/state", "PUT", { power: "on" });
});

// 2. Action to Turn Off all lights
logi.actions.onTriggered("lifx_off", async () => {
  console.log("[LIFX Plugin] Triggered: Turn Off All Lights");
  await callLifxAPI("lights/all/state", "PUT", { power: "off" });
});

// 3. Action to Toggle all lights
logi.actions.onTriggered("lifx_toggle", async () => {
  console.log("[LIFX Plugin] Triggered: Toggle All Lights");
  await callLifxAPI("lights/all/toggle", "POST");
});

// 4. Action to Activate Relax Scene
logi.actions.onTriggered("lifx_scene_relax", async () => {
  console.log("[LIFX Plugin] Triggered: Activate Relax Scene");
  try {
    const config = await logi.settings.getAll();
    const token = config.lifx_token;

    if (!token || token.trim() === "") {
      console.error("[LIFX Plugin] Error: LIFX Access Token is not configured. Aborting scene activation.");
      return;
    }

    console.log("[LIFX Plugin] Fetching scenes to find 'Relax'...");
    const response = await fetch("https://api.lifx.com/v1/scenes", {
      headers: {
        "Authorization": "Bearer " + token.trim()
      }
    });

    if (response.ok) {
      const scenes = await response.json();
      const relaxScene = scenes.find(s => s.name && s.name.toLowerCase() === "relax");
      if (relaxScene) {
        console.log(`[LIFX Plugin] Found 'Relax' scene with ID: ${relaxScene.id}. Activating...`);
        await callLifxAPI(`scenes/scene_id:${relaxScene.id}/activate`, "PUT");
      } else {
        console.warn("[LIFX Plugin] Could not find any scene named 'Relax' in your LIFX account.");
      }
    } else {
      const errorText = await response.text();
      console.error(`[LIFX Plugin] Failed to fetch scenes. Status ${response.status}: ${errorText}`);
    }
  } catch (error) {
    console.error("[LIFX Plugin] Error finding or activating Relax scene:", error);
  }
});
