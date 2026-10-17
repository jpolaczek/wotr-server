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
    if (!response.ok) {
      try {
        error = (await response.json()).error || "";
      } catch (ignored) {
        error = "";
      }
    }
    return { status: response.status, error, retryAfter: response.headers.get("Retry-After") };
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

const PAGES = { register, verify, forgot, reset };

document.addEventListener("DOMContentLoaded", () => {
  const page = PAGES[document.body.dataset.page];
  if (page) page();
});
