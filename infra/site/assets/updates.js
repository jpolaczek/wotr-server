// DIST-15: the What's new page. It lists the notes of the announced releases (GET /api/client/notes), newest first,
// each at #v<version> so the game's What's new link opens the right one. Notes are plain text: a line starting with
// "- " is a list item, any other line a paragraph; every text is written with textContent.
"use strict";

function element(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

function noteBody(notes) {
  const body = element("div", "release-body");
  let list = null;
  for (const raw of notes.split(/\r?\n/)) {
    const line = raw.trim();
    if (!line) {
      list = null;
      continue;
    }
    if (line.startsWith("- ") || line.startsWith("* ")) {
      if (!list) list = body.appendChild(element("ul"));
      list.appendChild(element("li", "", line.slice(2).trim()));
    } else {
      list = null;
      body.appendChild(element("p", "", line.replace(/^#+\s*/, "")));
    }
  }
  return body;
}

function published(at) {
  const date = new Date(at);
  return isNaN(date) ? "" : date.toLocaleDateString("en-GB", { year: "numeric", month: "long", day: "numeric" });
}

function release(entry, newest) {
  const article = element("article", "release-note");
  article.id = "v" + entry.version;
  const head = element("header", "release-head");
  head.appendChild(element("h2", "", "Version " + entry.version));
  const when = published(entry.publishedAt);
  head.appendChild(element("p", "meta", newest ? (when ? when + " · latest" : "latest") : when));
  article.appendChild(head);
  article.appendChild(entry.notes && entry.notes.trim() ? noteBody(entry.notes) : element("p", "note", "No notes for this version."));
  return article;
}

function reveal() {
  const id = decodeURIComponent(location.hash.slice(1));
  const target = id && document.getElementById(id);
  if (!target) return;
  target.classList.add("current");
  target.scrollIntoView({ block: "start" });
}

async function load() {
  const status = document.getElementById("status");
  const list = document.getElementById("releases");
  try {
    const response = await fetch("/api/client/notes", { headers: { Accept: "application/json" } });
    if (!response.ok) throw new Error(String(response.status));
    const releases = (await response.json()).releases || [];
    releases.forEach((entry, index) => list.appendChild(release(entry, index === 0)));
    status.textContent = releases.length ? "" : "No version has been published yet.";
    reveal();
  } catch (failure) {
    status.textContent = "The notes could not be loaded right now. Try again in a moment.";
  }
}

window.addEventListener("hashchange", reveal);
load();
