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
  caption?: string;
  name?: string;
  namePath?: string[];
  javaType?: string;
  objectTypePath?: string[];
  bounds?: Bounds;
  screenBounds?: Bounds;
  visible?: boolean;
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
  | 'FillEdit'
  | 'SelectDropDown'
  | 'Click'
  | 'SelectMenuItem'
  | 'Check'
  | 'Uncheck';

export interface TestScriptStep {
  keyword: ScriptKeyword;
  parentIdentifier: ElementIdentifier;
  objectIdentifier: ElementIdentifier;
  parameter?: string;   // Constant ID reference
  data?: string;       // Constant ID reference
  assertValue?: string; // Constant ID reference
  skipped?: boolean;   // When true, step is skipped during execution
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
}
