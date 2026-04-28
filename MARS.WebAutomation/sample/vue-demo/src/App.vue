<template>
  <div class="app">
    <header class="hdr">
      <h1>MARS WebAutomation — Vue 3 sample</h1>
      <p>Data table, form controls, tree, tabs, menus, native dialog, pseudo dialog.</p>
    </header>

    <nav class="menubar" role="menubar" aria-label="Main menu">
      <span class="brand">Menu</span>
      <div class="menu-item" @mouseenter="fileOpen = true" @mouseleave="fileOpen = false">
        <button type="button" class="menu-btn" aria-haspopup="true" :aria-expanded="fileOpen">File</button>
        <ul v-show="fileOpen" class="popup" role="menu">
          <li role="none"><button type="button" role="menuitem" @click="fileOpen = false">New</button></li>
          <li role="none"><button type="button" role="menuitem" @click="fileOpen = false">Open…</button></li>
          <li role="none"><button type="button" role="menuitem" @click="fileOpen = false">Exit</button></li>
        </ul>
      </div>
      <div class="menu-item" @mouseenter="helpOpen = true" @mouseleave="helpOpen = false">
        <button type="button" class="menu-btn" aria-haspopup="true" :aria-expanded="helpOpen">Help</button>
        <ul v-show="helpOpen" class="popup" role="menu">
          <li role="none"><button type="button" role="menuitem" @click="helpOpen = false">About</button></li>
        </ul>
      </div>
    </nav>

    <section class="card" aria-labelledby="tabs-h">
      <h2 id="tabs-h" class="sr-only">Tabs</h2>
      <div class="tabs" role="tablist">
        <button
          v-for="(t, i) in tabLabels"
          :key="t"
          type="button"
          role="tab"
          :aria-selected="activeTab === i"
          :tabindex="activeTab === i ? 0 : -1"
          :class="{ on: activeTab === i }"
          @click="activeTab = i"
        >
          {{ t }}
        </button>
      </div>
      <div v-show="activeTab === 0" role="tabpanel" class="tab-panel">
        <h3>Orders</h3>
        <form class="tab-panel-form" @submit.prevent>
        <div class="table-wrap">
          <table class="datatable" aria-label="Sample orders">
            <thead>
              <tr>
                <th>ID</th>
                <th>Customer <span class="th-hint">(click)</span></th>
                <th>Product <span class="th-hint">(click)</span></th>
                <th>Qty <span class="th-hint">(click)</span></th>
                <th>Status</th>
                <th>Rush</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in tableRows" :key="row.id">
                <td>{{ row.id }}</td>
                <td class="cell-edit">
                  <input
                    v-if="isEditing(row, 'customer')"
                    :id="cellInputId(row, 'customer')"
                    v-model="row.customer"
                    type="text"
                    class="cell-input"
                    @blur="editing = null"
                    @keydown.enter.prevent="editing = null"
                  />
                  <span v-else class="cell-display" @click="startEdit(row, 'customer')">{{ row.customer }}</span>
                </td>
                <td class="cell-edit">
                  <input
                    v-if="isEditing(row, 'product')"
                    :id="cellInputId(row, 'product')"
                    v-model="row.product"
                    type="text"
                    class="cell-input"
                    @blur="editing = null"
                    @keydown.enter.prevent="editing = null"
                  />
                  <span v-else class="cell-display" @click="startEdit(row, 'product')">{{ row.product }}</span>
                </td>
                <td class="cell-edit">
                  <input
                    v-if="isEditing(row, 'qty')"
                    :id="cellInputId(row, 'qty')"
                    v-model.number="row.qty"
                    type="number"
                    min="0"
                    class="cell-input cell-input-narrow"
                    @blur="editing = null"
                    @keydown.enter.prevent="editing = null"
                  />
                  <span v-else class="cell-display" @click="startEdit(row, 'qty')">{{ row.qty }}</span>
                </td>
                <td>
                  <select v-model="row.status" class="cell-select" :aria-label="'Status for order ' + row.id">
                    <option v-for="s in statusOptions" :key="s" :value="s">{{ s }}</option>
                  </select>
                </td>
                <td class="cell-center">
                  <input v-model="row.rush" type="checkbox" :aria-label="'Rush order ' + row.id" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        </form>
      </div>
      <div v-show="activeTab === 1" role="tabpanel" class="tab-panel">
        <h3>Regions (tree)</h3>
        <form class="tab-panel-form" aria-label="Tree panel" @submit.prevent>
        <ul class="tree" role="tree" aria-label="Regions">
          <li v-for="n in tree" :key="n.label" role="treeitem" :aria-expanded="n.open" class="tree-node">
            <button type="button" class="twisty" @click="n.open = !n.open">{{ n.open ? '▼' : '▶' }}</button>
            {{ n.label }}
            <ul v-show="n.open" role="group">
              <li v-for="c in n.children" :key="c" role="treeitem">{{ c }}</li>
            </ul>
          </li>
        </ul>
        </form>
      </div>
      <div v-show="activeTab === 2" role="tabpanel" class="tab-panel">
        <h3>Notes</h3>
        <form class="tab-panel-form" @submit.prevent>
        <textarea v-model="notes" name="tabNotes" rows="5" class="full" placeholder="Multi-line notes…" aria-label="Notes"></textarea>
        </form>
      </div>
      <div v-show="activeTab === 3" role="tabpanel" class="tab-panel">
        <h3>PQ Grid (ParamQuery / pqgridf)</h3>
        <p class="hint">GPL ParamQuery grid — inline editable cells (double-click or F2 depending on theme).</p>
        <form class="tab-panel-form" @submit.prevent>
        <div ref="pqGridHost" class="pq-host"></div>
        </form>
      </div>
      <div v-show="activeTab === 4" role="tabpanel" class="tab-panel">
        <h3>Div-based “datagrid”</h3>
        <p class="hint">CSS grid + div rows; click text cells to edit.</p>
        <form class="tab-panel-form" @submit.prevent>
        <div class="faux-grid" role="grid" aria-label="Inventory (div grid)">
          <div class="faux-head" role="row">
            <div role="columnheader">Code</div>
            <div role="columnheader">Name (click)</div>
            <div role="columnheader">Units</div>
            <div role="columnheader">OK</div>
          </div>
          <div v-for="r in divGridRows" :key="r.id" class="faux-row" role="row">
            <div class="faux-cell" role="gridcell">{{ r.code }}</div>
            <div class="faux-cell faux-edit" role="gridcell">
              <input
                v-if="divEdit?.id === r.id && divEdit?.field === 'name'"
                v-model="r.name"
                class="cell-input"
                type="text"
                @blur="divEdit = null"
                @keydown.enter.prevent="divEdit = null"
              />
              <span v-else class="cell-display" @click="divEdit = { id: r.id, field: 'name' }">{{ r.name }}</span>
            </div>
            <div class="faux-cell faux-edit" role="gridcell">
              <input
                v-if="divEdit?.id === r.id && divEdit?.field === 'units'"
                v-model.number="r.units"
                class="cell-input cell-input-narrow"
                type="number"
                min="0"
                @blur="divEdit = null"
                @keydown.enter.prevent="divEdit = null"
              />
              <span v-else class="cell-display" @click="divEdit = { id: r.id, field: 'units' }">{{ r.units }}</span>
            </div>
            <div class="faux-cell faux-center" role="gridcell">
              <input v-model="r.ok" type="checkbox" :aria-label="'OK ' + r.code" />
            </div>
          </div>
        </div>
        </form>
      </div>
    </section>

    <section class="card">
      <h2>Form controls</h2>
      <form class="grid-form" @submit.prevent>
        <label>Text field <input id="text-field" v-model="textField" type="text" name="username" autocomplete="username" /></label>
        <label>File <input type="file" name="doc" accept=".txt,.pdf,.png" /></label>
        <label class="row">Textarea <textarea v-model="notes" rows="3"></textarea></label>
        <label>Select (native)
          <select id="country-select" name="country" v-model="country">
            <option value="">— choose —</option>
            <option value="us">United States</option>
            <option value="cn">China</option>
            <option value="de">Germany</option>
          </select>
        </label>
        <div>
          <span id="vue-fake-lbl">Select (div-rendered)</span>
          <div class="fake-select" role="group" aria-labelledby="vue-fake-lbl">
            <button type="button" class="fake-select-btn" @click="fakeOpen = !fakeOpen">{{ fakeLabel }}</button>
            <ul v-show="fakeOpen" class="fake-select-list" role="listbox">
              <li v-for="o in fakeOptions" :key="o.v" role="option" @click="pickFake(o)">{{ o.t }}</li>
            </ul>
          </div>
        </div>
        <fieldset>
          <legend>Checkbox group</legend>
          <label><input v-model="optEmail" type="checkbox" name="notify" /> Email alerts</label>
          <label><input v-model="optSms" type="checkbox" name="sms" /> SMS alerts</label>
        </fieldset>
        <fieldset>
          <legend>Radio group</legend>
          <label><input v-model="tier" type="radio" name="tier" value="basic" /> Basic</label>
          <label><input v-model="tier" type="radio" name="tier" value="pro" /> Pro</label>
          <label><input v-model="tier" type="radio" name="tier" value="ent" /> Enterprise</label>
        </fieldset>
        <div class="btn-row">
          <button type="button" class="primary" @click="dialogRef?.showModal()">Open native &lt;dialog&gt;</button>
          <button type="button" @click="pseudoOpen = true">Open pseudo dialog (div)</button>
          <button type="submit">Submit (demo)</button>
          <input type="image" class="img-btn" src="https://www.w3.org/assets/logos/w3c/w3c-no-bars.svg" alt="W3C image button" width="72" height="24" title="Image submit" />
        </div>
      </form>
    </section>

    <section class="card" aria-labelledby="pw-sim-title">
      <h2 id="pw-sim-title">Playwright simulation</h2>
      <form class="tab-panel-form" @submit.prevent>
      <div class="btn-row">
        <label><input v-model="pwSimPseudo" type="checkbox" name="pwSimPseudo" /> Use pseudo dialog (div)</label>
        <button type="button" class="primary" @click="openPwSim">Open Playwright simulation dialog</button>
      </div>
      </form>
    </section>

    <section class="card" aria-labelledby="cross-url-title">
      <h2 id="cross-url-title">Cross-URL / iframe duplicate controls</h2>
      <p class="hint">The popup page and iframes expose the same name/id on text and select as this page (for Playwright disambiguation tests).</p>
      <form class="tab-panel-form" @submit.prevent>
      <div class="btn-row">
        <button type="button" class="primary" @click="openMirrorWindow">Open mirror page (new window)</button>
      </div>
      </form>
      <div class="iframe-grid">
        <iframe title="same-obj-frame-1" src="/mirror-controls.html?slot=1"></iframe>
        <iframe title="same-obj-frame-2" src="/mirror-controls.html?slot=2"></iframe>
      </div>
    </section>

    <section class="card" aria-labelledby="loc-lab-title">
      <h2 id="loc-lab-title">Locator lab (duplicate &amp; dynamic ids)</h2>
      <p class="hint">Reused template ids across rows/cells. Locator generation should compound aria-label / data-col / data-row-index instead of bare id.</p>
      <form class="tab-panel-form" @submit.prevent>
        <table class="datatable" aria-label="Dynamic id grid">
          <thead>
            <tr><th scope="col">Row</th><th scope="col">A</th><th scope="col">B</th></tr>
          </thead>
          <tbody>
            <tr id="rowId" data-row-index="0">
              <td><input id="cellId" name="dynField" type="text" data-col="A" aria-label="Row 0 column A" placeholder="r0a" /></td>
              <td><input id="cellId" name="dynField" type="text" data-col="B" aria-label="Row 0 column B" placeholder="r0b" /></td>
            </tr>
            <tr id="rowId" data-row-index="1">
              <td><input id="cellId" name="dynField" type="text" data-col="A" aria-label="Row 1 column A" placeholder="r1a" /></td>
              <td><input id="cellId" name="dynField" type="text" data-col="B" aria-label="Row 1 column B" placeholder="r1b" /></td>
            </tr>
          </tbody>
        </table>
        <p class="hint">Stable reference (unique id + data-testid):</p>
        <label>Stable field <input type="text" id="stable-lab-field" name="stableLab" data-testid="stable-lab-field" aria-label="Stable lab field" /></label>
        <p class="hint">Algorithmic id (treated as volatile):</p>
        <label>Volatile field <input type="text" id="row-92817" name="volatileRow" aria-label="Volatile row field" /></label>
      </form>
    </section>

    <dialog ref="dialogRef" class="dlg" aria-labelledby="dlg-title">
      <h2 id="dlg-title">Native dialog</h2>
      <p>This is a real <code>&lt;dialog&gt;</code> opened with <code>showModal()</code>.</p>
      <button type="button" @click="dialogRef?.close()">Close</button>
    </dialog>

    <dialog ref="pwSimDialogRef" class="dlg" aria-labelledby="pw-sim-native-title">
      <h2 id="pw-sim-native-title">Playwright simulation (native dialog)</h2>
      <p>Mode: native <code>&lt;dialog&gt;</code>.</p>
      <button type="button" @click="pwSimDialogRef?.close()">Close</button>
    </dialog>

    <div v-show="pseudoOpen" class="pseudo-overlay" role="presentation" @click.self="pseudoOpen = false">
      <div class="pseudo-box" role="dialog" aria-modal="true" aria-labelledby="pseudo-title">
        <h2 id="pseudo-title">Pseudo dialog</h2>
        <p>Overlay + <code>div</code> (not <code>&lt;dialog&gt;</code>).</p>
        <button type="button" @click="pseudoOpen = false">Close</button>
      </div>
    </div>

    <div v-show="pwSimOpen" class="pseudo-overlay" role="presentation" @click.self="pwSimOpen = false">
      <div class="pseudo-box" role="dialog" aria-modal="true" aria-labelledby="pw-sim-pseudo-title">
        <h2 id="pw-sim-pseudo-title">Playwright simulation (pseudo dialog)</h2>
        <p>Mode: div overlay (pseudo dialog).</p>
        <button type="button" @click="pwSimOpen = false">Close</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick, watch, onBeforeUnmount } from 'vue';

const dialogRef = ref(null);
const pwSimDialogRef = ref(null);
const pseudoOpen = ref(false);
const pwSimOpen = ref(false);
const pwSimPseudo = ref(false);
const activeTab = ref(0);
const tabLabels = ['Table', 'Tree', 'Textarea', 'PQ Grid', 'Div grid'];
const pqGridHost = ref(null);
let pqGridEl = null;
const divEdit = ref(null);
const textField = ref('');
const notes = ref('');
const country = ref('');
const fakeOpen = ref(false);
const fakeLabel = ref('— choose —');
const fakeOptions = [
  { v: 'us', t: 'United States' },
  { v: 'cn', t: 'China' },
  { v: 'de', t: 'Germany' },
];
function pickFake(o) {
  fakeLabel.value = o.t;
  fakeOpen.value = false;
}
const optEmail = ref(true);
const optSms = ref(false);
const tier = ref('pro');
const fileOpen = ref(false);
const helpOpen = ref(false);

function openPwSim() {
  if (pwSimPseudo.value) {
    pwSimOpen.value = true;
    return;
  }
  pwSimDialogRef.value?.showModal();
}

function openMirrorWindow() {
  window.open('/mirror-window.html', 'mars_mirror_window', 'width=920,height=700,resizable=yes,scrollbars=yes');
}

const statusOptions = ['Shipped', 'Pending', 'Processing', 'Cancelled'];
const editing = ref(null);
function isEditing(row, field) {
  return editing.value?.id === row.id && editing.value?.field === field;
}
function cellInputId(row, field) {
  return `cell-${row.id}-${field}`;
}
function startEdit(row, field) {
  editing.value = { id: row.id, field };
  nextTick(() => {
    const el = document.getElementById(cellInputId(row, field));
    el?.focus();
    el?.select?.();
  });
}

const tableRows = ref([
  { id: 1001, customer: 'Acme', product: 'Sensor A', qty: 4, status: 'Shipped', rush: false },
  { id: 1002, customer: 'Globex', product: 'Cable kit', qty: 12, status: 'Pending', rush: true },
  { id: 1003, customer: 'Initech', product: 'Bracket', qty: 50, status: 'Shipped', rush: false },
  { id: 1004, customer: 'Umbrella', product: 'Mount plate', qty: 7, status: 'Cancelled', rush: false },
  { id: 1005, customer: 'Stark', product: 'Power module', qty: 2, status: 'Processing', rush: true },
]);

const tree = ref([
  { label: 'North America', open: true, children: ['USA', 'Canada', 'Mexico'] },
  { label: 'Europe', open: false, children: ['DE', 'FR', 'UK'] },
  { label: 'Asia', open: false, children: ['CN', 'JP', 'KR'] },
]);

const divGridRows = ref([
  { id: 1, code: 'X-10', name: 'Alpha lot', units: 40, ok: true },
  { id: 2, code: 'X-11', name: 'Beta lot', units: 7, ok: false },
  { id: 3, code: 'X-12', name: 'Gamma lot', units: 120, ok: true },
]);

const pqRows = [
  { sku: 'PQ-01', label: 'Resistor 10k', qty: 500, bin: 'A1' },
  { sku: 'PQ-02', label: 'Cap 100nF', qty: 1200, bin: 'B2' },
  { sku: 'PQ-03', label: 'LED red', qty: 80, bin: 'C0' },
];

function tryInitPqGrid() {
  const host = pqGridHost.value;
  const $ = window.jQuery || window.$;
  if (!host || !$ || !$.fn || !$.fn.pqGrid || pqGridEl) return;
  const w = Math.max(320, host.clientWidth || 640);
  const localRows = pqRows.map((r) => ({ ...r }));
  pqGridEl = $(host);
  pqGridEl.pqGrid({
    width: w,
    height: 260,
    editable: true,
    dataModel: { location: 'local', data: localRows },
    colModel: [
      { title: 'SKU', dataIndx: 'sku', width: 90, editable: true },
      { title: 'Label', dataIndx: 'label', width: 200, editable: true },
      { title: 'Qty', dataIndx: 'qty', width: 70, dataType: 'integer', editable: true },
      { title: 'Bin', dataIndx: 'bin', width: 70, editable: true },
    ],
  });
}

watch(activeTab, async (i) => {
  if (i !== 3) {
    if (pqGridEl && window.jQuery) {
      try {
        pqGridEl.pqGrid('destroy');
      } catch {
        /* ignore */
      }
      pqGridEl = null;
    }
    return;
  }
  await nextTick();
  requestAnimationFrame(() => {
    tryInitPqGrid();
    if (pqGridEl) {
      try {
        pqGridEl.pqGrid('refreshDataAndView');
      } catch {
        /* ignore */
      }
    }
  });
});

onBeforeUnmount(() => {
  if (pqGridEl && window.jQuery) {
    try {
      pqGridEl.pqGrid('destroy');
    } catch {
      /* ignore */
    }
    pqGridEl = null;
  }
});
</script>

<style>
.app { font-family: system-ui, sans-serif; max-width: 960px; margin: 0 auto; padding: 1rem; color: #0f172a; }
.hdr h1 { font-size: 1.35rem; margin: 0 0 0.25rem; }
.hdr p { margin: 0 0 1rem; color: #64748b; font-size: 0.9rem; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0,0,0,0); border: 0; }
.menubar { display: flex; align-items: center; gap: 0.5rem; background: #1e293b; color: #fff; padding: 0.35rem 0.75rem; border-radius: 6px; margin-bottom: 1rem; position: relative; z-index: 5; }
.menubar .brand { font-weight: 600; margin-right: 0.5rem; }
.menu-item { position: relative; }
.menu-btn { background: transparent; color: inherit; border: none; padding: 0.4rem 0.75rem; cursor: pointer; border-radius: 4px; }
.menu-btn:hover { background: #334155; }
.popup { position: absolute; top: 100%; left: 0; min-width: 160px; background: #fff; color: #0f172a; border: 1px solid #e2e8f0; border-radius: 6px; box-shadow: 0 8px 24px rgba(0,0,0,.12); list-style: none; margin: 0; padding: 0.25rem; z-index: 10; }
.popup button { width: 100%; text-align: left; border: none; background: none; padding: 0.4rem 0.5rem; cursor: pointer; border-radius: 4px; }
.popup button:hover { background: #f1f5f9; }
.card { border: 1px solid #e2e8f0; border-radius: 8px; padding: 1rem; margin-bottom: 1rem; background: #fff; }
.card h2, .card h3 { margin-top: 0; font-size: 1.05rem; }
.tabs { display: flex; gap: 4px; border-bottom: 1px solid #e2e8f0; margin-bottom: 0.75rem; }
.tabs button { border: none; background: #f8fafc; padding: 0.5rem 1rem; cursor: pointer; border-radius: 6px 6px 0 0; }
.tabs button.on { background: #3b82f6; color: #fff; }
.tab-panel { min-height: 120px; }
.table-wrap { overflow: auto; }
.datatable { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
.datatable th, .datatable td { border: 1px solid #cbd5e1; padding: 0.45rem 0.6rem; text-align: left; }
.datatable thead { background: #f1f5f9; }
.th-hint { font-weight: 400; color: #64748b; font-size: 0.75rem; }
.cell-display { cursor: text; display: inline-block; min-width: 4rem; padding: 2px 6px; border-radius: 4px; }
.cell-display:hover { background: #e0f2fe; outline: 1px solid #7dd3fc; }
.cell-input { width: 100%; min-width: 6rem; box-sizing: border-box; padding: 0.25rem 0.35rem; border: 1px solid #2563eb; border-radius: 4px; }
.cell-input-narrow { min-width: 3.5rem; max-width: 5rem; }
.cell-select { min-width: 8.5rem; padding: 0.3rem 0.4rem; border-radius: 4px; border: 1px solid #cbd5e1; }
.cell-center { text-align: center; vertical-align: middle; }
.tree { list-style: none; padding-left: 0; margin: 0; }
.tree .tree-node { margin: 0.25rem 0; }
.twisty { border: none; background: #e2e8f0; width: 1.5rem; cursor: pointer; border-radius: 4px; margin-right: 0.25rem; }
.tree ul { list-style: none; padding-left: 1.5rem; margin: 0.25rem 0; }
.grid-form { display: grid; gap: 0.75rem; max-width: 640px; }
.grid-form label { display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.85rem; }
.grid-form input[type="text"], .grid-form select, .grid-form textarea { padding: 0.4rem; border: 1px solid #cbd5e1; border-radius: 4px; }
.grid-form .row { grid-column: 1 / -1; }
.full { width: 100%; box-sizing: border-box; }
fieldset { border: 1px solid #e2e8f0; border-radius: 6px; }
.btn-row { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
.primary { background: #2563eb; color: #fff; border: none; padding: 0.5rem 0.85rem; border-radius: 6px; cursor: pointer; }
.img-btn { border: 1px solid #cbd5e1; border-radius: 4px; cursor: pointer; padding: 2px; background: #fff; }
.fake-select { position: relative; max-width: 280px; }
.fake-select-btn { width: 100%; text-align: left; padding: 0.45rem; border: 1px solid #cbd5e1; border-radius: 4px; background: #fff; cursor: pointer; }
.fake-select-list { position: absolute; left: 0; right: 0; top: 100%; margin: 2px 0 0; padding: 0.25rem; list-style: none; background: #fff; border: 1px solid #cbd5e1; border-radius: 4px; z-index: 3; box-shadow: 0 4px 12px rgba(0,0,0,.08); }
.fake-select-list li { padding: 0.35rem 0.5rem; cursor: pointer; border-radius: 4px; }
.fake-select-list li:hover { background: #f1f5f9; }
.dlg { border: none; border-radius: 8px; padding: 1rem; max-width: 400px; box-shadow: 0 20px 50px rgba(0,0,0,.25); }
.dlg::backdrop { background: rgba(15,23,42,.45); }
.pseudo-overlay { position: fixed; inset: 0; background: rgba(15,23,42,.45); display: flex; align-items: center; justify-content: center; z-index: 100; }
.pseudo-box { background: #fff; padding: 1.25rem; border-radius: 8px; min-width: 280px; box-shadow: 0 20px 50px rgba(0,0,0,.2); }
.hint { font-size: 0.8rem; color: #64748b; margin: 0 0 0.5rem; }
.tab-panel-form { margin: 0; }
.iframe-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; margin-top: 0.75rem; }
.iframe-grid iframe { width: 100%; min-height: 240px; border: 1px solid #cbd5e1; border-radius: 6px; background: #fff; }
.pq-host { width: 100%; min-height: 268px; }
.faux-grid { display: flex; flex-direction: column; border: 1px solid #cbd5e1; border-radius: 6px; overflow: hidden; max-width: 720px; }
.faux-head, .faux-row { display: grid; grid-template-columns: 72px 1fr 88px 64px; align-items: center; }
.faux-head { background: #f1f5f9; font-weight: 600; font-size: 0.8rem; }
.faux-head > div, .faux-cell { padding: 0.4rem 0.55rem; border-bottom: 1px solid #e2e8f0; }
.faux-row:last-child .faux-cell { border-bottom: none; }
.faux-center { text-align: center; }
.faux-edit .cell-display { min-width: 2rem; }
</style>
