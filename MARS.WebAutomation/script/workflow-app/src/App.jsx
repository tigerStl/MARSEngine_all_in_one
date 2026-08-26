import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import ReactFlow, {
  Background,
  Controls,
  Handle,
  MarkerType,
  MiniMap,
  Panel,
  Position,
  ReactFlowProvider,
  useEdgesState,
  useNodesState,
  useReactFlow
} from 'reactflow';
import 'reactflow/dist/style.css';

function keywordStyle(keyword) {
  const k = (keyword || '').toLowerCase();
  if (k === 'filledit')
    return { borderColor: '#1d4ed8', background: 'linear-gradient(155deg,#e8f4fd,#dbeafe)' };
  if (k === 'clickbutton')
    return { borderColor: '#c2410c', background: 'linear-gradient(155deg,#fff7ed,#ffedd5)' };
  if (k === 'selectdropdown')
    return { borderColor: '#6d28d9', background: 'linear-gradient(155deg,#f5f3ff,#ede9fe)' };
  if (k === 'setbox')
    return { borderColor: '#047857', background: 'linear-gradient(155deg,#ecfdf5,#d1fae5)' };
  if (k === 'filltable')
    return { borderColor: '#0e7490', background: 'linear-gradient(155deg,#ecfeff,#cffafe)' };
  if (k === 'searchandclick' || k === 'searchandupdate')
    return { borderColor: '#a16207', background: 'linear-gradient(155deg,#fef9c3,#fef08a)' };
  if (k === 'selecttab')
    return { borderColor: '#7c3aed', background: 'linear-gradient(155deg,#f5f3ff,#ede9fe)' };
  return { borderColor: '#475569', background: 'linear-gradient(155deg,#f8fafc,#e2e8f0)' };
}

function StepNode({ data }) {
  const st = keywordStyle(data.keyword);
  const order = data.runOrder != null ? data.runOrder : '';
  return (
    <div
      style={{
        minWidth: 180,
        maxWidth: 280,
        padding: '10px 12px',
        fontSize: '11px',
        borderRadius: 14,
        border: '1px solid rgba(15,23,42,.12)',
        boxShadow: '0 6px 18px rgba(15,23,42,.12)',
        position: 'relative',
        ...st
      }}
    >
      <Handle
        type="target"
        position={Position.Left}
        style={{ width: 8, height: 8, border: '2px solid #475569', background: '#fff' }}
      />
      <Handle
        type="source"
        position={Position.Right}
        style={{ width: 8, height: 8, border: '2px solid #475569', background: '#fff' }}
      />
      <div
        style={{
          position: 'absolute',
          top: -10,
          left: 10,
          fontSize: 11,
          fontWeight: 700,
          color: '#0f172a',
          background: 'rgba(255,255,255,.92)',
          border: '1px solid rgba(15,23,42,.15)',
          borderRadius: 8,
          padding: '2px 8px',
          lineHeight: 1.2
        }}
      >
        {order}
      </div>
      <div style={{ fontWeight: 600, fontSize: '11px', color: '#0f172a', marginTop: 4 }}>{data.keyword || ''}</div>
      <div style={{ fontSize: '11px', color: '#475569', marginTop: 6 }}>
        {(data.logicalKind || '') + ' · ' + (data.sourceEvent || '')}
      </div>
      <div style={{ fontSize: '11px', color: '#475569', marginTop: 4, wordBreak: 'break-word' }}>
        {data.data || ''}
      </div>
      <div
        style={{
          fontSize: 10,
          color: '#64748b',
          marginTop: 4,
          maxHeight: '3.2em',
          overflow: 'hidden'
        }}
      >
        {data.locatorShort || ''}
      </div>
    </div>
  );
}

const nodeTypes = { step: StepNode };

function FlowCanvas() {
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [uiLang, setUiLang] = useState('en');
  const [ctx, setCtx] = useState(null);
  const [fitWidthMode, setFitWidthMode] = useState(false);
  const savedViewportRef = useRef(null);
  const { fitView, getViewport, setViewport } = useReactFlow();
  const labels = useMemo(
    () =>
      (uiLang || '').toLowerCase().startsWith('zh')
        ? { edit: '编辑', test: '测试', fitWidth: '适配宽度', restore: '恢复', refresh: '刷新' }
        : { edit: 'Edit', test: 'Test', fitWidth: 'Fit Width', restore: 'Restore', refresh: 'Refresh' },
    [uiLang]
  );

  const applySteps = useCallback(
    (steps) => {
      if (!Array.isArray(steps)) return;
      const next = steps.map((s, i) => {
        const id = String(s.index ?? i);
        const col = i % 3;
        const row = Math.floor(i / 3);
        const defX = 28 + col * 228;
        const defY = 28 + row * 120;
        const x = typeof s.x === 'number' && !Number.isNaN(s.x) ? s.x : defX;
        const y = typeof s.y === 'number' && !Number.isNaN(s.y) ? s.y : defY;
        return {
          id,
          type: 'step',
          position: { x, y },
          data: {
            runOrder: i + 1,
            keyword: s.keyword,
            logicalKind: s.logicalKind,
            sourceEvent: s.sourceEvent,
            data: s.data,
            locatorShort: s.locatorShort
          },
          draggable: true
        };
      });
      const stroke = '#64748b';
      const linkEdges =
        next.length < 2
          ? []
          : next.slice(0, -1).map((n, i) => ({
              id: `seq-${n.id}-${next[i + 1].id}`,
              source: n.id,
              target: next[i + 1].id,
              type: 'default',
              style: { stroke, strokeWidth: 2 },
              markerEnd: {
                type: MarkerType.ArrowClosed,
                width: 18,
                height: 18,
                color: stroke
              }
            }));
      setNodes(next);
      setEdges(linkEdges);
      window.requestAnimationFrame(() => {
        try {
          fitView({ padding: 0.15, duration: 0 });
        } catch (_) {
          /* ignore */
        }
      });
    },
    [fitView, setEdges, setNodes]
  );

  const dispatchHostMessage = useCallback(
    (raw) => {
      try {
        const msg = typeof raw === 'string' ? JSON.parse(raw) : raw;
        if (!msg || !msg.type) return;
        if (msg.type === 'setSteps') {
          if (msg.uiLanguage) setUiLang(String(msg.uiLanguage));
          applySteps(msg.steps);
          return;
        }
        if (msg.type === 'setZoom') {
          const p = Number(msg.percent) || 100;
          const z = Math.max(0.2, Math.min(2.4, p / 100));
          const vp = getViewport();
          setViewport({ x: vp.x, y: vp.y, zoom: z }, { duration: 0 });
          return;
        }
        if (msg.type === 'centerView') {
          fitView({ padding: 0.2, duration: 0 });
        }
      } catch (_) {
        /* ignore */
      }
    },
    [applySteps, fitView, getViewport, setViewport]
  );

  const sendHost = useCallback((payload) => {
    try {
      window.chrome?.webview?.postMessage(JSON.stringify(payload));
    } catch (_) {
      /* ignore */
    }
  }, []);

  useEffect(() => {
    window.__marsWorkflowOnHostMessage = dispatchHostMessage;
    return () => {
      delete window.__marsWorkflowOnHostMessage;
    };
  }, [dispatchHostMessage]);

  useEffect(() => {
    const handler = (ev) => dispatchHostMessage(ev.data);
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.addEventListener('message', handler);
      try {
        window.chrome.webview.postMessage(JSON.stringify({ type: 'canvasReady' }));
      } catch (_) {
        /* ignore */
      }
      return () => window.chrome.webview.removeEventListener('message', handler);
    }
    return undefined;
  }, [dispatchHostMessage]);

  const onNodeDragStop = useCallback((_, node) => {
    try {
      const idx = parseInt(node.id, 10);
      if (Number.isNaN(idx)) return;
      window.chrome.webview.postMessage(
        JSON.stringify({
          type: 'nodeMoved',
          index: idx,
          x: Math.round(node.position.x),
          y: Math.round(node.position.y)
        })
      );
    } catch (_) {
      /* ignore */
    }
  }, []);

  const closeContextMenu = useCallback(() => setCtx(null), []);

  const onNodeContextMenu = useCallback((ev, node) => {
    ev.preventDefault();
    setCtx({ index: parseInt(node.id, 10), x: ev.clientX, y: ev.clientY });
  }, []);

  useEffect(() => {
    const close = () => setCtx(null);
    window.addEventListener('click', close);
    return () => window.removeEventListener('click', close);
  }, []);

  const onNodeClick = useCallback(
    (_ev, node) => {
      const idx = parseInt(node.id, 10);
      if (Number.isNaN(idx)) return;
      sendHost({ type: 'stepSelected', index: idx });
    },
    [sendHost]
  );

  const onContextEdit = useCallback(() => {
    if (!ctx || Number.isNaN(ctx.index)) return;
    const cur = nodes.find((n) => parseInt(n.id, 10) === ctx.index);
    const newKeyword = window.prompt(labels.edit + ' keyword', cur?.data?.keyword || '');
    if (newKeyword == null) return;
    const newData = window.prompt(labels.edit + ' data', cur?.data?.data || '');
    if (newData == null) return;
    sendHost({ type: 'editStep', index: ctx.index, keyword: newKeyword, data: newData });
    setCtx(null);
  }, [ctx, nodes, labels, sendHost]);

  const onContextTest = useCallback(() => {
    if (!ctx || Number.isNaN(ctx.index)) return;
    sendHost({ type: 'testStep', index: ctx.index });
    setCtx(null);
  }, [ctx, sendHost]);

  const toggleFitWidth = useCallback(() => {
    if (fitWidthMode) {
      const vp = savedViewportRef.current;
      if (vp) setViewport(vp, { duration: 0 });
      setFitWidthMode(false);
      return;
    }
    if (!nodes.length) return;
    savedViewportRef.current = getViewport();
    let minX = Number.POSITIVE_INFINITY;
    let maxX = Number.NEGATIVE_INFINITY;
    let minY = Number.POSITIVE_INFINITY;
    nodes.forEach((n) => {
      const x = n.position?.x || 0;
      const y = n.position?.y || 0;
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x + 240);
    });
    const w = Math.max(120, window.innerWidth - 48);
    const contentW = Math.max(180, maxX - minX + 32);
    const z = Math.max(0.2, Math.min(2.4, w / contentW));
    const x = 20 - minX * z;
    const y = 24 - minY * z;
    setViewport({ x, y, zoom: z }, { duration: 0 });
    setFitWidthMode(true);
  }, [fitWidthMode, getViewport, nodes, setViewport]);

  const refreshFromHost = useCallback(() => sendHost({ type: 'requestRefresh' }), [sendHost]);

  useEffect(() => {
    const onWheel = (ev) => {
      if (!ev.ctrlKey) return;
      ev.preventDefault();
      try {
        window.chrome.webview.postMessage(
          JSON.stringify({ type: 'wheelZoom', delta: ev.deltaY < 0 ? 120 : -120 })
        );
      } catch (_) {
        /* ignore */
      }
    };
    document.addEventListener('wheel', onWheel, { passive: false });
    return () => document.removeEventListener('wheel', onWheel);
  }, []);

  return (
    <div style={{ width: '100%', height: '100%', minHeight: 320 }}>
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onNodeDragStop={onNodeDragStop}
        onNodeClick={onNodeClick}
        onNodeContextMenu={onNodeContextMenu}
        nodeTypes={nodeTypes}
        fitView
        minZoom={0.2}
        maxZoom={2.4}
        proOptions={{ hideAttribution: true }}
      >
        <Background gap={16} />
        <MiniMap />
        <Controls showInteractive={false} />
        <Panel position="top-right">
          <div style={{ display: 'flex', gap: 6 }}>
            <button type="button" onClick={toggleFitWidth}>
              {fitWidthMode ? labels.restore : labels.fitWidth}
            </button>
            <button type="button" onClick={refreshFromHost}>
              {labels.refresh}
            </button>
          </div>
        </Panel>
      </ReactFlow>
      {ctx ? (
        <div
          style={{
            position: 'fixed',
            left: ctx.x,
            top: ctx.y,
            zIndex: 9999,
            background: '#fff',
            border: '1px solid #cbd5e1',
            borderRadius: 8,
            boxShadow: '0 6px 20px rgba(15,23,42,.18)',
            minWidth: 120
          }}
        >
          <button
            type="button"
            onClick={onContextEdit}
            style={{ width: '100%', textAlign: 'left', border: 0, background: 'transparent', padding: '8px 10px' }}
          >
            {labels.edit}
          </button>
          <button
            type="button"
            onClick={onContextTest}
            style={{ width: '100%', textAlign: 'left', border: 0, background: 'transparent', padding: '8px 10px' }}
          >
            {labels.test}
          </button>
        </div>
      ) : null}
    </div>
  );
}

export default function App() {
  return (
    <ReactFlowProvider>
      <FlowCanvas />
    </ReactFlowProvider>
  );
}
