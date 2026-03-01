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
  parameter?: string;
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

function legacyIdentifierToObjectKey(id: Record<string, unknown> | undefined): Record<string, unknown> {
  if (!id || typeof id !== 'object') {
    return { javaName: '', javaType: '', index: 0 };
  }
  const key = { ...id } as Record<string, unknown>;
  if (key.javaName == null && key.name != null) key.javaName = key.name;
  if (key.javaType == null && key.type != null) key.javaType = key.type;
  if (key.index == null || typeof key.index !== 'number') key.index = 0;
  if (key.javaNamePath == null && Array.isArray(key.namePath)) key.javaNamePath = key.namePath;
  return key;
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
    parameter: step.parameter,
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
    const ts = (msg.timestamp ?? Date.now()) as number;
    const resolvedKeyword = typeof msg.keyword === 'string' && msg.keyword.trim()
      ? msg.keyword.trim()
      : keywordFromLegacyEvent(msg.event, msg.clickCount);
    if (!resolvedKeyword) return;

    const step: RecordedStepOutput = {
      id: 'step-' + (++this.stepId),
      keyword: resolvedKeyword,
      object: {
        parentKey: msg.parentIdentifier
          ? (legacyIdentifierToObjectKey(msg.parentIdentifier as Record<string, unknown>) as unknown as ObjectKey)
          : null,
        objectKey: legacyIdentifierToObjectKey(msg.objectIdentifier as Record<string, unknown>) as unknown as ObjectKey,
      },
      parameter: typeof msg.parameter === 'string' ? msg.parameter : '',
      data: typeof msg.data === 'string' ? msg.data : (typeof msg.content === 'string' ? msg.content : ''),
      meta: { ts },
    };
    this.callbacks.onStep(step);
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

function keywordFromLegacyEvent(eventName: string, clickCount?: number): string {
  const e = eventName;
  switch (e) {
    case 'click':
    case 'clickButton':
    case 'ClickButton':
      return clickCount === 2 ? 'DoubleClickButton' : 'ClickButton';
    case 'selectDropDown':
    case 'selectDropList':
    case 'SelectDropList':
      return 'SelectDropList';
    case 'selectMenuItem':
    case 'SelectMenuItem':
      return 'SelectMenuItem';
    case 'selectMenuIcon':
    case 'SelectMenuIcon':
      return 'SelectMenuIcon';
    case 'selectTreeList':
    case 'selectTreeNode':
    case 'SelectTreeList':
      return 'SelectTreeList';
    case 'selectTab':
    case 'SelectTab':
      return 'SelectTab';
    case 'searchAndUpdate':
    case 'SearchAndUpdate':
      return 'SearchAndUpdate';
    case 'searchAndClick':
    case 'SearchAndClick':
      return 'SearchAndClick';
    case 'selectPopupMenu':
    case 'SelectPopupMenu':
      return 'SelectPopupMenu';
    case 'fillEdit':
    case 'FillEdit':
      return 'FillEdit';
    case 'SetCheckBox':
      return 'SetCheckBox';
    case 'SetRadioBox':
      return 'SetRadioBox';
    case 'expandTreeNode':
    case 'collapseTreeNode':
    case 'ExpandTreeNode':
    case 'CollapseTreeNode':
      return 'SelectTreeList';
    default:
      return /^(Click|FillEdit|Select|Set|Search|Expand|Collapse|DoubleClick)/i.test(e) ? e : '';
  }
}
