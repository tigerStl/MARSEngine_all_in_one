/**
 * Adapter: RecordedStep (semantic, no bounds) -> TestScriptStep (panel/replay format).
 * Ensures identifiers never contain bounds in test steps (bounds/screenBounds are only used for
 * visual tree/export, not for replay object locators).
 */

import type { RecordedStep } from '../protocol/javaAgentProtocol';
import type { TestScriptStep, ElementIdentifier, ScriptKeyword } from '../types';

function keyToElementId(p: unknown): ElementIdentifier {
  const key = (p ?? {}) as Record<string, unknown>;
  const javaType = typeof key.javaType === 'string' ? key.javaType : '';
  const id: ElementIdentifier = {
    javaType,
  };
  if (typeof key.index === 'number') {
    // For now, JavaFX objects are located by javaName/text/title etc., not by index.
    // When index is not meaningfully handled, do not set it on the test step identifier.
    if (!javaType.startsWith('javafx.')) {
      id.index = key.index;
    }
  }
  if (typeof key.javaName === 'string' && key.javaName) id.name = key.javaName;
  if (Array.isArray(key.javaNamePath)) id.javaNamePath = key.javaNamePath as string[];
  if (typeof key.text === 'string') id.text = key.text;
  if (typeof key.caption === 'string') id.caption = key.caption;
  if (typeof key.title === 'string') id.title = key.title;
  if (typeof key.toolTipText === 'string') id.toolTipText = key.toolTipText;
  if (typeof key.value === 'string') id.value = key.value;
  if (typeof key.semanticRole === 'string') id.semanticRole = key.semanticRole;
  return id;
}

export function recordedStepToTestScriptStep(step: RecordedStep): TestScriptStep {
  const keyword = (step.keyword && step.keyword.trim()
    ? step.keyword
    : 'Click') as ScriptKeyword;
  return {
    keyword,
    parentIdentifier: step.object.parentKey ? keyToElementId(step.object.parentKey) : {},
    objectIdentifier: keyToElementId(step.object.objectKey),
    parameter: step.parameter ?? '',
    data: step.data ?? '',
    assertValue: '',
    skipped: false,
    waitTime: 0,
  };
}
