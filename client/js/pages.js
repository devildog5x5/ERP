import { api } from "./api.js";
import {
  badgeForStatus,
  bindModalClosers,
  closeModal,
  money,
  openModal,
  severityClass,
  toast,
} from "./utils.js";

export async function renderDashboard(root) {
  root.innerHTML = `
    <div class="topbar">
      <div>
        <p class="eyebrow">Overview</p>
        <h1>Operations dashboard</h1>
        <p>Monitor stock health, open buying, and critical reminders across the business.</p>
      </div>
      <div class="actions">
        <button class="btn secondary" id="run-reminders">Run reminder scan</button>
        <a class="btn" href="#/purchase-orders">Create purchase order</a>
      </div>
    </div>
    <div class="grid-kpi">
      <div class="kpi"><div class="kpi-label">Products</div><div class="kpi-value" id="kpi-products">—</div><div class="kpi-meta">Active catalog items</div></div>
      <div class="kpi alert"><div class="kpi-label">Low stock</div><div class="kpi-value" id="kpi-low">—</div><div class="kpi-meta">At or below reorder point</div></div>
      <div class="kpi"><div class="kpi-label">Open POs</div><div class="kpi-value" id="kpi-pos">—</div><div class="kpi-meta">Draft, ordered, or partial</div></div>
      <div class="kpi"><div class="kpi-label">Inventory value</div><div class="kpi-value" id="kpi-value">—</div><div class="kpi-meta">At current unit cost</div></div>
    </div>
    <div class="panel-grid">
      <section class="panel">
        <div class="panel-header"><h2>Low stock / buy now</h2><a class="btn ghost sm" href="#/inventory">View inventory</a></div>
        <div class="table-wrap" id="low-stock-table"></div>
      </section>
      <section class="panel">
        <div class="panel-header"><h2>Active reminders</h2><a class="btn ghost sm" href="#/reminders">Open inbox</a></div>
        <div class="stack" id="reminder-list"></div>
      </section>
    </div>
    <section class="panel spaced">
      <div class="panel-header"><h2>Pending purchase orders</h2></div>
      <div class="table-wrap" id="po-table"></div>
    </section>`;

  async function load() {
    const data = await api("/api/dashboard");
    document.getElementById("kpi-products").textContent = data.product_count;
    document.getElementById("kpi-low").textContent = data.low_stock_count;
    document.getElementById("kpi-pos").textContent = data.open_po_count;
    document.getElementById("kpi-value").textContent = money(data.inventory_value);

    const lowHost = document.getElementById("low-stock-table");
    lowHost.innerHTML = data.low_stock_products.length
      ? `<table><thead><tr><th>SKU</th><th>Product</th><th>On hand</th><th>Reorder at</th><th>Buy qty</th></tr></thead>
         <tbody>${data.low_stock_products.map((p) => `<tr>
           <td class="mono">${p.sku}</td><td>${p.name}</td>
           <td><span class="badge ${p.quantity_on_hand <= 0 ? "danger" : "warn"}">${p.quantity_on_hand} ${p.unit}</span></td>
           <td>${p.reorder_point}</td><td>${p.reorder_quantity}</td></tr>`).join("")}</tbody></table>`
      : `<div class="empty">All stocked products are above reorder point.</div>`;

    const remHost = document.getElementById("reminder-list");
    remHost.innerHTML = data.recent_reminders.length
      ? data.recent_reminders.map((r) => `
        <article class="reminder-card ${severityClass(r.severity)}">
          <div class="reminder-meta"><span>${r.reminder_type.replaceAll("_", " ")}</span>
          <span class="badge ${r.severity === "critical" ? "danger" : "warn"}">${r.severity}</span></div>
          <h3>${r.title}</h3><p>${r.message}</p></article>`).join("")
      : `<div class="empty">No open reminders.</div>`;

    const poHost = document.getElementById("po-table");
    poHost.innerHTML = data.pending_purchase_orders.length
      ? `<table><thead><tr><th>PO</th><th>Supplier</th><th>Status</th><th>Expected</th><th>Total</th></tr></thead>
         <tbody>${data.pending_purchase_orders.map((po) => `<tr>
           <td class="mono">${po.po_number}</td><td>${po.supplier?.name || "—"}</td>
           <td>${badgeForStatus(po.status)}</td><td>${po.expected_date || "—"}</td>
           <td>${money(po.total)}</td></tr>`).join("")}</tbody></table>`
      : `<div class="empty">No open purchase orders.</div>`;
  }

  document.getElementById("run-reminders").onclick = async () => {
    try {
      const result = await api("/api/reminders/run", { method: "POST" });
      toast(`Scan done · ${result.emails_sent} email(s) sent`);
      await load();
    } catch (err) {
      toast(err.message, true);
    }
  };

  await load();
}

export async function renderInventory(root) {
  root.innerHTML = `
    <div class="topbar">
      <div><p class="eyebrow">Operations</p><h1>Inventory</h1>
      <p>Track quantities, reorder points, and suggested buy amounts.</p></div>
      <div class="actions">
        <button class="btn secondary" id="filter-low">Show low stock</button>
        <button class="btn" id="open-product-modal">Add product</button>
      </div>
    </div>
    <section class="panel"><div class="table-wrap" id="products-table"></div></section>
    <div class="modal-backdrop" id="product-modal"><div class="modal">
      <h2>Add product</h2>
      <p class="modal-lead">Define stock thresholds so Ledgerly can alert before you run out.</p>
      <form id="product-form" class="form-grid three">
        <div class="field"><label>SKU</label><input name="sku" required /></div>
        <div class="field"><label>Name</label><input name="name" required /></div>
        <div class="field"><label>Category</label><input name="category" /></div>
        <div class="field"><label>Unit</label><input name="unit" value="ea" /></div>
        <div class="field"><label>Qty on hand</label><input name="quantity_on_hand" type="number" step="0.01" value="0" /></div>
        <div class="field"><label>Reorder point</label><input name="reorder_point" type="number" step="0.01" value="10" /></div>
        <div class="field"><label>Reorder qty</label><input name="reorder_quantity" type="number" step="0.01" value="25" /></div>
        <div class="field"><label>Unit cost</label><input name="unit_cost" type="number" step="0.01" value="0" /></div>
        <div class="field"><label>Sell price</label><input name="sell_price" type="number" step="0.01" value="0" /></div>
        <div class="field"><label>Supplier</label><select name="supplier_id" id="supplier-select"><option value="">None</option></select></div>
        <div class="field full"><label>Description</label><textarea name="description"></textarea></div>
        <div class="modal-actions full">
          <button type="button" class="btn secondary" data-close="product-modal">Cancel</button>
          <button class="btn" type="submit">Save product</button>
        </div>
      </form>
    </div></div>
    <div class="modal-backdrop" id="adjust-modal"><div class="modal">
      <h2>Adjust stock</h2>
      <form id="adjust-form" class="form-grid">
        <input type="hidden" name="product_id" />
        <div class="field"><label>Quantity delta (+/-)</label><input name="quantity_delta" type="number" step="0.01" required /></div>
        <div class="field"><label>Notes</label><input name="notes" placeholder="Cycle count, damage, etc." /></div>
        <div class="modal-actions full">
          <button type="button" class="btn secondary" data-close="adjust-modal">Cancel</button>
          <button class="btn" type="submit">Apply</button>
        </div>
      </form>
    </div></div>`;

  bindModalClosers(root);
  let lowOnly = false;

  async function loadSuppliers() {
    const suppliers = await api("/api/suppliers");
    document.getElementById("supplier-select").innerHTML =
      `<option value="">None</option>` + suppliers.map((s) => `<option value="${s.id}">${s.name}</option>`).join("");
  }

  async function loadProducts() {
    const products = await api(`/api/products${lowOnly ? "?low_stock=true" : ""}`);
    const host = document.getElementById("products-table");
    if (!products.length) {
      host.innerHTML = `<div class="empty">No products yet.</div>`;
      return;
    }
    host.innerHTML = `<table><thead><tr>
      <th>SKU</th><th>Name</th><th>On hand</th><th>Reorder</th><th>Buy qty</th><th>Cost</th><th>Price</th><th></th>
      </tr></thead><tbody>${products.map((p) => `<tr>
        <td class="mono">${p.sku}</td>
        <td>${p.name}<span class="cell-sub">${p.category || "Uncategorized"}</span></td>
        <td><span class="badge ${p.needs_reorder ? (p.quantity_on_hand <= 0 ? "danger" : "warn") : "ok"}">${p.quantity_on_hand} ${p.unit}</span></td>
        <td>${p.reorder_point}</td><td>${p.reorder_quantity}</td>
        <td>${money(p.unit_cost)}</td><td>${money(p.sell_price)}</td>
        <td><button class="btn ghost sm" data-adjust="${p.id}">Adjust</button></td>
      </tr>`).join("")}</tbody></table>`;
    host.querySelectorAll("[data-adjust]").forEach((btn) => {
      btn.onclick = () => {
        document.querySelector('#adjust-form [name="product_id"]').value = btn.dataset.adjust;
        openModal("adjust-modal");
      };
    });
  }

  document.getElementById("open-product-modal").onclick = () => openModal("product-modal");
  document.getElementById("filter-low").onclick = async () => {
    lowOnly = !lowOnly;
    document.getElementById("filter-low").textContent = lowOnly ? "Show all" : "Show low stock";
    await loadProducts();
  };
  document.getElementById("product-form").onsubmit = async (e) => {
    e.preventDefault();
    const payload = Object.fromEntries(new FormData(e.target).entries());
    ["quantity_on_hand", "reorder_point", "reorder_quantity", "unit_cost", "sell_price"].forEach(
      (k) => (payload[k] = Number(payload[k] || 0)),
    );
    payload.supplier_id = payload.supplier_id ? Number(payload.supplier_id) : null;
    try {
      await api("/api/products", { method: "POST", body: JSON.stringify(payload) });
      closeModal("product-modal");
      e.target.reset();
      toast("Product created");
      await loadProducts();
    } catch (err) {
      toast(err.message, true);
    }
  };
  document.getElementById("adjust-form").onsubmit = async (e) => {
    e.preventDefault();
    const fd = new FormData(e.target);
    try {
      await api(`/api/products/${fd.get("product_id")}/adjust`, {
        method: "POST",
        body: JSON.stringify({
          quantity_delta: Number(fd.get("quantity_delta")),
          notes: fd.get("notes") || null,
        }),
      });
      closeModal("adjust-modal");
      e.target.reset();
      toast("Stock adjusted");
      await loadProducts();
    } catch (err) {
      toast(err.message, true);
    }
  };

  await Promise.all([loadSuppliers(), loadProducts()]);
}

export async function renderPurchaseOrders(root) {
  root.innerHTML = `
    <div class="topbar">
      <div><p class="eyebrow">Operations</p><h1>Purchasing</h1>
      <p>Buy stock, track expected deliveries, and receive quantities into inventory.</p></div>
      <div class="actions">
        <button class="btn secondary" id="load-suggestions">Suggest from low stock</button>
        <button class="btn" id="open-po-modal">New purchase order</button>
      </div>
    </div>
    <section class="panel" style="margin-bottom:16px" id="suggestions-panel" hidden>
      <div class="panel-header"><h2>Reorder suggestions</h2></div>
      <div id="suggestions"></div>
    </section>
    <section class="panel"><div class="table-wrap" id="po-table"></div></section>
    <div class="modal-backdrop" id="po-modal"><div class="modal">
      <h2>New purchase order</h2>
      <p class="modal-lead">Place an order with a supplier. Receiving will update inventory automatically.</p>
      <form id="po-form">
        <div class="form-grid">
          <div class="field"><label>Supplier</label><select name="supplier_id" id="po-supplier" required></select></div>
          <div class="field"><label>Expected date</label><input name="expected_date" type="date" /></div>
          <div class="field full"><label>Notes</label><textarea name="notes"></textarea></div>
        </div>
        <div class="panel-header" style="margin-top:16px">
          <h2 style="font-size:1rem">Lines</h2>
          <button type="button" class="btn ghost" id="add-po-line">Add line</button>
        </div>
        <div class="lines-editor" id="po-lines"></div>
        <div class="modal-actions">
          <button type="button" class="btn secondary" data-close="po-modal">Cancel</button>
          <button class="btn" type="submit">Place order</button>
        </div>
      </form>
    </div></div>
    <div class="modal-backdrop" id="receive-modal"><div class="modal">
      <h2>Receive purchase order</h2>
      <form id="receive-form">
        <input type="hidden" name="po_id" />
        <div class="lines-editor" id="receive-lines"></div>
        <div class="modal-actions">
          <button type="button" class="btn secondary" data-close="receive-modal">Cancel</button>
          <button class="btn" type="submit">Receive stock</button>
        </div>
      </form>
    </div></div>`;

  bindModalClosers(root);
  let products = [];
  let suppliers = [];

  function poLineRow(selected = "", qty = 1, cost = "") {
    return `<div class="line-row">
      <select name="product_id" required><option value="">Product</option>
      ${products.map((p) => `<option value="${p.id}" ${String(p.id) === String(selected) ? "selected" : ""}>${p.sku} · ${p.name}</option>`).join("")}
      </select>
      <input name="quantity_ordered" type="number" step="0.01" min="0.01" value="${qty}" required />
      <input name="unit_cost" type="number" step="0.01" value="${cost}" placeholder="Unit cost" />
      <button type="button" class="btn secondary remove-line">✕</button>
    </div>`;
  }

  async function loadPOs() {
    const orders = await api("/api/purchase-orders");
    const host = document.getElementById("po-table");
    if (!orders.length) {
      host.innerHTML = `<div class="empty">No purchase orders yet.</div>`;
      return;
    }
    host.innerHTML = `<table><thead><tr><th>PO</th><th>Supplier</th><th>Status</th><th>Expected</th><th>Total</th><th>Lines</th><th></th></tr></thead>
      <tbody>${orders.map((po) => `<tr>
        <td class="mono">${po.po_number}</td><td>${po.supplier?.name || "—"}</td>
        <td>${badgeForStatus(po.status)}</td><td>${po.expected_date || "—"}</td>
        <td>${money(po.total)}</td>
        <td>${po.lines.map((l) => `<span class="mono">${l.product?.sku || l.product_id}</span>: ${l.quantity_received}/${l.quantity_ordered}`).join("<br>")}</td>
        <td>${["ordered", "partial", "draft"].includes(po.status)
          ? `<button class="btn ghost sm" data-receive='${JSON.stringify(po).replace(/'/g, "&#39;")}'>Receive</button>`
          : ""}</td>
      </tr>`).join("")}</tbody></table>`;

    host.querySelectorAll("[data-receive]").forEach((btn) => {
      btn.onclick = () => {
        const po = JSON.parse(btn.getAttribute("data-receive"));
        document.querySelector('#receive-form [name="po_id"]').value = po.id;
        document.getElementById("receive-lines").innerHTML = po.lines.map((line) => {
          const remaining = Number(line.quantity_ordered) - Number(line.quantity_received);
          return `<div class="line-row" style="grid-template-columns:2fr 1fr">
            <div>${line.product?.name || line.product_id}<div class="cell-sub">Remaining ${remaining}</div></div>
            <input type="number" step="0.01" min="0.01" max="${remaining}" value="${remaining > 0 ? remaining : 0}" data-line-id="${line.id}" ${remaining <= 0 ? "disabled" : ""} />
          </div>`;
        }).join("");
        openModal("receive-modal");
      };
    });
  }

  [products, suppliers] = await Promise.all([api("/api/products"), api("/api/suppliers")]);
  document.getElementById("po-supplier").innerHTML = suppliers.map((s) => `<option value="${s.id}">${s.name}</option>`).join("");
  await loadPOs();

  document.getElementById("open-po-modal").onclick = () => {
    document.getElementById("po-lines").innerHTML = poLineRow();
    openModal("po-modal");
  };
  document.getElementById("add-po-line").onclick = () => {
    document.getElementById("po-lines").insertAdjacentHTML("beforeend", poLineRow());
  };
  document.getElementById("po-lines").onclick = (e) => {
    if (e.target.classList.contains("remove-line")) e.target.closest(".line-row").remove();
  };
  document.getElementById("po-form").onsubmit = async (e) => {
    e.preventDefault();
    const fd = new FormData(e.target);
    const rows = [...document.querySelectorAll("#po-lines .line-row")];
    const payload = {
      supplier_id: Number(fd.get("supplier_id")),
      expected_date: fd.get("expected_date") || null,
      notes: fd.get("notes") || null,
      place_order: true,
      lines: rows.map((row) => ({
        product_id: Number(row.querySelector('[name="product_id"]').value),
        quantity_ordered: Number(row.querySelector('[name="quantity_ordered"]').value),
        unit_cost: row.querySelector('[name="unit_cost"]').value
          ? Number(row.querySelector('[name="unit_cost"]').value)
          : null,
      })),
    };
    try {
      await api("/api/purchase-orders", { method: "POST", body: JSON.stringify(payload) });
      closeModal("po-modal");
      toast("Purchase order created");
      await loadPOs();
    } catch (err) {
      toast(err.message, true);
    }
  };
  document.getElementById("receive-form").onsubmit = async (e) => {
    e.preventDefault();
    const poId = document.querySelector('#receive-form [name="po_id"]').value;
    const lines = [...document.querySelectorAll("#receive-lines input[data-line-id]")]
      .filter((input) => !input.disabled && Number(input.value) > 0)
      .map((input) => ({ line_id: Number(input.dataset.lineId), quantity_received: Number(input.value) }));
    try {
      await api(`/api/purchase-orders/${poId}/receive`, { method: "POST", body: JSON.stringify({ lines }) });
      closeModal("receive-modal");
      toast("Stock received");
      await loadPOs();
    } catch (err) {
      toast(err.message, true);
    }
  };
  document.getElementById("load-suggestions").onclick = async () => {
    try {
      const data = await api("/api/purchase-orders/reorder-suggestions");
      const panel = document.getElementById("suggestions-panel");
      const host = document.getElementById("suggestions");
      panel.hidden = false;
      const entries = Object.entries(data.suggestions || {});
      if (!entries.length) {
        host.innerHTML = `<div class="empty">No supplier-linked low-stock items to suggest.</div>`;
        return;
      }
      host.innerHTML = entries.map(([supplierId, lines]) => {
        const supplier = suppliers.find((s) => String(s.id) === String(supplierId));
        return `<div style="margin-bottom:14px;padding-bottom:14px;border-bottom:1px solid var(--line)">
          <div class="panel-header"><strong>${supplier?.name || "Supplier " + supplierId}</strong>
          <button class="btn ghost" data-suggest='${JSON.stringify({ supplierId, lines }).replace(/'/g, "&#39;")}'>Create PO</button></div>
          <table><thead><tr><th>SKU</th><th>Product</th><th>On hand</th><th>Suggested buy</th></tr></thead>
          <tbody>${lines.map((l) => `<tr><td class="mono">${l.sku}</td><td>${l.name}</td><td>${l.quantity_on_hand}</td><td>${l.quantity_ordered}</td></tr>`).join("")}</tbody></table>
        </div>`;
      }).join("");
      host.querySelectorAll("[data-suggest]").forEach((btn) => {
        btn.onclick = () => {
          const { supplierId, lines } = JSON.parse(btn.getAttribute("data-suggest"));
          document.getElementById("po-supplier").value = supplierId;
          document.getElementById("po-lines").innerHTML = lines
            .map((l) => poLineRow(l.product_id, l.quantity_ordered, l.unit_cost))
            .join("");
          openModal("po-modal");
        };
      });
    } catch (err) {
      toast(err.message, true);
    }
  };
}

export async function renderSalesOrders(root) {
  root.innerHTML = `
    <div class="topbar">
      <div><p class="eyebrow">Operations</p><h1>Sales</h1>
      <p>Fulfill customer orders and automatically reduce inventory quantities.</p></div>
      <div class="actions"><button class="btn" id="open-so-modal">New sales order</button></div>
    </div>
    <section class="panel"><div class="table-wrap" id="so-table"></div></section>
    <div class="modal-backdrop" id="so-modal"><div class="modal">
      <h2>New sales order</h2>
      <p class="modal-lead">Confirm and fulfill an order. Stock is deducted when the order is fulfilled.</p>
      <form id="so-form">
        <div class="form-grid">
          <div class="field"><label>Customer</label><select name="customer_id" id="so-customer" required></select></div>
          <div class="field full"><label>Notes</label><textarea name="notes"></textarea></div>
        </div>
        <div class="panel-header" style="margin-top:16px">
          <h2 style="font-size:1rem">Lines</h2>
          <button type="button" class="btn ghost" id="add-so-line">Add line</button>
        </div>
        <div class="lines-editor" id="so-lines"></div>
        <div class="modal-actions">
          <button type="button" class="btn secondary" data-close="so-modal">Cancel</button>
          <button class="btn" type="submit">Fulfill order</button>
        </div>
      </form>
    </div></div>`;

  bindModalClosers(root);
  let products = [];
  let customers = [];

  function soLineRow() {
    return `<div class="line-row">
      <select name="product_id" required><option value="">Product</option>
      ${products.map((p) => `<option value="${p.id}">${p.sku} · ${p.name} (${p.quantity_on_hand} on hand)</option>`).join("")}
      </select>
      <input name="quantity" type="number" step="0.01" min="0.01" value="1" required />
      <input name="unit_price" type="number" step="0.01" placeholder="Price" />
      <button type="button" class="btn secondary remove-line">✕</button>
    </div>`;
  }

  async function loadSOs() {
    const orders = await api("/api/sales-orders");
    const host = document.getElementById("so-table");
    host.innerHTML = orders.length
      ? `<table><thead><tr><th>Order</th><th>Customer</th><th>Status</th><th>Date</th><th>Total</th><th>Lines</th></tr></thead>
         <tbody>${orders.map((o) => `<tr>
           <td class="mono">${o.order_number}</td><td>${o.customer?.name || "—"}</td>
           <td>${badgeForStatus(o.status)}</td><td>${o.order_date}</td><td>${money(o.total)}</td>
           <td>${o.lines.map((l) => `<span class="mono">${l.product?.sku || l.product_id}</span> × ${l.quantity}`).join("<br>")}</td>
         </tr>`).join("")}</tbody></table>`
      : `<div class="empty">No sales orders yet.</div>`;
  }

  [products, customers] = await Promise.all([api("/api/products"), api("/api/customers")]);
  document.getElementById("so-customer").innerHTML = customers.map((c) => `<option value="${c.id}">${c.name}</option>`).join("");
  await loadSOs();

  document.getElementById("open-so-modal").onclick = () => {
    document.getElementById("so-lines").innerHTML = soLineRow();
    openModal("so-modal");
  };
  document.getElementById("add-so-line").onclick = () => {
    document.getElementById("so-lines").insertAdjacentHTML("beforeend", soLineRow());
  };
  document.getElementById("so-lines").onclick = (e) => {
    if (e.target.classList.contains("remove-line")) e.target.closest(".line-row").remove();
  };
  document.getElementById("so-form").onsubmit = async (e) => {
    e.preventDefault();
    const fd = new FormData(e.target);
    const rows = [...document.querySelectorAll("#so-lines .line-row")];
    const payload = {
      customer_id: Number(fd.get("customer_id")),
      notes: fd.get("notes") || null,
      fulfill: true,
      lines: rows.map((row) => ({
        product_id: Number(row.querySelector('[name="product_id"]').value),
        quantity: Number(row.querySelector('[name="quantity"]').value),
        unit_price: row.querySelector('[name="unit_price"]').value
          ? Number(row.querySelector('[name="unit_price"]').value)
          : null,
      })),
    };
    try {
      await api("/api/sales-orders", { method: "POST", body: JSON.stringify(payload) });
      closeModal("so-modal");
      toast("Sales order fulfilled");
      products = await api("/api/products");
      await loadSOs();
    } catch (err) {
      toast(err.message, true);
    }
  };
}

async function renderDirectory(root, kind) {
  const isSupplier = kind === "suppliers";
  const title = isSupplier ? "Suppliers" : "Customers";
  const endpoint = isSupplier ? "/api/suppliers" : "/api/customers";
  const lead = isSupplier
    ? "Vendor contacts used for purchase orders and reorder suggestions."
    : "Customer records for sales orders and fulfillment.";

  root.innerHTML = `
    <div class="topbar">
      <div><p class="eyebrow">Directory</p><h1>${title}</h1><p>${lead}</p></div>
      <div class="actions"><button class="btn" id="open-modal">Add ${isSupplier ? "supplier" : "customer"}</button></div>
    </div>
    <section class="panel"><div class="table-wrap" id="table"></div></section>
    <div class="modal-backdrop" id="modal"><div class="modal">
      <h2>Add ${isSupplier ? "supplier" : "customer"}</h2>
      <p class="modal-lead">${isSupplier ? "Suppliers power purchasing and automatic reorder suggestions." : "Keep buyer contacts ready for sales and fulfillment."}</p>
      <form id="form" class="form-grid">
        <div class="field"><label>Name</label><input name="name" required /></div>
        <div class="field"><label>Email</label><input name="email" type="email" /></div>
        <div class="field"><label>Phone</label><input name="phone" /></div>
        <div class="field full"><label>Address</label><textarea name="address"></textarea></div>
        <div class="field full"><label>Notes</label><textarea name="notes"></textarea></div>
        <div class="modal-actions full">
          <button type="button" class="btn secondary" data-close="modal">Cancel</button>
          <button class="btn" type="submit">Save</button>
        </div>
      </form>
    </div></div>`;

  bindModalClosers(root);

  async function load() {
    const rows = await api(endpoint);
    const host = document.getElementById("table");
    host.innerHTML = rows.length
      ? `<table><thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>Address</th></tr></thead>
         <tbody>${rows.map((r) => `<tr><td>${r.name}</td><td>${r.email || "—"}</td><td>${r.phone || "—"}</td><td>${r.address || "—"}</td></tr>`).join("")}</tbody></table>`
      : `<div class="empty">No ${title.toLowerCase()} yet.</div>`;
  }

  document.getElementById("open-modal").onclick = () => openModal("modal");
  document.getElementById("form").onsubmit = async (e) => {
    e.preventDefault();
    const payload = Object.fromEntries(new FormData(e.target).entries());
    Object.keys(payload).forEach((k) => {
      if (!payload[k]) payload[k] = null;
    });
    try {
      await api(endpoint, { method: "POST", body: JSON.stringify(payload) });
      closeModal("modal");
      e.target.reset();
      toast(`${isSupplier ? "Supplier" : "Customer"} added`);
      await load();
    } catch (err) {
      toast(err.message, true);
    }
  };
  await load();
}

export const renderSuppliers = (root) => renderDirectory(root, "suppliers");
export const renderCustomers = (root) => renderDirectory(root, "customers");

export async function renderReminders(root) {
  root.innerHTML = `
    <div class="topbar">
      <div><p class="eyebrow">Overview</p><h1>Reminders & alerts</h1>
      <p>In-app and email alerts for low stock, suggested buys, and overdue deliveries.</p></div>
      <div class="actions">
        <button class="btn secondary" id="toggle-all">Show resolved</button>
        <button class="btn" id="run-scan">Run scan now</button>
      </div>
    </div>
    <section class="panel"><div class="stack" id="list"></div></section>`;

  let unresolvedOnly = true;

  async function load() {
    const rows = await api(`/api/reminders?unresolved_only=${unresolvedOnly}`);
    const host = document.getElementById("list");
    if (!rows.length) {
      host.innerHTML = `<div class="empty">No reminders in this view.</div>`;
      return;
    }
    host.innerHTML = rows.map((r) => `
      <article class="reminder-card ${severityClass(r.severity)}">
        <div class="reminder-meta">
          <span>${r.reminder_type.replaceAll("_", " ")} · ${new Date(r.created_at).toLocaleString()}</span>
          <span class="badge ${r.severity === "critical" ? "danger" : r.severity === "warning" ? "warn" : ""}">${r.severity}</span>
        </div>
        <h3>${r.title}</h3><p>${r.message}</p>
        <div class="actions">
          ${r.email_sent ? `<span class="badge ok">Email sent</span>` : `<span class="badge warn">Email pending</span>`}
          ${r.is_resolved ? `<span class="badge ok">Resolved</span>` : `
            <button class="btn ghost" data-read="${r.id}">Mark read</button>
            <button class="btn" data-resolve="${r.id}">Resolve</button>`}
        </div>
      </article>`).join("");

    host.querySelectorAll("[data-read]").forEach((btn) => {
      btn.onclick = async () => {
        try {
          await api(`/api/reminders/${btn.dataset.read}/read`, { method: "POST" });
          await load();
        } catch (err) {
          toast(err.message, true);
        }
      };
    });
    host.querySelectorAll("[data-resolve]").forEach((btn) => {
      btn.onclick = async () => {
        try {
          await api(`/api/reminders/${btn.dataset.resolve}/resolve`, { method: "POST" });
          toast("Resolved");
          await load();
        } catch (err) {
          toast(err.message, true);
        }
      };
    });
  }

  document.getElementById("toggle-all").onclick = async () => {
    unresolvedOnly = !unresolvedOnly;
    document.getElementById("toggle-all").textContent = unresolvedOnly ? "Show resolved" : "Show open only";
    await load();
  };
  document.getElementById("run-scan").onclick = async () => {
    try {
      const result = await api("/api/reminders/run", { method: "POST" });
      toast(`Scan complete · ${result.emails_sent} email(s)`);
      await load();
    } catch (err) {
      toast(err.message, true);
    }
  };
  await load();
}

export async function renderSettings(root) {
  root.innerHTML = `
    <div class="topbar">
      <div><p class="eyebrow">System</p><h1>Settings</h1>
      <p>Configure alert email delivery and reminder preferences.</p></div>
    </div>
    <section class="panel" style="max-width:760px">
      <div class="panel-header"><h2>Alert preferences</h2></div>
      <form id="settings-form" class="form-grid">
        <div class="field"><label>Alert email to</label><input name="alert_email_to" type="email" required /></div>
        <div class="field"><label>Reminder interval (minutes)</label><input name="reminder_interval_minutes" type="number" min="1" /></div>
        <div class="field"><label>Email enabled</label>
          <select name="email_enabled"><option value="true">Yes</option><option value="false">No</option></select></div>
        <div class="field"><label>Low stock emails</label>
          <select name="low_stock_email"><option value="true">Yes</option><option value="false">No</option></select></div>
        <div class="field"><label>PO overdue / expected emails</label>
          <select name="po_overdue_email"><option value="true">Yes</option><option value="false">No</option></select></div>
        <div class="field full"><p id="smtp-note" class="settings-note"></p></div>
        <div class="actions full">
          <button class="btn" type="submit">Save settings</button>
          <button class="btn secondary" type="button" id="test-scan">Test reminder + email cycle</button>
        </div>
      </form>
    </section>`;

  const form = document.getElementById("settings-form");
  const s = await api("/api/settings");
  form.alert_email_to.value = s.alert_email_to;
  form.reminder_interval_minutes.value = s.reminder_interval_minutes;
  form.email_enabled.value = String(s.email_enabled);
  form.low_stock_email.value = String(s.low_stock_email);
  form.po_overdue_email.value = String(s.po_overdue_email);
  document.getElementById("smtp-note").textContent = s.smtp_configured
    ? "SMTP is configured via environment variables. Emails will be sent through your mail server."
    : "SMTP is not configured. Emails run in console mode on the API server. Set SMTP_* in .env for real delivery.";

  form.onsubmit = async (e) => {
    e.preventDefault();
    const fd = new FormData(form);
    try {
      await api("/api/settings", {
        method: "PUT",
        body: JSON.stringify({
          alert_email_to: fd.get("alert_email_to"),
          reminder_interval_minutes: Number(fd.get("reminder_interval_minutes")),
          email_enabled: fd.get("email_enabled") === "true",
          low_stock_email: fd.get("low_stock_email") === "true",
          po_overdue_email: fd.get("po_overdue_email") === "true",
        }),
      });
      toast("Settings saved");
    } catch (err) {
      toast(err.message, true);
    }
  };
  document.getElementById("test-scan").onclick = async () => {
    try {
      const result = await api("/api/reminders/run", { method: "POST" });
      toast(`Cycle ran · emails sent: ${result.emails_sent}`);
    } catch (err) {
      toast(err.message, true);
    }
  };
}
