/**
 * Converts agent scan output (roots/children) to UIObject format
 * Generates unique names, parent+object structure, index for disambiguation
 */

import { UIObject, ElementIdentifier, UIObjectTree } from './types';

export interface ScannedNode {
  id: string;
  parentId?: string;
  javaType: string;
  text?: string;
  value?: string;
  name?: string;
  caption?: string;
  title?: string;
  toolTipText?: string;
  baseTypes?: string[];
  bounds?: { x: number; y: number; width: number; height: number };
  screenBounds?: { x: number; y: number; width: number; height: number };
  visible?: boolean;
  children?: ScannedNode[];
}

export interface ScanOutput {
  roots?: ScannedNode[];
}

function sortByTopLeft(a: ScannedNode, b: ScannedNode): number {
  const ax = a.bounds?.y ?? 0;
  const ay = a.bounds?.x ?? 0;
  const bx = b.bounds?.y ?? 0;
  const by = b.bounds?.x ?? 0;
  if (ax !== bx) return ax - bx;
  return ay - by;
}

function flattenTree(node: ScannedNode, parentId: string | null, list: ScannedNode[]): void {
  const copy: ScannedNode = { ...node, parentId: parentId ?? undefined };
  list.push(copy);
  const children = node.children ?? [];
  children.sort(sortByTopLeft);
  for (const c of children) {
    flattenTree(c, node.id, list);
  }
}

function generateUniqueName(
  node: ScannedNode,
  used: Set<string>,
  parentPath: string[]
): string {
  const parts: string[] = [];
  if (node.caption) parts.push(sanitize(node.caption));
  else if (node.text) parts.push(sanitize(node.text.substring(0, 30)));
  else if (node.name) parts.push(sanitize(node.name));
  else if (node.toolTipText) parts.push(sanitize(node.toolTipText.substring(0, 30)));
  else parts.push(node.javaType.split('.').pop() ?? 'Component');

  let base = parts.join('_') || 'Component';
  base = base.replace(/[^a-zA-Z0-9_]/g, '_');
  if (!/^[a-zA-Z]/.test(base)) base = 'C_' + base;

  let name = base;
  let idx = 0;
  while (used.has(name)) {
    idx++;
    name = `${base}_${idx}`;
  }
  used.add(name);
  return name;
}

function sanitize(s: string): string {
  return s.replace(/\s+/g, '_').replace(/[^a-zA-Z0-9_]/g, '');
}

export function convertScanToUIObjects(scan: ScanOutput): UIObject[] {
  const roots = scan.roots ?? [];
  const flat: ScannedNode[] = [];
  for (const r of roots) {
    flattenTree(r, null, flat);
  }

  const idToNode = new Map<string, ScannedNode>();
  for (const n of flat) {
    idToNode.set(n.id, n);
  }

  const used = new Set<string>();
  const result: UIObject[] = [];

  for (const node of flat) {
    const parent = node.parentId ? idToNode.get(node.parentId) : null;
    const parentIdentifier: ElementIdentifier | null = parent
      ? {
          javaType: parent.javaType,
          text: parent.text,
          name: parent.name,
          caption: parent.caption,
        }
      : null;

    const siblings = flat.filter((n) => n.parentId === node.parentId).sort(sortByTopLeft);
    const index = siblings.findIndex((s) => s.id === node.id);
    const needIndex = siblings.filter(
      (s) =>
        s.javaType === node.javaType &&
        (s.text ?? '') === (node.text ?? '') &&
        (s.name ?? '') === (node.name ?? '')
    ).length > 1;

    const identifier: ElementIdentifier = {
      javaType: node.javaType,
    };
    if (node.text) identifier.text = node.text;
    if (node.value != null) identifier.value = node.value;
    if (node.name) identifier.name = node.name;
    if (node.caption) identifier.caption = node.caption;
    if (node.title) identifier.title = node.title;
    if (node.toolTipText) identifier.toolTipText = node.toolTipText;
    if (node.bounds) identifier.bounds = node.bounds;
    if (node.screenBounds) identifier.screenBounds = node.screenBounds;
    if (node.visible !== undefined) identifier.visible = node.visible;
    if (node.baseTypes && node.baseTypes.length) identifier.baseTypes = node.baseTypes;

    const uniqueName = generateUniqueName(node, used, []);

    result.push({
      uniqueName,
      parent: parentIdentifier,
      identifier,
      ...(needIndex && index >= 0 ? { index } : {}),
    });
  }

  return result;
}

/** Convert scan output to a tree (for panel display). Preserves hierarchy. */
export function convertScanToUIObjectTree(scan: ScanOutput): UIObjectTree[] {
  const roots = scan.roots ?? [];
  const flat: ScannedNode[] = [];
  for (const r of roots) {
    flattenTree(r, null, flat);
  }
  const idToNode = new Map<string, ScannedNode>();
  for (const n of flat) {
    idToNode.set(n.id, n);
  }
  const used = new Set<string>();
  const idToObj = new Map<string, UIObject>();
  for (const node of flat) {
    const parent = node.parentId ? idToNode.get(node.parentId) : null;
    const parentIdentifier: ElementIdentifier | null = parent
      ? {
          javaType: parent.javaType,
          text: parent.text,
          name: parent.name,
          caption: parent.caption,
        }
      : null;
    const siblings = flat.filter((n) => n.parentId === node.parentId).sort(sortByTopLeft);
    const index = siblings.findIndex((s) => s.id === node.id);
    const needIndex =
      siblings.filter(
        (s) =>
          s.javaType === node.javaType &&
          (s.text ?? '') === (node.text ?? '') &&
          (s.name ?? '') === (node.name ?? '')
      ).length > 1;
    const identifier: ElementIdentifier = { javaType: node.javaType };
    if (node.text) identifier.text = node.text;
    if (node.value != null) identifier.value = node.value;
    if (node.name) identifier.name = node.name;
    if (node.caption) identifier.caption = node.caption;
    if (node.title) identifier.title = node.title;
    if (node.toolTipText) identifier.toolTipText = node.toolTipText;
    if (node.bounds) identifier.bounds = node.bounds;
    if (node.screenBounds) identifier.screenBounds = node.screenBounds;
    if (node.visible !== undefined) identifier.visible = node.visible;
    if (node.baseTypes && node.baseTypes.length) identifier.baseTypes = node.baseTypes;
    const uniqueName = generateUniqueName(node, used, []);
    const obj: UIObject = {
      uniqueName,
      parent: parentIdentifier,
      identifier,
      ...(needIndex && index >= 0 ? { index } : {}),
    };
    idToObj.set(node.id, obj);
  }
  function toTree(node: ScannedNode): UIObjectTree {
    const obj = idToObj.get(node.id)!;
    const children = (node.children ?? []).slice().sort(sortByTopLeft).map(toTree);
    return { ...obj, children };
  }
  return roots.map(toTree);
}
