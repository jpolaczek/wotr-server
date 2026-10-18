// ADMIN-01: the admin panel. It signs an administrator in for a short admin session (POST /api/admin/session), shows
// the counts of players and games and pages through the accounts. The token stays in this page's memory only, so a
// reload or a closed tab signs out; every text is written with textContent.
"use strict";

const PAGE_SIZE = 50;
const SUMMARY = [
  ["users", "Players"],
  ["confirmedUsers", "Confirmed emails"],
  ["signedInLastWeek", "Signed in, last 7 days"],
  ["tablesCreated", "Tables created"],
  ["tablesWaiting", "Tables waiting now"],
  ["gamesStarted", "Games started"],
  ["gamesFinished", "Games finished"],
  ["gamesInProgress", "Games in progress"],
  ["gamesRemovedUnfinished", "Removed unfinished"],
  ["freePeoplesWins", "Free Peoples wins"],
  ["shadowWins", "Shadow wins"],
  ["testSessions", "Test scenario sessions"],
  ["demoGames", "Demo games vs CPU"],
];

let token = "";
let page = 0;
let pages = 1;

function $(id) {
  return document.getElementById(id);
}

function status(id, text, kind) {
  const node = $(id);
  node.textContent = text;
  node.dataset.kind = kind || "";
}

async function request(method, path, body) {
  const headers = { Accept: "application/json" };
  if (body) headers["Content-Type"] = "application/json";
  if (token) headers.Authorization = "Bearer " + token;
  try {
    const response = await fetch(path, { method, headers, body: body ? JSON.stringify(body) : undefined, cache: "no-store" });
    const text = await response.text();
    const retryAfter = Number(response.headers.get("Retry-After")) || 0;
    return { status: response.status, data: text ? JSON.parse(text) : null, retryAfter };
  } catch (failure) {
    return { status: 0, data: null };
  }
}

function problem(result) {
  // SEC-02: a name locked after a series of failed sign-ins waits for its lock, in whole minutes.
  if (result.status === 429 && result.data && result.data.error === "SignInLocked") {
    const minutes = Math.max(1, Math.ceil(result.retryAfter / 60));
    return `Too many failed sign-ins for this name. Try again in ${minutes} ${minutes === 1 ? "minute" : "minutes"}.`;
  }
  if (result.status === 429) return "Too many requests. Wait a minute and try again.";
  if (result.status === 0) return "The server is not reachable right now.";
  return "Something went wrong on the server.";
}

function show(signedIn) {
  $("sign-in").hidden = signedIn;
  $("panel").hidden = !signedIn;
}

async function signIn() {
  const username = $("username").value.trim();
  const password = $("password").value;
  if (!username || !password) {
    status("status", "Enter the account name and the password.", "error");
    return;
  }
  const result = await request("POST", "/api/admin/session", { username, password });
  $("password").value = "";
  if (result.status !== 200) {
    status("status", result.status === 404
      ? "Not accepted: a wrong name or password, or the account is not an administrator."
      : problem(result), "error");
    return;
  }
  token = result.data.token;
  status("status", "");
  $("signed-in").textContent = "Signed in as " + result.data.username + " · until " + time(result.data.expiresAt);
  show(true);
  page = 0;
  await load();
}

async function signOut() {
  if (token) await request("POST", "/api/auth/logout");
  ended("Signed out.");
}

function ended(text) {
  token = "";
  show(false);
  status("status", text, "done");
}

async function load() {
  status("panel-status", "Loading…");
  const [summary, users] = await Promise.all([
    request("GET", "/api/admin/summary"),
    request("GET", `/api/admin/users?page=${page}&size=${PAGE_SIZE}`),
  ]);
  if (summary.status === 404 || users.status === 404) {
    ended("The admin session has ended. Sign in again.");
    return;
  }
  if (summary.status !== 200 || users.status !== 200) {
    status("panel-status", problem(summary.status !== 200 ? summary : users), "error");
    return;
  }
  renderSummary(summary.data);
  renderUsers(users.data);
  status("panel-status", "");
}

function renderSummary(summary) {
  const list = $("summary");
  list.replaceChildren();
  for (const [key, label] of SUMMARY) {
    const tile = document.createElement("div");
    const term = document.createElement("dt");
    const value = document.createElement("dd");
    term.textContent = label;
    value.textContent = Number(summary[key]).toLocaleString();
    tile.append(term, value);
    list.append(tile);
  }
  $("summary-at").textContent = "Counted at " + time(summary.at) + ". Test scenarios and demo games are not games between players.";
}

function renderUsers(result) {
  const body = $("users").tBodies[0];
  body.replaceChildren();
  for (const user of result.users) {
    const row = document.createElement("tr");
    row.append(
      cell(user.username),
      cell(user.createdAt ? time(user.createdAt) : "before the panel"),
      cell(user.lastSignInAt ? time(user.lastSignInAt) : "—"),
      cell(user.emailConfirmed ? "yes" : "no"),
      cell(user.tablesHosted, true),
      cell(user.games, true),
      cell(user.gamesFinished, true),
      cell(user.wins, true));
    body.append(row);
  }
  pages = Math.max(1, Math.ceil(result.total / result.pageSize));
  $("page").textContent = `Page ${result.page + 1} of ${pages} · ${result.total} accounts`;
  $("previous").disabled = result.page === 0;
  $("next").disabled = result.page + 1 >= pages;
}

function cell(value, number) {
  const node = document.createElement("td");
  node.textContent = number ? Number(value).toLocaleString() : String(value);
  if (number) node.className = "number";
  return node;
}

function time(value) {
  return new Date(value).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
}

function busy(button, action) {
  return async () => {
    button.disabled = true;
    try {
      await action();
    } finally {
      button.disabled = false;
    }
  };
}

document.addEventListener("DOMContentLoaded", () => {
  const form = $("form");
  const submit = form.querySelector("button");
  form.addEventListener("submit", (event) => {
    event.preventDefault();
    busy(submit, signIn)();
  });
  $("refresh").addEventListener("click", busy($("refresh"), load));
  $("sign-out").addEventListener("click", busy($("sign-out"), signOut));
  $("previous").addEventListener("click", () => {
    if (page > 0) {
      page--;
      load();
    }
  });
  $("next").addEventListener("click", () => {
    if (page + 1 < pages) {
      page++;
      load();
    }
  });
});
