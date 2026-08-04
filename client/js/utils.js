export function money(n) {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(Number(n || 0));
}

export function toast(message, isError = false) {
  const host = document.getElementById("toast-host");
  if (!host) return;
  const el = document.createElement("div");
  el.className = "toast";
  if (isError) el.style.background = "#7f1d1d";
  el.textContent = message;
  host.appendChild(el);
  setTimeout(() => el.remove(), 3200);
}

export function badgeForStatus(status) {
  const map = {
    draft: "badge",
    ordered: "badge warn",
    partial: "badge warn",
    received: "badge ok",
    cancelled: "badge danger",
    confirmed: "badge warn",
    fulfilled: "badge ok",
  };
  return `<span class="${map[status] || "badge"}">${status}</span>`;
}

export function severityClass(severity) {
  if (severity === "critical") return "critical";
  if (severity === "warning") return "warning";
  return "";
}

export function openModal(id) {
  document.getElementById(id)?.classList.add("open");
}

export function closeModal(id) {
  document.getElementById(id)?.classList.remove("open");
}

export function bindModalClosers(root = document) {
  root.querySelectorAll("[data-close]").forEach((btn) => {
    btn.addEventListener("click", () => closeModal(btn.dataset.close));
  });
}
