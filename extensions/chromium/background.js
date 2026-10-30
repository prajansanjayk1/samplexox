// Quietora Focus v4.0 — Browser Shield Background Service Worker (Manifest V3)
const SYNC_URL = "http://127.0.0.1:41789/api/policy";
const SYNC_ALARM_NAME = "quietora-policy-sync";

// Default educational allowlist
const DEFAULT_ALLOWLIST = [
  "edu",
  "ac.in",
  "khanacademy.org",
  "coursera.org",
  "edx.org",
  "mit.edu",
  "stanford.edu",
  "harvard.edu",
  "stackoverflow.com",
  "github.com",
  "wikipedia.org",
  "arxiv.org",
  "scholar.google.com",
  "canvas.instructure.com",
  "blackboard.com"
];

// Adult content block list (sample representative core patterns)
const ADULT_DOMAINS = [
  "pornhub.com",
  "xvideos.com",
  "xnxx.com",
  "xhamster.com",
  "redtube.com",
  "youporn.com",
  "chaturbate.com",
  "onlyfans.com",
  "stripchat.com",
  "livejasmin.com",
  "cam4.com",
  "adultfriendfinder.com"
];

chrome.runtime.onInstalled.addListener(() => {
  console.log("[Quietora Shield] Extension installed. Initializing alarms and default rules.");
  chrome.alarms.create(SYNC_ALARM_NAME, { periodInMinutes: 0.1 }); // Sync every 6 seconds
  syncPolicyWithDesktop();
});

chrome.alarms.onAlarm.addListener((alarm) => {
  if (alarm.name === SYNC_ALARM_NAME) {
    syncPolicyWithDesktop();
  }
});

async function syncPolicyWithDesktop() {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 2000);

    const response = await fetch(SYNC_URL, {
      method: "GET",
      signal: controller.signal,
      headers: { "Content-Type": "application/json" }
    });
    clearTimeout(timeoutId);

    if (response.ok) {
      const policy = await response.json();
      await chrome.storage.local.set({
        lastSyncUtc: new Date().toISOString(),
        isConnected: true,
        policy: policy
      });
      await applyDnrRules(policy);
    } else {
      markDisconnected();
    }
  } catch (err) {
    markDisconnected();
  }
}

async function markDisconnected() {
  const data = await chrome.storage.local.get(["policy"]);
  await chrome.storage.local.set({ isConnected: false });
  // Maintain cached policy if recently active
  if (data.policy) {
    await applyDnrRules(data.policy);
  }
}

async function applyDnrRules(policy) {
  const isFocusActive = policy.isFocusActive ?? false;
  const isAdultShieldActive = policy.isAdultShieldActive ?? true;
  const blockedSites = policy.blockedWebsites ?? [
    "youtube.com", "instagram.com", "reddit.com", "tiktok.com", "facebook.com", "twitter.com", "x.com", "netflix.com", "twitch.tv"
  ];
  const allowlist = [...DEFAULT_ALLOWLIST, ...(policy.allowlistedDomains ?? [])];
  const blockShortsOnly = policy.blockShortsOnly ?? false;

  const existingRules = await chrome.declarativeNetRequest.getDynamicRules();
  const ruleIdsToRemove = existingRules.map(r => r.id);

  const newRules = [];
  let ruleId = 1;

  // 1. Allowlist rules (Priority 3000)
  for (const domain of allowlist) {
    newRules.push({
      id: ruleId++,
      priority: 3000,
      action: { type: "allow" },
      condition: {
        urlFilter: `||${domain}`,
        resourceTypes: ["main_frame", "sub_frame"]
      }
    });
  }

  // 2. Adult Content Filtering (Priority 2500)
  if (isAdultShieldActive) {
    for (const domain of ADULT_DOMAINS) {
      newRules.push({
        id: ruleId++,
        priority: 2500,
        action: {
          type: "redirect",
          redirect: {
            extensionPath: `/blocked.html?reason=adult&domain=${encodeURIComponent(domain)}`
          }
        },
        condition: {
          urlFilter: `||${domain}`,
          resourceTypes: ["main_frame"]
        }
      });
    }
  }

  // 3. Short-Form Video Interceptor (Priority 2200)
  // When active, intercepts shorts and reels
  if (isFocusActive || blockShortsOnly) {
    const shortFormPaths = [
      "*://*.youtube.com/shorts*",
      "*://*.instagram.com/reels*",
      "*://*.instagram.com/explore*",
      "*://*.tiktok.com/*",
      "*://*.reddit.com/r/popular*"
    ];

    for (const urlPattern of shortFormPaths) {
      newRules.push({
        id: ruleId++,
        priority: 2200,
        action: {
          type: "redirect",
          redirect: {
            extensionPath: `/blocked.html?reason=shorts&url=${encodeURIComponent(urlPattern)}`
          }
        },
        condition: {
          urlFilter: urlPattern,
          resourceTypes: ["main_frame"]
        }
      });
    }
  }

  // 4. Whole Website Blocks (Priority 1500)
  if (isFocusActive && !blockShortsOnly) {
    for (const site of blockedSites) {
      const clean = site.toLowerCase().replace("https://", "").replace("http://", "").replace("www.", "").trim();
      if (!clean) continue;

      newRules.push({
        id: ruleId++,
        priority: 1500,
        action: {
          type: "redirect",
          redirect: {
            extensionPath: `/blocked.html?reason=focus&domain=${encodeURIComponent(clean)}`
          }
        },
        condition: {
          urlFilter: `||${clean}`,
          resourceTypes: ["main_frame"]
        }
      });
    }
  }

  try {
    await chrome.declarativeNetRequest.updateDynamicRules({
      removeRuleIds: ruleIdsToRemove,
      addRules: newRules
    });
    console.log(`[Quietora Shield] Applied ${newRules.length} dynamic filtering rules.`);
  } catch (ex) {
    console.error("[Quietora Shield] Failed to update rules:", ex);
  }
}
