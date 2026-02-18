/**
 * Unified recorder: agent events -> filter + aggregators -> semantic steps only.
 * Schema: { keyword, objectRef(parentKey+objectKey), data, ts, meta }. No bounds in identifiers.
 */

import type { ObjectRef, ObjectKey, BoundsRect } from '../protocol/javaAgentProtocol';
import type { RecordedStep } from '../protocol/javaAgentProtocol';
import type { CanonicalEvent, SemanticStep } from './types';
import { toStrictKey, toStrictRef } from './types';
import { cancelAll } from './aggregators/timerQueue';
import { shouldRecordClick, isComboBox, isTreeView, isMenuItem, isToolButton, isEditControl, isTab } from './aggregators/recordFilter';
import { ClickAggregator } from './aggregators/clickAggregator';
import { EditSession } from './aggregators/editSession';
import { ComboBoxSession } from './aggregators/comboBoxSession';
import { TreeSelectionAggregator } from './aggregators/treeSelectionAggregator';
import { ToolButtonAggregator } from './aggregators/toolButtonAggregator';
import { MenuSelectionSession } from './aggregators/menuSelectionSession';

export type RecordedStepOutput = RecordedStep;

export interface RecordingCallbacks {
  onStep: (step: RecordedStepOutput) => void;
  onSnapshotHint?: (windowId: string, rootNodeId: string) => void;
  readControlValue?: (ref: ObjectRef) => Promise<string>;
  readTreeSelection?: (treeRef: ObjectRef) => Promise<string[]>;
  readMenuPath?: (itemRef: ObjectRef) => Promise<string[]>;
}

/** Legacy agent message (e.g. from Java record agent). */
export interface LegacyRecordMessage {
  event: string;
  keyword?: string;
  parentIdentifier?: Record<string, unknown>;
  objectIdentifier?: Record<string, unknown>;
  data?: string;
  content?: string;
  timestamp?: number;
  clickCount?: number;
  [key: string]: unknown;
}

function identifierToObjectKey(id: Record<string, unknown> | undefined): ObjectKey {
  if (!id || typeof id !== 'object') return { javaName: '', javaType: '', index: 0 };
  const javaName = (id.name ?? id.accessibleName ?? id.javaName ?? '') as string;
  const javaType = (id.javaType ?? id.className ?? id.type ?? '') as string;
  const index = typeof id.index === 'number' ? id.index : 0;
  const javaNamePath = Array.isArray(id.javaNamePath) ? (id.javaNamePath as string[]) : undefined;
  return { javaName: String(javaName), javaType: String(javaType), index, javaNamePath };
}

function legacyToObjectRef(msg: LegacyRecordMessage): ObjectRef {
  const parent = msg.parentIdentifier ? identifierToObjectKey(msg.parentIdentifier as Record<string, unknown>) : null;
  const self = identifierToObjectKey(msg.objectIdentifier as Record<string, unknown>);
  return { parent, self };
}

function semanticToRecordedStep(step: SemanticStep, id: string): RecordedStepOutput {
  const debugBounds = step.meta?.debugBounds;
  return {
    id,
    keyword: step.keyword,
    object: {
      parentKey: step.objectRef.parent,
      objectKey: step.objectRef.self,
    },
    data: step.data,
    meta: {
      ts: step.ts,
      debugBounds,
      clickType: step.meta?.clickType,
      emitEnter: step.meta?.emitEnter,
      clickedTool: step.meta?.clickedTool,
    },
  };
}

export class RecordingEngine {
  private stepId = 0;
  private callbacks: RecordingCallbacks;
  private clickAggregator: ClickAggregator;
  private editSession: EditSession;
  private comboBoxSession: ComboBoxSession;
  private treeAggregator: TreeSelectionAggregator;
  private toolButtonAggregator: ToolButtonAggregator;
  private menuSession: MenuSelectionSession;

  constructor(callbacks: RecordingCallbacks) {
    this.callbacks = callbacks;

    const onSemanticStep = (step: SemanticStep) => {
      this.stepId += 1;
      this.callbacks.onStep(semanticToRecordedStep(step, 'step-' + this.stepId));
    };

    this.clickAggregator = new ClickAggregator({ onStep: onSemanticStep });
    this.editSession = new EditSession({
      onStep: onSemanticStep,
      readControlValue: callbacks.readControlValue,
    });
    this.comboBoxSession = new ComboBoxSession({
      onStep: onSemanticStep,
      readControlValue: callbacks.readControlValue,
    });
    this.treeAggregator = new TreeSelectionAggregator({
      onStep: onSemanticStep,
      readTreeSelection: callbacks.readTreeSelection,
    });
    this.toolButtonAggregator = new ToolButtonAggregator({ onStep: onSemanticStep });
    this.menuSession = new MenuSelectionSession({
      onStep: onSemanticStep,
      readMenuPath: callbacks.readMenuPath,
    });
  }

  /**
   * Accept canonical events (ui.mouse, ui.focus, ui.key) or legacy agent messages.
   * Priority: Menu > ToolButton > ComboBox > TreeView > Edit > Generic Click.
   */
  onAgentEvent(e: CanonicalEvent | LegacyRecordMessage | Record<string, unknown>): void {
    const ev = e as CanonicalEvent | LegacyRecordMessage;

    if (isCanonicalEvent(ev)) {
      this.onCanonicalEvent(ev);
      return;
    }

    if (isLegacyMessage(ev)) {
      this.onLegacyMessage(ev);
      return;
    }
  }

  private onCanonicalEvent(ev: CanonicalEvent): void {
    if (ev.kind === 'ui.mouse') {
      if (ev.type === 'click' || ev.type === 'dblclick') {
        const targetRef = ev.target;
        if (!shouldRecordClick(targetRef)) return;
        if (isMenuItem(targetRef)) {
          this.menuSession.onMenuItemClick(
            ev.ts,
            targetRef!,
            targetRef!,
            [targetRef?.self?.javaName ?? ''],
            ev.type === 'dblclick' ? 'DoubleClick' : 'Click'
          );
          return;
        }
        if (isToolButton(targetRef) && targetRef?.parent) {
          this.toolButtonAggregator.onToolButtonClick(ev.ts, targetRef, { parent: null, self: targetRef.parent }, {});
          return;
        }
        if (isComboBox(targetRef)) {
          this.comboBoxSession.onComboClick();
          return;
        }
        if (isTreeView(targetRef)) {
          this.treeAggregator.onTreeClick(ev.ts, targetRef, ev.type === 'dblclick' ? 'DoubleClick' : 'Click');
          return;
        }
        if (isEditControl(targetRef)) return;
        this.clickAggregator.onMouseClick(ev.ts, (ev as { x: number }).x, (ev as { y: number }).y, targetRef);
      }
      return;
    }
    if (ev.kind === 'ui.focus') {
      if (ev.type === 'focusGained') {
        this.editSession.onFocusGained(ev.ts, ev.target);
        this.comboBoxSession.onFocusGained(ev.ts, ev.target);
      } else {
        this.editSession.onFocusLost(ev.ts, ev.target);
        this.comboBoxSession.onFocusLost(ev.ts, ev.target);
      }
      return;
    }
    if (ev.kind === 'ui.key') {
      if (ev.type === 'char' && ev.key === 'Enter') {
        this.editSession.onEnter(ev.ts, ev.target);
        this.comboBoxSession.onEnter(ev.ts, ev.target);
      } else {
        this.editSession.onKey(ev.ts, ev.target!);
      }
    }
  }

  private onLegacyMessage(msg: LegacyRecordMessage): void {
    const ref = legacyToObjectRef(msg);
    const ts = (msg.timestamp ?? Date.now()) as number;

    switch (msg.event) {
      case 'click':
      case 'clickButton': {
        const metaClickCount = (msg as { meta?: { clickCount?: number } }).meta?.clickCount;
        const clickCount = (metaClickCount ?? msg.clickCount ?? 1) as number;
        const kw = (msg.keyword ?? '') as string;

        if (kw === 'SelectMenuItem') {
          this.menuSession.commitMenuPath(ts, ref, (msg.data as string) ?? '');
          return;
        }
        if (kw === 'SelectTreeNode' || kw === 'SelectTreeList') {
          this.treeAggregator.commitPath(ts, ref, (msg.data as string) ?? '');
          return;
        }
        if (kw === 'ClickMenuIcon' || kw === 'SelectMenuIcon') {
          const toolbarRef = ref.parent ? { parent: null, self: ref.parent } : ref;
          this.toolButtonAggregator.onToolButtonClick(ts, ref, toolbarRef, {
            text: msg.objectIdentifier?.text as string,
            caption: msg.objectIdentifier?.caption as string,
            tooltip: msg.objectIdentifier?.toolTipText as string,
            name: msg.objectIdentifier?.name as string,
          });
          return;
        }
        if (isComboBox(ref)) return;
        if (isTreeView(ref)) {
          this.treeAggregator.commitPath(ts, ref, (msg.data as string) ?? '');
          return;
        }
        if (isTab(ref)) {
          this.stepId += 1;
          this.callbacks.onStep(
            semanticToRecordedStep(
              {
                keyword: 'SelectTab',
                objectRef: toStrictRef(ref),
                data: (msg.data ?? msg.content ?? ref.self?.javaName ?? '') as string,
                ts,
              },
              'step-' + this.stepId
            )
          );
          return;
        }
        if (!shouldRecordClick(ref)) return;
        const keyword = clickCount === 2 || kw === 'DoubleClick' || kw === 'DoubleClickButton' ? 'DoubleClickButton' : 'ClickButton';
        this.stepId += 1;
        this.callbacks.onStep(
          semanticToRecordedStep(
            {
              keyword,
              objectRef: toStrictRef(ref),
              ts,
              meta: keyword === 'DoubleClickButton' ? { clickType: 'DoubleClick' } : undefined,
            },
            'step-' + this.stepId
          )
        );
        return;
      }
      case 'selectDropDown':
      case 'selectDropList': {
        this.stepId += 1;
        this.callbacks.onStep(
          semanticToRecordedStep(
            { keyword: 'SelectDropList', objectRef: toStrictRef(ref), data: (msg.data ?? msg.content ?? '') as string, ts },
            'step-' + this.stepId
          )
        );
        return;
      }
      case 'selectMenuItem': {
        this.menuSession.commitMenuPath(ts, ref, (msg.data as string) ?? '');
        return;
      }
      case 'selectMenuIcon': {
        const toolbarRef = ref.parent ? { parent: null, self: ref.parent } : ref;
        this.toolButtonAggregator.onToolButtonClick(ts, ref, toolbarRef, {
          text: msg.objectIdentifier?.text as string,
          caption: msg.objectIdentifier?.caption as string,
          tooltip: msg.objectIdentifier?.toolTipText as string,
          name: msg.objectIdentifier?.name as string,
        });
        return;
      }
      case 'selectTreeList': {
        this.treeAggregator.commitPath(ts, ref, (msg.data as string) ?? '');
        return;
      }
      case 'fillEdit': {
        const content = (msg.data ?? msg.content ?? '') as string;
        const fillEditData = '{HOME}' + '{DEL}'.repeat(30) + content;
        this.stepId += 1;
        this.callbacks.onStep(
          semanticToRecordedStep(
            { keyword: 'FillEdit', objectRef: toStrictRef(ref), data: fillEditData, ts },
            'step-' + this.stepId
          )
        );
        return;
      }
      case 'expandTreeNode':
      case 'collapseTreeNode': {
        this.treeAggregator.commitPath(ts, ref, (msg.data as string) ?? '');
        return;
      }
      default:
        break;
    }
  }

  /** Emit one semantic step (e.g. from legacy stream). Identifiers are stripped of bounds. */
  emitStep(keyword: string, objectRef: { parent: ObjectKey | null; self: ObjectKey }, data?: string, ts?: number, meta?: SemanticStep['meta']): void {
    this.stepId += 1;
    const step: RecordedStepOutput = {
      id: 'step-' + this.stepId,
      keyword,
      object: { parentKey: toStrictKey(objectRef.parent), objectKey: toStrictKey(objectRef.self) },
      data,
      meta: { ts: ts ?? Date.now(), debugBounds: meta?.debugBounds, clickType: meta?.clickType },
    };
    this.callbacks.onStep(step);
  }

  flush(): void {
    cancelAll();
    this.clickAggregator.flush();
    this.editSession.flush();
    this.comboBoxSession.flush();
    this.treeAggregator.flush();
    this.toolButtonAggregator.flush();
    this.menuSession.flush();
  }

  stop(): void {
    this.flush();
  }
}

function isCanonicalEvent(ev: unknown): ev is CanonicalEvent {
  const k = (ev as { kind?: string })?.kind;
  return k === 'ui.mouse' || k === 'ui.focus' || k === 'ui.key';
}

function isLegacyMessage(ev: unknown): ev is LegacyRecordMessage {
  return typeof ev === 'object' && ev !== null && 'event' in ev && typeof (ev as { event: unknown }).event === 'string';
}
