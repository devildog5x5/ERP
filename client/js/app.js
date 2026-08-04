import { API_BASE, checkHealth } from "./api.js";
import {
  renderCustomers,
  renderDashboard,
  renderInventory,
  renderPurchaseOrders,
  renderReminders,
  renderSalesOrders,
  renderSettings,
  renderSuppliers,
} from "./pages.js";
import { toast } from "./utils.js";

const routes = {
  "/": renderDashboard,
  "/inventory": renderInventory,
  "/purchase-orders": renderPurchaseOrders,
  "/sales-orders": renderSalesOrders,
  "/suppliers": renderSuppliers,
  "/customers": renderCustomers,
  "/reminders": renderReminders,
  "/settings": renderSettings,
};

function currentPath() {
  const hash = window.location.hash.replace(/^#/, "") || "/";
  return hash.startsWith("/") ? hash : `/${hash}`;
}

function setActiveNav(path) {
  document.querySelectorAll("#main-nav a[data-route]").forEach((link) => {
    link.classList.toggle("active", link.dataset.route === path);
  });
}

async function render() {
  const path = currentPath();
  const view = routes[path] || routes["/"];
  setActiveNav(routes[path] ? path : "/");
  const root = document.getElementById("app");
  root.innerHTML = `<div class="empty">Loading…</div>`;
  try {
    await view(root);
    document.title = `Ledgerly · ${path === "/" ? "Dashboard" : path.slice(1)}`;
  } catch (err) {
    root.innerHTML = `<div class="empty">Failed to load page: ${err.message}<br><br>
      Confirm the API server is running at <span class="mono">${API_BASE}</span>.</div>`;
    toast(err.message, true);
  }
}

async function updateConnectionStatus() {
  const el = document.getElementById("connection-status");
  try {
    const health = await checkHealth();
    el.textContent = `Connected to ${health.app} at ${API_BASE}`;
  } catch {
    el.textContent = `API offline · expected ${API_BASE}`;
  }
}

window.addEventListener("hashchange", render);
window.addEventListener("DOMContentLoaded", async () => {
  if (!window.location.hash) window.location.hash = "#/";
  await updateConnectionStatus();
  await render();
  setInterval(updateConnectionStatus, 15000);
});
