/**
 * Rule 4: TreeSelectionAggregator – TreeView click => SelectTreeList, data = "Root;Parent;...;Selected" (path from root to selected node, ";" separated).
 */

import type { ObjectRef } from '../../protocol/javaAgentProtocol';
import { toStrictRef, type SemanticStep } from '../types';
import { schedule, cancel } from './timerQueue';
import { isTreeView } from './recordFilter';

const TREE_READ_DELAY_MS = 80;
const TREE_PENDING_ID = 'treeSelection.pending';

export interface TreeSelectionAggregatorCallbacks {
  onStep: (step: SemanticStep) => void;
  readTreeSelection?: (treeRef: ObjectRef) => Promise<string[]>;
}

interface PendingTree {
  treeRef: ObjectRef;
  ts: number;
  clickType: 'Click' | 'DoubleClick';
}

export class TreeSelectionAggregator {
  private pending: PendingTree | null = null;
  private callbacks: TreeSelectionAggregatorCallbacks;

  constructor(callbacks: TreeSelectionAggregatorCallbacks) {
    this.callbacks = callbacks;
  }

  onTreeClick(ts: number, targetRef: ObjectRef | undefined, clickType: 'Click' | 'DoubleClick'): void {
    if (!targetRef?.self || !isTreeView(targetRef)) return;

    const treeRef = targetRef;

    const doEmit = (pathStr: string) => {
      this.callbacks.onStep({
        keyword: 'SelectTreeList',
        objectRef: toStrictRef(treeRef),
        data: pathStr,
        ts,
        meta: clickType === 'DoubleClick' ? { clickType: 'DoubleClick' } : undefined,
      });
    };

    if (this.callbacks.readTreeSelection) {
      schedule(TREE_PENDING_ID, TREE_READ_DELAY_MS, async () => {
        this.pending = null;
        try {
          const path = await this.callbacks.readTreeSelection!(treeRef);
          const pathStr = Array.isArray(path) ? path.join(';') : '';
          doEmit(pathStr);
        } catch {
          doEmit('');
        }
      });
      this.pending = { treeRef, ts, clickType };
    } else {
      doEmit('');
    }
  }

  /** Called when agent sends tree path (e.g. from Java agent step). */
  commitPath(ts: number, treeRef: ObjectRef | undefined, pathStr: string): void {
    cancel(TREE_PENDING_ID);
    this.pending = null;
    if (!treeRef?.self) return;
    this.callbacks.onStep({
      keyword: 'SelectTreeList',
      objectRef: toStrictRef(treeRef),
      data: pathStr,
      ts,
    });
  }

  flush(): void {
    cancel(TREE_PENDING_ID);
    this.pending = null;
  }
}
