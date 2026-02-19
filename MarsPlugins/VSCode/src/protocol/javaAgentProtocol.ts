/**
 * JSON-RPC over WebSocket: Agent <-> Extension
 * Request/response + event stream.
 */

export interface BoundsRect {
  x: number;
  y: number;
  w: number;
  h: number;
}

export interface ObjectKey {
  javaName: string;
  javaType: string;
  javaNamePath?: string[];
  index: number;
  bounds?: BoundsRect;
}

/** Identifiers for steps: no position. Bounds only in meta.debugBounds. */
export interface ObjectKeyStrict {
  javaName: string;
  javaType: string;
  javaNamePath?: string[];
  index: number;
}

export interface ObjectRef {
  parent: ObjectKey | null;
  self: ObjectKey;
}

export interface ObjectTreeNode {
  id: string;
  role: string;
  name: string;
  text: string;
  className: string;
  bounds: BoundsRect;
  enabled: boolean;
  visible: boolean;
  focusable: boolean;
  tabIndex?: number;
  zIndex?: number;
  value?: string;
  children: string[];
  parentId?: string;
}

export interface ObjectTreeWindow {
  windowId: string;
  title: string;
  bounds: BoundsRect;
  rootNodeId: string;
}

export interface ObjectTreeSnapshot {
  timestamp: number;
  processId?: number;
  appName?: string;
  windows: ObjectTreeWindow[];
  nodes: Record<string, ObjectTreeNode>;
}

// --- Requests (method + params) ---
export type AgentRequest =
  | { method: 'agent.ping'; id: number; params?: void }
  | { method: 'agent.listJvms'; id: number; params?: void }
  | { method: 'agent.attach'; id: number; params: { pid: number } }
  | { method: 'agent.getObjectTree'; id: number; params?: { rootWindowHint?: string } }
  | { method: 'agent.subscribeEvents'; id: number; params?: { includeTreeRef?: boolean } }
  | { method: 'agent.readControlValue'; id: number; params: { objectKey: ObjectKey; parentKey?: ObjectKey | null } }
  | { method: 'agent.perform'; id: number; params: PerformParams };

export interface PerformParams {
  action: 'Click' | 'DoubleClick' | 'SetText' | 'PressKey' | 'SelectTab';
  target: { parentKey: ObjectKey | null; objectKey: ObjectKey };
  data?: string;
}

export interface AgentResponse {
  id: number;
  result?: unknown;
  error?: { code: number; message: string };
}

export interface ListJvmsResult {
  jvms: Array<{ pid: number; displayName: string }>;
}

export interface AttachResult {
  ok: boolean;
  port?: number;
}

// --- Events (push from agent) ---
export type AgentEvent =
  | { event: 'ui.mouse'; ts: number; type: 'down' | 'up' | 'click' | 'dblclick'; button: number; x: number; y: number; target?: ObjectRef }
  | { event: 'ui.key'; ts: number; type: 'down' | 'up' | 'char'; key: string; code: number; target?: ObjectRef }
  | { event: 'ui.focus'; ts: number; type: 'focusGained' | 'focusLost'; target: ObjectRef }
  | { event: 'ui.window'; ts: number; type: 'activated' | 'opened' | 'closed'; windowTitle: string }
  | { event: 'ui.snapshotHint'; ts: number; windowId: string; rootNodeId: string };

// --- Step (recorder output / replay input) ---
export interface StepObject {
  parentKey: ObjectKey | null;
  objectKey: ObjectKey;
}

export interface StepObjectStrict {
  parentKey: ObjectKeyStrict | null;
  objectKey: ObjectKeyStrict;
}

export interface RecordedStep {
  id: string;
  keyword: string;
  object: StepObject;
  parameter?: string;
  data?: string;
  meta?: { ts?: number; confidence?: number; bounds?: BoundsRect; debugBounds?: BoundsRect; clickType?: string; emitEnter?: boolean; clickedTool?: ObjectKeyStrict };
}
