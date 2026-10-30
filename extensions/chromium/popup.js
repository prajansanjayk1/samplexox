chrome.storage.local.get(["isConnected", "policy"], (data) => {
  const statusEl = document.getElementById("status");
  const focusEl = document.getElementById("focusState");
  const policyEl = document.getElementById("policyInfo");

  if (data.isConnected) {
    statusEl.className = "status-badge connected";
    statusEl.innerHTML = '<span class="dot"></span> Synced with Desktop';
  } else {
    statusEl.className = "status-badge disconnected";
    statusEl.innerHTML = '<span class="dot"></span> Offline / Standalone';
  }

  if (data.policy && data.policy.isFocusActive) {
    focusEl.textContent = `Active (${data.policy.sessionMode || "Deep Focus"})`;
    focusEl.style.color = "#10B981";
    policyEl.textContent = `Shielding ${data.policy.blockedWebsites?.length || 0} Sites`;
  } else {
    focusEl.textContent = "Ready to Focus";
    focusEl.style.color = "#F8FAFC";
    policyEl.textContent = data.policy?.isAdultShieldActive ? "Safe Browsing Active" : "Shield Idle";
  }
});
