/**
 * Adapter: RecordedStep (semantic, no bounds) -> TestScriptStep (panel/replay format).
 * Ensures identifiers never contain bounds.
 */

import type { RecordedStep } from '../protocol/javaAgentProtocol';
import type { TestScriptStep, ElementIdentifier, ScriptKeyword } from '../types';

const KEYWORDS: ScriptKeyword[] = [
  'Click', 'ClickButton', 'DoubleClickButton', 'ClickMenuIcon', 'FillEdit',
  'SelectDropDown', 'SelectDropList', 'SelectListItem', 'SelectMenuItem',
  'SelectTreeList', 'SelectTab', 'SelectMenuIcon', 'SelectPopupMenu',
  'SearchAndClick', 'SearchAndUpdate', 'Check', 'Uncheck',
];

function keyToElementId(p: { javaName: string; javaType: string; javaNamePath?: string[]; index: number }): ElementIdentifier {
  const id: ElementIdentifier = {
    javaType: p.javaType,
    index: p.index,
  };
  if (p.javaName) id.name = p.javaName;
  if (p.javaNamePath?.length) id.javaNamePath = p.javaNamePath;
  return id;
}

export function recordedStepToTestScriptStep(step: RecordedStep): TestScriptStep {
  const keyword = KEYWORDS.includes(step.keyword as ScriptKeyword) ? (step.keyword as ScriptKeyword) : 'Click';
  return {
    keyword,
    parentIdentifier: step.object.parentKey ? keyToElementId(step.object.parentKey) : {},
    objectIdentifier: keyToElementId(step.object.objectKey),
    parameter: step.parameter ?? '',
    data: step.data ?? '',
    assertValue: '',
    skipped: false,
  };
}
