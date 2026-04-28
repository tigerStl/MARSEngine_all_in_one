import React, { useEffect, useRef, useState } from 'react';

const statusOptions = ['Shipped', 'Pending', 'Processing', 'Cancelled'];

const initialRows = [
  { id: 2001, customer: 'Acme', product: 'Sensor A', qty: 4, status: 'Shipped', rush: false },
  { id: 2002, customer: 'Globex', product: 'Cable kit', qty: 12, status: 'Pending', rush: true },
  { id: 2003, customer: 'Initech', product: 'Bracket', qty: 50, status: 'Shipped', rush: false },
  { id: 2004, customer: 'Umbrella', product: 'Mount plate', qty: 7, status: 'Cancelled', rush: false },
  { id: 2005, customer: 'Stark', product: 'Power module', qty: 2, status: 'Processing', rush: true },
];

const initialTree = [
  { label: 'North America', open: true, children: ['USA', 'Canada', 'Mexico'] },
  { label: 'Europe', open: false, children: ['DE', 'FR', 'UK'] },
  { label: 'Asia', open: false, children: ['CN', 'JP', 'KR'] },
];

const tabLabels = ['Table', 'Tree', 'Textarea', 'PQ Grid', 'Div grid'];

const pqInventory = [
  { sku: 'R-01', label: 'Bolt M6', qty: 44, bin: 'S1' },
  { sku: 'R-02', label: 'Washer', qty: 200, bin: 'S2' },
  { sku: 'R-03', label: 'Rivet', qty: 35, bin: 'T0' },
];

export default function App() {
  const dialogRef = useRef(null);
  const pwSimDialogRef = useRef(null);
  const [pseudoOpen, setPseudoOpen] = useState(false);
  const [activeTab, setActiveTab] = useState(0);
  const [textField, setTextField] = useState('');
  const [notes, setNotes] = useState('');
  const [country, setCountry] = useState('');
  const [optEmail, setOptEmail] = useState(true);
  const [optSms, setOptSms] = useState(false);
  const [tier, setTier] = useState('pro');
  const [fileOpen, setFileOpen] = useState(false);
  const [helpOpen, setHelpOpen] = useState(false);
  const [pwSimPseudo, setPwSimPseudo] = useState(false);
  const [pwSimOpen, setPwSimOpen] = useState(false);
  const [fakeOpen, setFakeOpen] = useState(false);
  const [fakeLabel, setFakeLabel] = useState('— choose —');
  const [tree, setTree] = useState(initialTree);
  const [tableRows, setTableRows] = useState(initialRows);
  const [editing, setEditing] = useState(null);
  const pqHostRef = useRef(null);
  const pqJqRef = useRef(null);
  const [divGridRows, setDivGridRows] = useState([
    { id: 1, code: 'D-aa', name: 'Foam sheet', units: 12, ok: true },
    { id: 2, code: 'D-bb', name: 'Tape roll', units: 3, ok: false },
    { id: 3, code: 'D-cc', name: 'Label roll', units: 90, ok: true },
  ]);
  const [divEdit, setDivEdit] = useState(null);

  useEffect(() => {
    if (activeTab !== 3) return undefined;
    let cancelled = false;
    const id = window.requestAnimationFrame(() => {
      if (cancelled) return;
      const el = pqHostRef.current;
      const $ = window.jQuery;
      if (!el || !$?.fn?.pqGrid || pqJqRef.current) return;
      const w = Math.max(320, el.clientWidth || 640);
      pqJqRef.current = $(el).pqGrid({
        width: w,
        height: 260,
        editable: true,
        dataModel: { location: 'local', data: pqInventory },
        colModel: [
          { title: 'SKU', dataIndx: 'sku', width: 90, editable: true },
          { title: 'Label', dataIndx: 'label', width: 200, editable: true },
          { title: 'Qty', dataIndx: 'qty', width: 70, dataType: 'integer', editable: true },
          { title: 'Bin', dataIndx: 'bin', width: 70, editable: true },
        ],
      });
    });
    return () => {
      cancelled = true;
      window.cancelAnimationFrame(id);
      const g = pqJqRef.current;
      pqJqRef.current = null;
      if (g) {
        try {
          g.pqGrid('destroy');
        } catch {
          /* ignore */
        }
      }
    };
  }, [activeTab]);

  useEffect(() => {
    if (!editing) return;
    const el = document.getElementById(`cell-${editing.id}-${editing.field}`);
    if (el && 'focus' in el) {
      el.focus();
      if ('select' in el && typeof el.select === 'function') el.select();
    }
  }, [editing]);

  const updateRow = (id, patch) => {
    setTableRows((prev) => prev.map((r) => (r.id === id ? { ...r, ...patch } : r)));
  };

  const updateDivRow = (id, patch) => {
    setDivGridRows((prev) => prev.map((r) => (r.id === id ? { ...r, ...patch } : r)));
  };

  const toggleTree = (idx) => {
    setTree((prev) =>
      prev.map((n, i) => (i === idx ? { ...n, open: !n.open } : n))
    );
  };

  return (
    <div className="app">
      <header className="hdr">
        <h1>MARS WebAutomation — React sample</h1>
        <p>Data table, form controls, tree, tabs, menus, native dialog, pseudo dialog.</p>
      </header>

      <nav className="menubar" role="menubar" aria-label="Main menu">
        <span className="brand">Menu</span>
        <div
          className="menu-item"
          onMouseEnter={() => setFileOpen(true)}
          onMouseLeave={() => setFileOpen(false)}
        >
          <button type="button" className="menu-btn" aria-haspopup="true" aria-expanded={fileOpen}>
            File
          </button>
          {fileOpen && (
            <ul className="popup" role="menu">
              <li role="none">
                <button type="button" role="menuitem" onClick={() => setFileOpen(false)}>
                  New
                </button>
              </li>
              <li role="none">
                <button type="button" role="menuitem" onClick={() => setFileOpen(false)}>
                  Open…
                </button>
              </li>
              <li role="none">
                <button type="button" role="menuitem" onClick={() => setFileOpen(false)}>
                  Exit
                </button>
              </li>
            </ul>
          )}
        </div>
        <div
          className="menu-item"
          onMouseEnter={() => setHelpOpen(true)}
          onMouseLeave={() => setHelpOpen(false)}
        >
          <button type="button" className="menu-btn" aria-haspopup="true" aria-expanded={helpOpen}>
            Help
          </button>
          {helpOpen && (
            <ul className="popup" role="menu">
              <li role="none">
                <button type="button" role="menuitem" onClick={() => setHelpOpen(false)}>
                  About
                </button>
              </li>
            </ul>
          )}
        </div>
      </nav>

      <section className="card" aria-labelledby="tabs-h">
        <h2 id="tabs-h" className="sr-only">
          Tabs
        </h2>
        <div className="tabs" role="tablist">
          {tabLabels.map((t, i) => (
            <button
              key={t}
              type="button"
              role="tab"
              aria-selected={activeTab === i}
              tabIndex={activeTab === i ? 0 : -1}
              className={activeTab === i ? 'on' : ''}
              onClick={() => setActiveTab(i)}
            >
              {t}
            </button>
          ))}
        </div>
        {activeTab === 0 && (
          <div role="tabpanel" className="tab-panel">
            <h3>Orders</h3>
            <form className="tab-panel-form" onSubmit={(e) => e.preventDefault()}>
            <div className="table-wrap">
              <table className="datatable" aria-label="Sample orders">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>
                      Customer <span className="th-hint">(click)</span>
                    </th>
                    <th>
                      Product <span className="th-hint">(click)</span>
                    </th>
                    <th>
                      Qty <span className="th-hint">(click)</span>
                    </th>
                    <th>Status</th>
                    <th>Rush</th>
                  </tr>
                </thead>
                <tbody>
                  {tableRows.map((row) => (
                    <tr key={row.id}>
                      <td>{row.id}</td>
                      <td className="cell-edit">
                        {editing?.id === row.id && editing?.field === 'customer' ? (
                          <input
                            id={`cell-${row.id}-customer`}
                            type="text"
                            className="cell-input"
                            value={row.customer}
                            onChange={(e) => updateRow(row.id, { customer: e.target.value })}
                            onBlur={() => setEditing(null)}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter') setEditing(null);
                            }}
                          />
                        ) : (
                          <span className="cell-display" onClick={() => setEditing({ id: row.id, field: 'customer' })}>
                            {row.customer}
                          </span>
                        )}
                      </td>
                      <td className="cell-edit">
                        {editing?.id === row.id && editing?.field === 'product' ? (
                          <input
                            id={`cell-${row.id}-product`}
                            type="text"
                            className="cell-input"
                            value={row.product}
                            onChange={(e) => updateRow(row.id, { product: e.target.value })}
                            onBlur={() => setEditing(null)}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter') setEditing(null);
                            }}
                          />
                        ) : (
                          <span className="cell-display" onClick={() => setEditing({ id: row.id, field: 'product' })}>
                            {row.product}
                          </span>
                        )}
                      </td>
                      <td className="cell-edit">
                        {editing?.id === row.id && editing?.field === 'qty' ? (
                          <input
                            id={`cell-${row.id}-qty`}
                            type="number"
                            min={0}
                            className="cell-input cell-input-narrow"
                            value={row.qty}
                            onChange={(e) => updateRow(row.id, { qty: Number(e.target.value) || 0 })}
                            onBlur={() => setEditing(null)}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter') setEditing(null);
                            }}
                          />
                        ) : (
                          <span className="cell-display" onClick={() => setEditing({ id: row.id, field: 'qty' })}>
                            {row.qty}
                          </span>
                        )}
                      </td>
                      <td>
                        <select
                          className="cell-select"
                          value={row.status}
                          aria-label={`Status for order ${row.id}`}
                          onChange={(e) => updateRow(row.id, { status: e.target.value })}
                        >
                          {statusOptions.map((s) => (
                            <option key={s} value={s}>
                              {s}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td className="cell-center">
                        <input
                          type="checkbox"
                          checked={row.rush}
                          aria-label={`Rush order ${row.id}`}
                          onChange={(e) => updateRow(row.id, { rush: e.target.checked })}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            </form>
          </div>
        )}
        {activeTab === 1 && (
          <div role="tabpanel" className="tab-panel">
            <h3>Regions (tree)</h3>
            <form className="tab-panel-form" onSubmit={(e) => e.preventDefault()} aria-label="Tree panel">
            <ul className="tree" role="tree" aria-label="Regions">
              {tree.map((n, idx) => (
                <li key={n.label} role="treeitem" aria-expanded={n.open} className="tree-node">
                  <button type="button" className="twisty" onClick={() => toggleTree(idx)}>
                    {n.open ? '▼' : '▶'}
                  </button>
                  {n.label}
                  {n.open && (
                    <ul role="group">
                      {n.children.map((c) => (
                        <li key={c} role="treeitem">
                          {c}
                        </li>
                      ))}
                    </ul>
                  )}
                </li>
              ))}
            </ul>
            </form>
          </div>
        )}
        {activeTab === 2 && (
          <div role="tabpanel" className="tab-panel">
            <h3>Notes</h3>
            <form className="tab-panel-form" onSubmit={(e) => e.preventDefault()}>
            <textarea
              name="tabNotes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={5}
              className="full"
              placeholder="Multi-line notes…"
              aria-label="Notes"
            />
            </form>
          </div>
        )}
        {activeTab === 3 && (
          <div role="tabpanel" className="tab-panel">
            <h3>PQ Grid (ParamQuery / pqgridf)</h3>
            <p className="hint">GPL ParamQuery grid — editable cells (double-click / F2 per theme).</p>
            <form className="tab-panel-form" onSubmit={(e) => e.preventDefault()}>
            <div ref={pqHostRef} className="pq-host" />
            </form>
          </div>
        )}
        {activeTab === 4 && (
          <div role="tabpanel" className="tab-panel">
            <h3>Div-based &quot;datagrid&quot;</h3>
            <p className="hint">CSS grid + div rows; click text cells to edit.</p>
            <form className="tab-panel-form" onSubmit={(e) => e.preventDefault()}>
            <div className="faux-grid" role="grid" aria-label="Parts (div grid)">
              <div className="faux-head" role="row">
                <div role="columnheader">Code</div>
                <div role="columnheader">Name (click)</div>
                <div role="columnheader">Units</div>
                <div role="columnheader">OK</div>
              </div>
              {divGridRows.map((r) => (
                <div key={r.id} className="faux-row" role="row">
                  <div className="faux-cell" role="gridcell">
                    {r.code}
                  </div>
                  <div className="faux-cell faux-edit" role="gridcell">
                    {divEdit?.id === r.id && divEdit?.field === 'name' ? (
                      <input
                        type="text"
                        className="cell-input"
                        value={r.name}
                        onChange={(e) => updateDivRow(r.id, { name: e.target.value })}
                        onBlur={() => setDivEdit(null)}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') setDivEdit(null);
                        }}
                      />
                    ) : (
                      <span className="cell-display" onClick={() => setDivEdit({ id: r.id, field: 'name' })}>
                        {r.name}
                      </span>
                    )}
                  </div>
                  <div className="faux-cell faux-edit" role="gridcell">
                    {divEdit?.id === r.id && divEdit?.field === 'units' ? (
                      <input
                        type="number"
                        min={0}
                        className="cell-input cell-input-narrow"
                        value={r.units}
                        onChange={(e) => updateDivRow(r.id, { units: Number(e.target.value) || 0 })}
                        onBlur={() => setDivEdit(null)}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') setDivEdit(null);
                        }}
                      />
                    ) : (
                      <span className="cell-display" onClick={() => setDivEdit({ id: r.id, field: 'units' })}>
                        {r.units}
                      </span>
                    )}
                  </div>
                  <div className="faux-cell faux-center" role="gridcell">
                    <input
                      type="checkbox"
                      checked={r.ok}
                      aria-label={'OK ' + r.code}
                      onChange={(e) => updateDivRow(r.id, { ok: e.target.checked })}
                    />
                  </div>
                </div>
              ))}
            </div>
            </form>
          </div>
        )}
      </section>

      <section className="card">
        <h2>Form controls</h2>
        <form className="grid-form" onSubmit={(e) => e.preventDefault()}>
          <label>
            Text field
            <input
              id="text-field"
              value={textField}
              onChange={(e) => setTextField(e.target.value)}
              type="text"
              name="username"
              autoComplete="username"
            />
          </label>
          <label>
            File
            <input type="file" name="doc" accept=".txt,.pdf,.png" />
          </label>
          <label className="row">
            Textarea
            <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} />
          </label>
          <label>
            Select (native)
            <select id="country-select" name="country" value={country} onChange={(e) => setCountry(e.target.value)}>
              <option value="">— choose —</option>
              <option value="us">United States</option>
              <option value="cn">China</option>
              <option value="de">Germany</option>
            </select>
          </label>
          <div>
            <span id="react-fake-lbl">Select (div-rendered)</span>
            <div className="fake-select" role="group" aria-labelledby="react-fake-lbl">
              <button
                type="button"
                className="fake-select-btn"
                onClick={() => setFakeOpen((o) => !o)}
                aria-expanded={fakeOpen}
                aria-haspopup="listbox"
              >
                {fakeLabel}
              </button>
              {fakeOpen && (
                <ul className="fake-select-list" role="listbox">
                  {[
                    { v: 'us', t: 'United States' },
                    { v: 'cn', t: 'China' },
                    { v: 'de', t: 'Germany' },
                  ].map((o) => (
                    <li
                      key={o.v}
                      role="option"
                      onClick={() => {
                        setFakeLabel(o.t);
                        setFakeOpen(false);
                      }}
                    >
                      {o.t}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
          <fieldset>
            <legend>Checkbox group</legend>
            <label>
              <input type="checkbox" checked={optEmail} onChange={(e) => setOptEmail(e.target.checked)} name="notify" />{' '}
              Email alerts
            </label>
            <label>
              <input type="checkbox" checked={optSms} onChange={(e) => setOptSms(e.target.checked)} name="sms" /> SMS alerts
            </label>
          </fieldset>
          <fieldset>
            <legend>Radio group</legend>
            <label>
              <input type="radio" name="tier" value="basic" checked={tier === 'basic'} onChange={() => setTier('basic')} />{' '}
              Basic
            </label>
            <label>
              <input type="radio" name="tier" value="pro" checked={tier === 'pro'} onChange={() => setTier('pro')} /> Pro
            </label>
            <label>
              <input type="radio" name="tier" value="ent" checked={tier === 'ent'} onChange={() => setTier('ent')} /> Enterprise
            </label>
          </fieldset>
          <div className="btn-row">
            <button type="button" className="primary" onClick={() => dialogRef.current?.showModal()}>
              Open native &lt;dialog&gt;
            </button>
            <button type="button" onClick={() => setPseudoOpen(true)}>
              Open pseudo dialog (div)
            </button>
            <button type="submit">Submit (demo)</button>
            <input
              type="image"
              className="img-btn"
              src="https://www.w3.org/assets/logos/w3c/w3c-no-bars.svg"
              alt="W3C image button"
              width="72"
              height="24"
              title="Image submit"
            />
          </div>
        </form>
      </section>

      <section className="card" aria-labelledby="pw-sim-title">
        <h2 id="pw-sim-title">Playwright simulation</h2>
        <form className="tab-panel-form" onSubmit={(e) => e.preventDefault()}>
        <div className="btn-row">
          <label>
            <input type="checkbox" name="pwSimPseudo" checked={pwSimPseudo} onChange={(e) => setPwSimPseudo(e.target.checked)} /> Use pseudo dialog (div)
          </label>
          <button
            type="button"
            className="primary"
            onClick={() => {
              if (pwSimPseudo) setPwSimOpen(true);
              else pwSimDialogRef.current?.showModal();
            }}
          >
            Open Playwright simulation dialog
          </button>
        </div>
        </form>
      </section>

      <section className="card" aria-labelledby="cross-url-title">
        <h2 id="cross-url-title">Cross-URL / iframe duplicate controls</h2>
        <p className="hint">The popup page and iframes expose the same name/id on text and select as this page (for Playwright disambiguation tests).</p>
        <form className="tab-panel-form" onSubmit={(e) => e.preventDefault()}>
        <div className="btn-row">
          <button
            type="button"
            className="primary"
            onClick={() => window.open('/mirror-window.html', 'mars_mirror_window', 'width=920,height=700,resizable=yes,scrollbars=yes')}
          >
            Open mirror page (new window)
          </button>
        </div>
        </form>
        <div className="iframe-grid">
          <iframe title="same-obj-frame-1" src="/mirror-controls.html?slot=1" />
          <iframe title="same-obj-frame-2" src="/mirror-controls.html?slot=2" />
        </div>
      </section>

      <section className="card" aria-labelledby="loc-lab-title">
        <h2 id="loc-lab-title">Locator lab (duplicate &amp; dynamic ids)</h2>
        <p className="hint">
          Reused template ids across rows/cells. Locator generation should compound aria-label / data-col / data-row-index instead of bare id.
        </p>
        <form className="tab-panel-form" onSubmit={(e) => e.preventDefault()}>
          <table className="datatable" aria-label="Dynamic id grid">
            <thead>
              <tr>
                <th scope="col">Row</th>
                <th scope="col">A</th>
                <th scope="col">B</th>
              </tr>
            </thead>
            <tbody>
              <tr id="rowId" data-row-index="0">
                <td>
                  <input id="cellId" name="dynField" type="text" data-col="A" aria-label="Row 0 column A" placeholder="r0a" />
                </td>
                <td>
                  <input id="cellId" name="dynField" type="text" data-col="B" aria-label="Row 0 column B" placeholder="r0b" />
                </td>
              </tr>
              <tr id="rowId" data-row-index="1">
                <td>
                  <input id="cellId" name="dynField" type="text" data-col="A" aria-label="Row 1 column A" placeholder="r1a" />
                </td>
                <td>
                  <input id="cellId" name="dynField" type="text" data-col="B" aria-label="Row 1 column B" placeholder="r1b" />
                </td>
              </tr>
            </tbody>
          </table>
          <p className="hint">Stable reference (unique id + data-testid):</p>
          <label>
            Stable field{' '}
            <input type="text" id="stable-lab-field" name="stableLab" data-testid="stable-lab-field" aria-label="Stable lab field" />
          </label>
          <p className="hint">Algorithmic id (treated as volatile):</p>
          <label>
            Volatile field <input type="text" id="row-92817" name="volatileRow" aria-label="Volatile row field" />
          </label>
        </form>
      </section>

      <dialog ref={dialogRef} className="dlg" aria-labelledby="dlg-title">
        <h2 id="dlg-title">Native dialog</h2>
        <p>
          This is a real <code>&lt;dialog&gt;</code> opened with <code>showModal()</code>.
        </p>
        <button type="button" onClick={() => dialogRef.current?.close()}>
          Close
        </button>
      </dialog>

      <dialog ref={pwSimDialogRef} className="dlg" aria-labelledby="pw-sim-native-title">
        <h2 id="pw-sim-native-title">Playwright simulation (native dialog)</h2>
        <p>Mode: native <code>&lt;dialog&gt;</code>.</p>
        <button type="button" onClick={() => pwSimDialogRef.current?.close()}>
          Close
        </button>
      </dialog>

      {pseudoOpen && (
        <div className="pseudo-overlay" role="presentation" onClick={(e) => e.target === e.currentTarget && setPseudoOpen(false)}>
          <div className="pseudo-box" role="dialog" aria-modal="true" aria-labelledby="pseudo-title">
            <h2 id="pseudo-title">Pseudo dialog</h2>
            <p>
              Overlay + <code>div</code> (not <code>&lt;dialog&gt;</code>).
            </p>
            <button type="button" onClick={() => setPseudoOpen(false)}>
              Close
            </button>
          </div>
        </div>
      )}

      {pwSimOpen && (
        <div className="pseudo-overlay" role="presentation" onClick={(e) => e.target === e.currentTarget && setPwSimOpen(false)}>
          <div className="pseudo-box" role="dialog" aria-modal="true" aria-labelledby="pw-sim-pseudo-title">
            <h2 id="pw-sim-pseudo-title">Playwright simulation (pseudo dialog)</h2>
            <p>Mode: div overlay (pseudo dialog).</p>
            <button type="button" onClick={() => setPwSimOpen(false)}>
              Close
            </button>
          </div>
        </div>
      )}

      <style>{`
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
        .dlg { border: none; border-radius: 8px; padding: 1rem; max-width: 400px; box-shadow: 0 20px 50px rgba(0,0,0,.25); }
        .dlg::backdrop { background: rgba(15,23,42,.45); }
        .pseudo-overlay { position: fixed; inset: 0; background: rgba(15,23,42,.45); display: flex; align-items: center; justify-content: center; z-index: 100; }
        .pseudo-box { background: #fff; padding: 1.25rem; border-radius: 8px; min-width: 280px; box-shadow: 0 20px 50px rgba(0,0,0,.2); }
        .fake-select { position: relative; max-width: 280px; }
        .fake-select-btn { width: 100%; text-align: left; padding: 0.45rem; border: 1px solid #cbd5e1; border-radius: 4px; background: #fff; cursor: pointer; }
        .fake-select-list { position: absolute; left: 0; right: 0; top: 100%; margin: 2px 0 0; padding: 0.25rem; list-style: none; background: #fff; border: 1px solid #cbd5e1; border-radius: 4px; z-index: 3; box-shadow: 0 4px 12px rgba(0,0,0,.08); }
        .fake-select-list li { padding: 0.35rem 0.5rem; cursor: pointer; border-radius: 4px; }
        .fake-select-list li:hover { background: #f1f5f9; }
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
      `}</style>
    </div>
  );
}
