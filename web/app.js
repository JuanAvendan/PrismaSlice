// Página de búsqueda de PrismaSlice. Sin dependencias ni build.
// En Vercel la API se sirve por un rewrite (/api → Azure Container Apps), así que
// las llamadas son de mismo origen. En local apunta a la API de `dotnet run`.
const API_BASE = ["localhost", "127.0.0.1"].includes(location.hostname)
  ? "http://localhost:5004"
  : "";

// La API escala a cero: la primera petición tras un rato inactiva tarda más.
const COLD_START_NOTICE_MS = 3000;

const form = document.getElementById("search-form");
const input = document.getElementById("q");
const button = form.querySelector("button");
const statusEl = document.getElementById("status");
const resultsEl = document.getElementById("results");
const template = document.getElementById("result-template");

form.addEventListener("submit", (event) => {
  event.preventDefault();
  search(input.value.trim());
});

document.querySelectorAll(".chip").forEach((chip) =>
  chip.addEventListener("click", () => {
    input.value = chip.textContent;
    search(chip.textContent);
  }));

const initial = new URLSearchParams(location.search).get("q");
if (initial) {
  input.value = initial;
  search(initial);
}

async function search(query) {
  if (!query) return;

  history.replaceState(null, "", `?q=${encodeURIComponent(query)}`);
  setStatus("Searching…");
  button.disabled = true;
  resultsEl.replaceChildren();

  const slow = setTimeout(
    () => setStatus("Waking up the API (it scales to zero when idle)… this takes a few seconds."),
    COLD_START_NOTICE_MS);
  const started = performance.now();

  try {
    const hits = await getJson(`/api/tickets/search?q=${encodeURIComponent(query)}&limit=8`);
    const elapsed = Math.round(performance.now() - started);
    resultsEl.replaceChildren(...hits.map(renderHit));
    setStatus(hits.length
      ? `${hits.length} results · ${elapsed} ms`
      : "No tickets found.");
  } catch (error) {
    setStatus(error.message, true);
  } finally {
    clearTimeout(slow);
    button.disabled = false;
  }
}

function renderHit(hit) {
  const item = template.content.firstElementChild.cloneNode(true);
  const similarity = Math.max(0, Math.min(1, hit.similarity));

  item.querySelector(".code").textContent = hit.code;
  item.querySelector(".title").textContent = hit.title;
  item.querySelector(".fill").style.width = `${(similarity * 100).toFixed(0)}%`;
  item.querySelector(".value").textContent = hit.similarity.toFixed(2);
  item.querySelector(".category").textContent = hit.category;
  item.querySelector(".status-badge").textContent = humanize(hit.status);
  const priority = item.querySelector(".priority");
  priority.textContent = `${hit.priority} priority`;
  priority.dataset.value = hit.priority;

  const head = item.querySelector(".result-head");
  const details = item.querySelector(".details");
  head.addEventListener("click", () => toggleDetails(hit.code, head, details));
  return item;
}

async function toggleDetails(code, head, details) {
  const open = head.getAttribute("aria-expanded") === "true";
  head.setAttribute("aria-expanded", String(!open));
  details.hidden = open;
  if (open || details.childElementCount) return;

  details.textContent = "Loading…";
  try {
    const t = await getJson(`/api/tickets/${encodeURIComponent(code)}`);
    const description = document.createElement("p");
    description.textContent = t.description;

    const facts = document.createElement("dl");
    for (const [label, value] of [
      ["Requester", t.requesterName],
      ["Assignee", t.assigneeName ?? "Unassigned"],
      ["Team", t.teamName ?? "—"],
      ["Labels", t.labels.length ? t.labels.join(", ") : "—"],
      ["Created", new Date(t.createdAt).toLocaleDateString()],
    ]) {
      const dt = document.createElement("dt");
      dt.textContent = label;
      const dd = document.createElement("dd");
      dd.textContent = value;
      facts.append(dt, dd);
    }
    details.replaceChildren(description, facts);
  } catch (error) {
    details.textContent = error.message;
  }
}

async function getJson(path) {
  let response;
  try {
    response = await fetch(API_BASE + path, { headers: { Accept: "application/json" } });
  } catch {
    throw new Error("Could not reach the API. Please try again in a moment.");
  }

  if (response.ok) return response.json();

  // La API responde ProblemDetails (RFC 9457): se muestra su título.
  const problem = await response.json().catch(() => null);
  const messages = {
    429: "Too many searches in a minute. Please wait a moment.",
    503: "Semantic search is temporarily unavailable.",
  };
  throw new Error(messages[response.status] ?? problem?.title ?? `Request failed (${response.status}).`);
}

function setStatus(text, isError = false) {
  statusEl.textContent = text;
  statusEl.classList.toggle("error", isError);
}

function humanize(value) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}
