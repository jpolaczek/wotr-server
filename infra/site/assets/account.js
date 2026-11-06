// AUTH-02 (D16): the account pages — registration, email confirmation, password reset request and new password.
// They call the game server on this origin; links from the emails carry their token in the fragment (#token=…),
// which the page reads once and removes from the address bar.
"use strict";

const NAME = /^[A-Za-z0-9._-]{3,32}$/;

function $(id) {
  return document.getElementById(id);
}

function status(text, kind) {
  const node = $("status");
  node.textContent = text;
  node.dataset.kind = kind || "";
}

function done(text) {
  const form = $("form");
  if (form) form.remove();
  status(text, "done");
}

function takeToken() {
  const token = new URLSearchParams(location.hash.slice(1)).get("token") || "";
  history.replaceState(null, "", location.pathname);
  return token;
}

function validEmail(value) {
  const at = value.indexOf("@");
  return value.length <= 254 && at > 0 && at === value.lastIndexOf("@") && /\.[^.]+$/.test(value.slice(at + 1)) && !/\s/.test(value);
}

function minutes(retryAfter) {
  const seconds = parseInt(retryAfter || "", 10);
  return Number.isFinite(seconds) ? Math.max(1, Math.ceil(seconds / 60)) : 10;
}

async function post(path, body) {
  try {
    const response = await fetch(path, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify(body),
    });
    let error = "";
    let data = null;
    try {
      data = await response.json();
      error = response.ok ? "" : data.error || "";
    } catch (ignored) {
      error = "";
    }
    return { status: response.status, data, error, retryAfter: response.headers.get("Retry-After") };
  } catch (failure) {
    return { status: 0, error: "Network" };
  }
}

function common(result) {
  if (result.status === 429) return `Too many attempts from this network. Try again in ${minutes(result.retryAfter)} minutes.`;
  if (result.status === 0) return "The server is not reachable right now. Try again later.";
  return "Something went wrong on the server. Try again later.";
}

function onSubmit(handler) {
  const form = $("form");
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const button = form.querySelector("button");
    button.disabled = true;
    try {
      await handler();
    } finally {
      button.disabled = false;
    }
  });
}

function passwords() {
  const password = $("password").value;
  if (password.length < 12 || password.length > 128) return status("The password needs 12–128 characters.", "error"), null;
  if (password !== $("confirm").value) return status("The two passwords are different.", "error"), null;
  return password;
}

function register() {
  onSubmit(async () => {
    const username = $("username").value.trim();
    const email = $("email").value.trim();
    if (!NAME.test(username)) return status("The account name needs 3–32 letters (a–z), digits or . _ -", "error");
    if (!validEmail(email)) return status("Enter a valid email address.", "error");
    const password = passwords();
    if (password === null) return;
    status("Creating the account…");
    const result = await post("/api/auth/register", { username, email, password });
    if (result.status === 200)
      return done(`Your account ${username} is ready. We sent a confirmation link to ${email}. ` +
        "Open War of the Ring Online and sign in with your name and password.");
    const messages = {
      UsernameTaken: "That account name is taken. Choose another one.",
      EmailTaken: "An account with this email already exists. If it is yours, use “Forgot your password?”.",
      InvalidRegistration: "Check the account name, the email and the password.",
    };
    status(messages[result.error] || common(result), "error");
  });
}

async function verify() {
  const token = takeToken();
  if (!token) return status("This link is incomplete. Open the whole link from the email.", "error");
  const result = await post("/api/account/verify-email", { token });
  if (result.status === 204) return status("Your email is confirmed. You can use it to sign in and to reset your password.", "done");
  status(result.error === "InvalidToken"
    ? "This link is invalid, already used or expired. Ask for a new one under “Forgot password” — an unconfirmed account gets a new confirmation link."
    : common(result), "error");
}

function forgot() {
  onSubmit(async () => {
    const login = $("login").value.trim();
    if (!NAME.test(login) && !validEmail(login)) return status("Enter your account name or email.", "error");
    status("Sending…");
    const result = await post("/api/account/password-reset", { login });
    if (result.status === 202)
      return done("If the account has a confirmed email, we sent it a link to choose a new password (valid for 60 minutes). " +
        "If the email is not confirmed yet, a new confirmation link comes instead. Check the spam folder too.");
    status(common(result), "error");
  });
}

function reset() {
  const token = takeToken();
  if (!token) {
    $("form").remove();
    return status("This link is incomplete. Open the whole link from the email.", "error");
  }
  onSubmit(async () => {
    const password = passwords();
    if (password === null) return;
    status("Changing the password…");
    const result = await post("/api/account/password-reset/confirm", { token, password });
    if (result.status === 204)
      return done("Your password is changed and every device is signed out. Sign in in the game with the new password.");
    const messages = {
      InvalidToken: "This link is invalid, already used or expired. Ask for a new one under “Forgot password”.",
      InvalidPassword: "The password needs 12–128 characters.",
    };
    status(messages[result.error] || common(result), "error");
  });
}

// AVATAR-02 (AVATAR-01 A4-A6): the account page. Its token lives in this variable only, never in storage or the address, so
// a reload asks for the password again. A click on an avatar saves it at once; a refusal keeps the old one and says why.
const AVATARS = [
  ["eye", "The Eye"],
  ["ring", "The Ring"],
  ["gondor", "Soldier of Gondor"],
  ["rohan", "Soldier of Rohan"],
  ["elves", "Elven warrior"],
  ["dwarves", "Dwarven warrior"],
  ["north", "Soldier of the North"],
  ["sauron", "Orc of Sauron"],
  ["isengard", "Uruk-hai of Isengard"],
  ["southrons", "Southron warrior"],
];

let accountToken = "";

async function accountCall(method, path, body) {
  try {
    const response = await fetch(path, {
      method,
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        Authorization: `Bearer ${accountToken}`,
      },
      body: body ? JSON.stringify(body) : undefined,
    });
    const data = response.status === 204 ? null : await response.json().catch(() => null);
    return { status: response.status, data, error: (data && data.error) || "", retryAfter: response.headers.get("Retry-After") };
  } catch (failure) {
    return { status: 0, error: "Network" };
  }
}

function avatarButton(id, label) {
  const button = document.createElement("button");
  button.type = "button";
  button.className = "avatar";
  button.dataset.avatar = id;
  const picture = document.createElement("img");
  picture.src = `/assets/avatars/${id}.png`;
  picture.srcset = `/assets/avatars/${id}.png 1x, /assets/avatars/${id}@2x.png 2x`;
  picture.alt = label;
  picture.width = 96;
  picture.height = 96;
  const name = document.createElement("span");
  name.textContent = label;
  name.setAttribute("aria-hidden", "true");
  const mark = document.createElement("span");
  mark.className = "avatar-mark";
  mark.textContent = "Your avatar";
  button.append(picture, name, mark);
  button.addEventListener("click", () => chooseAvatar(id, label));
  return button;
}

function markAvatar(chosen) {
  for (const button of $("avatars").children) {
    button.setAttribute("aria-pressed", button.dataset.avatar === chosen ? "true" : "false");
  }
}

function busyAvatars(busy) {
  for (const button of $("avatars").children) button.disabled = busy;
}

function showSignIn(message) {
  accountToken = "";
  $("password").value = "";
  $("panel").hidden = true;
  $("sign-in").hidden = false;
  status(message || "", message ? "error" : "");
}

function showAccount(name, avatar) {
  $("name").textContent = name;
  markAvatar(avatar);
  $("sign-in").hidden = true;
  $("panel").hidden = false;
}

async function chooseAvatar(id, label) {
  busyAvatars(true);
  const result = await accountCall("PUT", "/api/account/avatar", { avatar: id });
  busyAvatars(false);
  if (result.status === 401) {
    showSignIn("Your session has ended. Sign in again.");
  } else if (result.status === 200) {
    markAvatar(result.data.avatar);
    status(`${label} is now your avatar.`, "done");
  } else {
    status(`Your avatar did not change. ${common(result)}`, "error");
  }
}

async function signOut() {
  await accountCall("DELETE", "/api/account/session");
  showSignIn();
  status("You have signed out.", "done");
}

function profile() {
  const grid = $("avatars");
  for (const [id, label] of AVATARS) grid.append(avatarButton(id, label));
  $("sign-out").addEventListener("click", signOut);
  onSubmit(async () => {
    const username = $("username").value.trim();
    const password = $("password").value;
    if (!username || !password) return status("Enter your account name and password.", "error");
    const result = await post("/api/account/session", { username, password });
    if (result.status === 401) return status("Account name or password not accepted. Try again, or reset your password.", "error");
    if (result.status !== 200) return status(common(result), "error");
    accountToken = result.data.token;
    $("password").value = "";
    status("", "");
    showAccount(result.data.name, result.data.avatar);
  });
}

const PAGES = { register, verify, forgot, reset, profile };

document.addEventListener("DOMContentLoaded", () => {
  const page = PAGES[document.body.dataset.page];
  if (page) page();
});
