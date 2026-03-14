/**
 * Java UI Automation - Type Definitions
 * All constant references - no hardcoded strings in script/object definitions
 */

export interface Bounds {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface ElementIdentifier {
  text?: string;
  value?: string;
  caption?: string;
  title?: string;
  toolTipText?: string;
  name?: string;
  /** Object name (for display); may differ from name when caption/text used. */
  objectName?: string;
  namePath?: string[];
  javaType?: string;
  objectTypePath?: string[];
  /** JavaType path from root to this node (for disambiguation). */
  javaTypePath?: string[];
  /** JavaName path from root to this node (for disambiguation). */
  javaNamePath?: string[];
  baseTypes?: string[];
  bounds?: Bounds;
  screenBounds?: Bounds;
  visible?: boolean;
  /** Index when name+javaType+path still duplicate (0,1,2... top-to-bottom, left-to-right). */
  index?: number;
  /** Semantic role from config (e.g. Tree, Table, Edit); used for display and parsing. */
  semanticRole?: string;
}

export interface UIObject {
  uniqueName: string;
  parent?: ElementIdentifier | null;
  identifier: ElementIdentifier;
  index?: number;
}

/** UIObject with children for tree display */
export interface UIObjectTree extends UIObject {
  children: UIObjectTree[];
}

export type ScriptKeyword =
  | 'Click'
  | 'ClickButton'
  | 'DoubleClickButton'
  | 'ClickMenuIcon'
  | 'FillEdit'
  | 'SelectDropDown'
  | 'SelectDropList'
  | 'SelectListItem'
  | 'SelectMenuItem'
  | 'SelectTreeList'
  | 'SelectTab'
  | 'SelectMenuIcon'
  | 'SelectPopupMenu'
  | 'ClickAT'
  | 'SearchAndClick'
  | 'SearchAndUpdate'
  | 'VerifyObjectValue'
  | 'SetRadioBox'
  | 'SetCheckBox'
  | 'Check'
  | 'Uncheck';

/** Single source of truth for all step keywords (runtime list). */
export const SCRIPT_KEYWORDS: readonly ScriptKeyword[] = [
  'Click', 'ClickButton', 'DoubleClickButton', 'ClickMenuIcon', 'FillEdit',
  'SelectDropDown', 'SelectDropList', 'SelectListItem', 'SelectMenuItem',
  'SelectTreeList', 'SelectTab', 'SelectMenuIcon', 'SelectPopupMenu', 'ClickAT',
  'SearchAndClick', 'SearchAndUpdate', 'VerifyObjectValue',
  'SetRadioBox', 'SetCheckBox', 'Check', 'Uncheck',
] as const;

export function isScriptKeyword(s: string): s is ScriptKeyword {
  return (SCRIPT_KEYWORDS as readonly string[]).includes(s);
}

export interface TestScriptStep {
  keyword: ScriptKeyword;
  parentIdentifier: ElementIdentifier;
  objectIdentifier: ElementIdentifier;
  parameter?: string;   // Constant ID reference
  data?: string;       // Constant ID reference
  assertValue?: string; // Constant ID reference
  skipped?: boolean;   // When true, step is skipped during execution
  /** Per-step wait time in seconds. 0 or undefined means use default (auto). */
  waitTime?: number;
}

export interface ConstantEntry {
  id: string;
  value: string;
  category?: string;
}

export interface ConstantsFile {
  constants: ConstantEntry[];
}

export interface ScannedComponent {
  id: string;
  javaType: string;
  text?: string;
  name?: string;
  caption?: string;
  bounds?: { x: number; y: number; width: number; height: number };
  children: ScannedComponent[];
  parentId?: string;
}

export interface JavaProcess {
  pid: number;
  mainClass?: string;
  commandLine?: string;
  displayName: string;
  source?: string;
}
