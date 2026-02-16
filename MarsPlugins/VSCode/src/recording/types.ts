/**
 * Recording pipeline: canonical event and semantic step types.
 * Object identifiers: javaName, javaType, javaNamePath, index only (no bounds).
 */

import type { ObjectKey, ObjectRef, BoundsRect } from '../protocol/javaAgentProtocol';

export interface ObjectKeyStrict {
  javaName: string;
  javaType: string;
  javaNamePath?: string[];
  index: number;
}

export interface ObjectRefStrict {
  parent: ObjectKeyStrict | null;
  self: ObjectKeyStrict;
}

export interface SemanticStep {
  keyword: string;
  objectRef: ObjectRefStrict;
  data?: string;
  ts: number;
  meta?: {
    debugBounds?: BoundsRect;
    clickType?: string;
    emitEnter?: boolean;
    clickedTool?: ObjectKeyStrict;
  };
}

/** Canonical raw events from agent (or mapped from legacy). */
export type CanonicalMouseType = 'down' | 'up' | 'click' | 'dblclick';
export type CanonicalFocusType = 'focusGained' | 'focusLost';
export type CanonicalKeyType = 'down' | 'up' | 'char';

export interface CanonicalMouseEvent {
  kind: 'ui.mouse';
  ts: number;
  type: CanonicalMouseType;
  button: number;
  x: number;
  y: number;
  target?: ObjectRef;
}

export interface CanonicalFocusEvent {
  kind: 'ui.focus';
  ts: number;
  type: CanonicalFocusType;
  target: ObjectRef;
}

export interface CanonicalKeyEvent {
  kind: 'ui.key';
  ts: number;
  type: CanonicalKeyType;
  key: string;
  code: number;
  target?: ObjectRef;
}

export type CanonicalEvent = CanonicalMouseEvent | CanonicalFocusEvent | CanonicalKeyEvent;

/** Strip bounds from ObjectKey for step output. */
export function toStrictKey(k: ObjectKey | undefined | null): ObjectKeyStrict {
  if (!k) return { javaName: '', javaType: '', index: 0 };
  const strict: ObjectKeyStrict = {
    javaName: k.javaName ?? '',
    javaType: k.javaType ?? '',
    index: typeof k.index === 'number' ? k.index : 0,
  };
  if (k.javaNamePath && k.javaNamePath.length) strict.javaNamePath = [...k.javaNamePath];
  return strict;
}

export function toStrictRef(ref: ObjectRef | undefined | null): ObjectRefStrict {
  if (!ref) return { parent: null, self: toStrictKey(undefined) };
  return {
    parent: ref.parent ? toStrictKey(ref.parent) : null,
    self: toStrictKey(ref.self),
  };
}

export function sameTargetStrict(a: ObjectRefStrict | undefined, b: ObjectRefStrict | undefined): boolean {
  if (!a || !b) return a === b;
  if (a.self.javaName !== b.self.javaName || a.self.javaType !== b.self.javaType) return false;
  const ap = (a.self.javaNamePath ?? []).join('/');
  const bp = (b.self.javaNamePath ?? []).join('/');
  if (ap !== bp) return false;
  return (a.self.index ?? 0) === (b.self.index ?? 0);
}

export function sameTargetRef(a: ObjectRef | undefined, b: ObjectRef | undefined): boolean {
  if (!a || !b) return a === b;
  if ((a.self?.javaName ?? '') !== (b.self?.javaName ?? '')) return false;
  if ((a.self?.javaType ?? '') !== (b.self?.javaType ?? '')) return false;
  const ap = (a.self?.javaNamePath ?? []).join('/');
  const bp = (b.self?.javaNamePath ?? []).join('/');
  if (ap !== bp) return false;
  return (a.self?.index ?? 0) === (b.self?.index ?? 0);
}
