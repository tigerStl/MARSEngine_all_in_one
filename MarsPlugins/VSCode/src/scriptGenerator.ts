/**
 * Test script generator - FillEdit, SelectDropDown, etc.
 * All literals must reference constants
 */

import {
  TestScriptStep,
  UIObject,
  ConstantsFile,
  ConstantEntry,
  ScriptKeyword,
  ElementIdentifier,
} from './types';
import * as path from 'path';
import * as fs from 'fs';

export interface ScriptGenerationInput {
  keyword: ScriptKeyword;
  parentObject: UIObject;
  targetObject: UIObject;
  dataConstantId?: string;
  assertConstantId?: string;
}

function elementToIdentifier(obj: UIObject): ElementIdentifier {
  const id: ElementIdentifier = { ...obj.identifier };
  if (obj.index !== undefined && obj.index >= 0) {
    (id as Record<string, unknown>).index = obj.index;
  }
  return id;
}

export function generateScriptStep(input: ScriptGenerationInput): TestScriptStep {
  return {
    keyword: input.keyword,
    parentIdentifier: input.parentObject.identifier ?? {},
    objectIdentifier: elementToIdentifier(input.targetObject),
    data: input.dataConstantId,
    assertValue: input.assertConstantId,
  };
}

export function addConstant(
  constants: ConstantsFile,
  value: string,
  prefix = 'CONST'
): string {
  const id = `${prefix}_${value.toUpperCase().replace(/[^A-Z0-9]/g, '_')}_${Date.now().toString(36)}`;
  constants.constants.push({ id, value });
  return id;
}

export function ensureConstantExists(
  constants: ConstantsFile,
  id: string,
  value: string
): void {
  if (!constants.constants.find((c) => c.id === id)) {
    constants.constants.push({ id, value });
  }
}

export function saveScript(
  steps: TestScriptStep[],
  filePath: string
): void {
  fs.writeFileSync(
    filePath,
    JSON.stringify({ steps }, null, 2),
    'utf-8'
  );
}

export function saveConstants(constants: ConstantsFile, filePath: string): void {
  fs.writeFileSync(filePath, JSON.stringify(constants, null, 2), 'utf-8');
}

export function loadConstants(filePath: string): ConstantsFile {
  const content = fs.readFileSync(filePath, 'utf-8');
  return JSON.parse(content) as ConstantsFile;
}

export function inferKeywordFromJavaType(javaType: string): ScriptKeyword {
  const t = javaType.toLowerCase();
  if (t.includes('jtextfield') || t.includes('jtextarea') || t.includes('jpasswordfield')) {
    return 'FillEdit';
  }
  if (t.includes('jcombobox') || t.includes('jlist')) {
    return 'SelectDropDown';
  }
  if (t.includes('jbutton') || t.includes('jmenuitem')) {
    return 'Click';
  }
  if (t.includes('jcheckbox')) {
    return 'Check';
  }
  return 'Click';
}
