// DIST-04: download buttons from the server's release catalog (GET /api/client/downloads), never a fixed version.
// D18: macOS, Linux (also for the Steam Deck) and, from 2026-10-17 before its tester acceptance (DIST-13), Windows
// as experimental.
"use strict";

const OFFERED = {
  "windows-x64": "Download for Windows (experimental)",
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

function installerOffer(release, installer) {
  const link = element("a", { className: "download", href: installer.url, textContent: OFFERED[installer.platform] });
  const meta = element("p", { className: "meta" }, [
    `Version ${release.version} · ${megabytes(installer.size)} · SHA-256 `,
    element("code", { textContent: installer.sha256 }),
  ]);
  return element("div", {}, [link, meta]);
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
    show(container, offers.map((installer) => installerOffer(release, installer)));
  } catch (error) {
    show(container, [element("p", { className: "status", textContent: "The download list is not available right now. Please try again later." })]);
  }
}

document.addEventListener("DOMContentLoaded", loadDownloads);
