/**
 * Rule 3: ClickAggregator – distinguish Click vs DoubleClick.
 * Same target, interval <= DBLCLICK_MS, distance <= 6px => DoubleClick; else Click.
 * Only for generic Button; ComboBox/Menu/Tree handled by their aggregators.
 */

import type { ObjectRef } from '../../protocol/javaAgentProtocol';
import { toStrictRef, sameTargetRef, type SemanticStep } from '../types';
import { schedule, cancel } from './timerQueue';
import { shouldRecordClick, isComboBox, isTreeView, isMenuItem, isToolButton, isTab } from './recordFilter';

const DBLCLICK_MS = 450;
const DIST_PX = 6;
const PENDING_ID = 'clickAggregator.pending';

function manhattan(
  a: { x?: number; y?: number; w?: number; width?: number; h?: number; height?: number },
  b: { x?: number; y?: number; w?: number; width?: number; h?: number; height?: number }
): number {
  const ax = a.x ?? 0;
  const ay = a.y ?? 0;
  const bx = b.x ?? 0;
  const by = b.y ?? 0;
  return Math.abs(ax - bx) + Math.abs(ay - by);
}

export interface ClickAggregatorCallbacks {
  onStep: (step: SemanticStep) => void;
}

interface Pending {
  ts: number;
  targetRef: ObjectRef | undefined;
  x: number;
  y: number;
}

export class ClickAggregator {
  private pending: Pending | null = null;
  private callbacks: ClickAggregatorCallbacks;

  constructor(callbacks: ClickAggregatorCallbacks) {
    this.callbacks = callbacks;
  }

  /** Call for ui.mouse type 'click' or after second 'up' when we synthesize click. */
  onMouseClick(ts: number, x: number, y: number, targetRef: ObjectRef | undefined): void {
    if (!shouldRecordClick(targetRef)) return;
    if (isComboBox(targetRef) || isTreeView(targetRef) || isMenuItem(targetRef) || isToolButton(targetRef)) return;
    if (isTab(targetRef)) {
      this.callbacks.onStep({
        keyword: 'SelectTab',
        objectRef: toStrictRef(targetRef),
        data: targetRef?.self?.javaName ?? '',
        ts,
      });
      return;
    }

    const prev = this.pending;
    if (prev?.targetRef && sameTargetRef(prev.targetRef, targetRef)) {
      const dt = ts - prev.ts;
      const dist = manhattan(
        prev.targetRef?.self?.bounds ?? { x: prev.x, y: prev.y },
        targetRef?.self?.bounds ?? { x, y }
      );
      if (dt <= DBLCLICK_MS && dist <= DIST_PX) {
        cancel(PENDING_ID);
        this.pending = null;
        this.callbacks.onStep({
          keyword: 'DoubleClickButton',
          objectRef: toStrictRef(targetRef),
          ts,
          meta: { clickType: 'DoubleClick' },
        });
        return;
      }
    }

    if (prev) {
      cancel(PENDING_ID);
      this.emitClick(prev.ts, prev.targetRef);
      this.pending = null;
    }

    schedule(PENDING_ID, DBLCLICK_MS, () => {
      if (this.pending) {
        this.emitClick(this.pending!.ts, this.pending!.targetRef);
        this.pending = null;
      }
    });
    this.pending = { ts, targetRef, x, y };
  }

  private emitClick(ts: number, targetRef: ObjectRef | undefined): void {
    this.callbacks.onStep({
      keyword: 'ClickButton',
      objectRef: toStrictRef(targetRef),
      ts,
    });
  }

  flush(): void {
    cancel(PENDING_ID);
    if (this.pending) {
      this.emitClick(this.pending.ts, this.pending.targetRef);
      this.pending = null;
    }
  }
}
