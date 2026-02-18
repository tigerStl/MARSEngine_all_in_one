/**
 * Rule 2: ClickRecordFilter – drop clicks on disabled/invisible or non-interactive roles.
 * Only allowlist roles produce semantic click steps.
 */

import type { ObjectRef } from '../../protocol/javaAgentProtocol';

const NON_INTERACTIVE_ROLES = new Set([
  'Label',
  'StaticText',
  'Text',
  'Panel',
  'GroupBox',
  'Container',
  'ScrollPane',
  'SplitPane',
  'TableCellRenderer',
  'Icon',
  'Image',
  'Separator',
  'StatusBar',
  'ToolBar', // toolbar itself not recorded; children (ToolButton) are
]);

const ALLOWLIST_TYPES = new Set([
  'Button',
  'ToolButton',
  'ToggleButton',
  'TextField',
  'TextArea',
  'ComboBox',
  'TreeView',
  'JTree',
  'MenuItem',
  'JMenuItem',
  'Tab',
  'JButton',
  'JToggleButton',
  'JTextField',
  'JTextArea',
  'JComboBox',
  'JMenuItem',
  'JTabbedPane',
]);

function typeMatchesAllowlist(javaType: string): boolean {
  if (!javaType) return false;
  const t = javaType.replace(/^javax\.swing\./, '').replace(/^java\.awt\./, '');
  if (ALLOWLIST_TYPES.has(t)) return true;
  if (t.includes('Button')) return true;
  if (t.includes('TextField') || t.includes('TextArea')) return true;
  if (t.includes('ComboBox')) return true;
  if (t.includes('Tree') && !t.includes('Renderer')) return true;
  if (t.includes('MenuItem')) return true;
  if (t.includes('Tab')) return true;
  return false;
}

function typeMatchesBlocklist(javaType: string): boolean {
  if (!javaType) return false;
  const t = javaType.replace(/^javax\.swing\./, '').replace(/^java\.awt\./, '');
  if (NON_INTERACTIVE_ROLES.has(t)) return true;
  if (t.includes('Label') || t.includes('StaticText')) return true;
  if (t.includes('Panel') || t.includes('GroupBox') || t.includes('ScrollPane') || t.includes('SplitPane')) return true;
  if (t.includes('Renderer')) return true;
  if (t.includes('Separator') || t.includes('StatusBar')) return true;
  if (t.includes('Menu') && !t.includes('MenuItem') && !t.includes('JMenuItem') && !t.includes('MenuBar')) return true;
  if (t === 'JToolBar') return true; // toolbar itself not recorded
  return false;
}

/**
 * Returns true if this click (or mouse down/up) should be recorded.
 * Drop if target is disabled, invisible, or role is non-interactive.
 */
export function shouldRecordClick(target: ObjectRef | undefined, options?: { enabled?: boolean; visible?: boolean }): boolean {
  if (!target?.self) return false;
  const enabled = options?.enabled ?? true;
  const visible = options?.visible ?? true;
  if (!enabled || !visible) return false;
  const javaType = (target.self.javaType ?? '') as string;
  if (typeMatchesBlocklist(javaType)) return false;
  if (!typeMatchesAllowlist(javaType)) return false;
  return true;
}

export function isComboBox(target: ObjectRef | undefined): boolean {
  const t = (target?.self?.javaType ?? '') as string;
  return t.includes('ComboBox');
}

export function isTreeView(target: ObjectRef | undefined): boolean {
  const t = (target?.self?.javaType ?? '') as string;
  return t.includes('JTree') || t.includes('TreeView');
}

export function isMenuItem(target: ObjectRef | undefined): boolean {
  const t = (target?.self?.javaType ?? '') as string;
  return t.includes('MenuItem') || t.includes('JMenuItem');
}

export function isMenuBar(target: ObjectRef | undefined): boolean {
  const t = (target?.self?.javaType ?? '') as string;
  return t.includes('MenuBar') || t.includes('JMenuBar');
}

export function isToolButton(target: ObjectRef | undefined): boolean {
  const t = (target?.self?.javaType ?? '') as string;
  if (t.includes('JButton') && target?.parent?.javaType?.includes('ToolBar')) return true;
  return t.includes('ToolButton');
}

export function isToolBar(target: ObjectRef | undefined): boolean {
  const t = (target?.self?.javaType ?? '') as string;
  return t.includes('ToolBar') || t.includes('JToolBar');
}

export function isTab(target: ObjectRef | undefined): boolean {
  const t = (target?.self?.javaType ?? '') as string;
  return t.includes('TabbedPane') || t.includes('Tab');
}

export function isEditControl(target: ObjectRef | undefined): boolean {
  const t = (target?.self?.javaType ?? '') as string;
  return t.includes('TextField') || t.includes('TextArea') || t.includes('JTextComponent');
}

export function isEditableComboEditor(target: ObjectRef | undefined): boolean {
  const t = (target?.self?.javaType ?? '') as string;
  return t.includes('ComboBox') || t.includes('JTextField'); // combo editor is often JTextField
}
