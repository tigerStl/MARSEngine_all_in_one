/**
 * Rule 6: ToolButtonAggregator – click on ToolButton => SelectMenuIcon.
 * objectRef = parent toolbar, data = toolButton text/caption/tooltip/name.
 */

import type { ObjectRef } from '../../protocol/javaAgentProtocol';
import { toStrictRef, type SemanticStep } from '../types';
import { isToolButton, isToolBar } from './recordFilter';

function toolButtonData(targetRef: ObjectRef, options?: { text?: string; caption?: string; tooltip?: string; name?: string }): string {
  const o = options ?? {};
  if (o.text && String(o.text).trim()) return String(o.text).trim();
  if (o.caption && String(o.caption).trim()) return String(o.caption).trim();
  if (o.tooltip && String(o.tooltip).trim()) return String(o.tooltip).trim();
  if (o.name && String(o.name).trim()) return String(o.name).trim();
  const self = targetRef?.self;
  if (self?.javaName) return self.javaName;
  return '';
}

export interface ToolButtonAggregatorCallbacks {
  onStep: (step: SemanticStep) => void;
}

export class ToolButtonAggregator {
  private callbacks: ToolButtonAggregatorCallbacks;

  constructor(callbacks: ToolButtonAggregatorCallbacks) {
    this.callbacks = callbacks;
  }

  onToolButtonClick(
    ts: number,
    buttonRef: ObjectRef,
    toolbarRef: ObjectRef,
    options?: { text?: string; caption?: string; tooltip?: string; name?: string }
  ): void {
    if (!isToolButton(buttonRef) || !isToolBar(toolbarRef)) return;

    const data = toolButtonData(buttonRef, options);
    this.callbacks.onStep({
      keyword: 'SelectMenuIcon',
      objectRef: toStrictRef(toolbarRef),
      data: data || (toolbarRef.self?.javaName ?? ''),
      ts,
      meta: { clickedTool: toStrictRef(buttonRef).self },
    });
  }

  flush(): void {}
}
