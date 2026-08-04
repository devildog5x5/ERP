const DEFAULT_API = "http://127.0.0.1:8000";

function resolveApiBase() {
  const params = new URLSearchParams(window.location.search);
  const fromQuery = params.get("api");
  if (fromQuery) {
    localStorage.setItem("ledgerly.apiBase", fromQuery);
    return fromQuery.replace(/\/$/, "");
  }
  const stored = localStorage.getItem("ledgerly.apiBase");
  if (stored) return stored.replace(/\/$/, "");
  return DEFAULT_API;
}

export const API_BASE = resolveApiBase();

export async function api(path, options = {}) {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { "Content-Type": "application/json", ...(options.headers || {}) },
    ...options,
  });
  const text = await res.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  if (!res.ok) {
    const detail = data?.detail || data || res.statusText;
    throw new Error(typeof detail === "string" ? detail : JSON.stringify(detail));
  }
  return data;
}

export async function checkHealth() {
  return api("/api/health");
}
