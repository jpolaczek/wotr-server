// DIST-04: download buttons from the server's release catalog (GET /api/client/downloads), never a fixed version.
// D18: macOS, Linux (also for the Steam Deck) and Windows; Windows is no longer experimental since the owner's check
// of 2026-10-18 (DIST-13).
"use strict";

const OFFERED = {
  "windows-x64": "Download for Windows",
  "macos-universal": "Download for macOS",
  "linux-x64": "Download for Linux (AppImage)",
};

function element(tag, properties, children) {
  const node = Object.assign(document.createElement(tag), properties || {});
  (children || []).forEach((child) => node.append(child));
  return node;
}

function megabytes(bytes) {
  return (bytes / 1048576).toFixed(0) + " MB";
}

function show(container, nodes) {
  container.replaceChildren(...nodes);
}

// The visitor's own system, from the browser's description of it; "" when unknown. Only the order of the offers
// depends on it.
function visitorPlatform() {
  const agent = navigator.userAgent || "";
  if (/Windows/i.test(agent)) return "windows-x64";
  if (/Macintosh|Mac OS X/i.test(agent)) return "macos-universal";
  if (/Linux|X11|CrOS/i.test(agent) && !/Android/i.test(agent)) return "linux-x64";
  return "";
}

function installerOffer(release, installer, mine) {
  const link = element("a", { className: "download", href: installer.url, textContent: OFFERED[installer.platform] });
  const meta = element("p", { className: "meta" }, [
    `Version ${release.version} · ${megabytes(installer.size)} · SHA-256 `,
    element("code", { textContent: installer.sha256 }),
  ]);
  const label = mine ? [element("p", { className: "mine-label", textContent: "For your computer" })] : [];
  return element("div", { className: mine ? "offer mine" : "offer" }, [...label, link, meta]);
}

async function loadDownloads() {
  const container = document.getElementById("downloads");
  try {
    const response = await fetch("/api/client/downloads", { headers: { Accept: "application/json" } });
    if (!response.ok) throw new Error("catalog " + response.status);
    const release = (await response.json()).latest;
    const offers = release
      ? release.installers.filter((installer) => installer.platform in OFFERED && installer.url.startsWith("https://"))
      : [];
    if (offers.length === 0) {
      show(container, [element("p", { className: "status", textContent: "The first version is on its way. Check back soon." })]);
      return;
    }
    const mine = visitorPlatform();
    offers.sort((first, second) => (second.platform === mine) - (first.platform === mine));
    show(container, offers.map((installer) => installerOffer(release, installer, installer.platform === mine)));
  } catch (error) {
    show(container, [element("p", { className: "status", textContent: "The download list is not available right now. Please try again later." })]);
  }
}

document.addEventListener("DOMContentLoaded", loadDownloads);
