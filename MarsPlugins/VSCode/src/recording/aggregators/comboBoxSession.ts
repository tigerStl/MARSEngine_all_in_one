/**
 * Rule 1: ComboBoxSession – no click steps; output SelectDropList on focusLost/selectionChanged/Enter.
 * data = selected value.
 */

import type { ObjectRef } from '../../protocol/javaAgentProtocol';
import { toStrictRef, sameTargetRef, type SemanticStep } from '../types';
import { isComboBox, isEditableComboEditor } from './recordFilter';

export interface ComboBoxSessionCallbacks {
  onStep: (step: SemanticStep) => void;
  readControlValue?: (ref: ObjectRef) => Promise<string>;
}

export class ComboBoxSession {
  private session: {
    targetRef: ObjectRef;
    startTs: number;
    initialValue: string;
    interacted: boolean;
  } | null = null;
  private callbacks: ComboBoxSessionCallbacks;

  constructor(callbacks: ComboBoxSessionCallbacks) {
    this.callbacks = callbacks;
  }

  onFocusGained(ts: number, targetRef: ObjectRef): void {
    if (!targetRef?.self) return;
    if (!isComboBox(targetRef) && !isEditableComboEditor(targetRef)) return;
    const ref = isComboBox(targetRef) ? targetRef : this.findComboBoxParent(targetRef);
    if (!ref) return;
    this.session = {
      targetRef: ref,
      startTs: ts,
      initialValue: '',
      interacted: false,
    };
    if (this.callbacks.readControlValue) {
      this.callbacks.readControlValue(ref).then((v) => {
        if (this.session && sameTargetRef(this.session.targetRef, ref)) this.session.initialValue = v ?? '';
      });
    }
  }

  onComboClick(): void {
    if (this.session) this.session.interacted = true;
  }

  private findComboBoxParent(_ref: ObjectRef): ObjectRef | null {
    const parent = _ref.parent;
    if (!parent) return null;
    const javaType = (parent.javaType ?? '') as string;
    if (!javaType.includes('ComboBox')) return null;
    return { parent: null, self: parent };
  }

  async onFocusLost(ts: number, currentTarget: ObjectRef | undefined): Promise<void> {
    if (!this.session) return;
    if (currentTarget && !sameTargetRef(this.session.targetRef, currentTarget)) return;
    await this.finalize(ts);
  }

  onSelectionChanged(ts: number, targetRef: ObjectRef | undefined, selectedText: string): void {
    if (!this.session || !targetRef) return;
    if (!sameTargetRef(this.session.targetRef, targetRef)) return;
    this.finalizeSync(ts, selectedText);
  }

  async onEnter(ts: number, targetRef: ObjectRef | undefined): Promise<void> {
    if (!this.session) return;
    if (targetRef && !sameTargetRef(this.session.targetRef, targetRef)) return;
    await this.finalize(ts);
  }

  private async finalize(ts: number): Promise<void> {
    const s = this.session;
    this.session = null;
    if (!s) return;

    let finalValue = '';
    if (this.callbacks.readControlValue) {
      try {
        finalValue = await this.callbacks.readControlValue(s.targetRef);
      } catch {
        finalValue = '';
      }
    }

    if (!s.interacted && finalValue === s.initialValue) return;

    this.callbacks.onStep({
      keyword: 'SelectDropList',
      objectRef: toStrictRef(s.targetRef),
      data: finalValue,
      ts,
    });
  }

  private finalizeSync(ts: number, selectedText: string): void {
    const s = this.session;
    this.session = null;
    if (!s) return;
    if (!s.interacted && selectedText === s.initialValue) return;
    this.callbacks.onStep({
      keyword: 'SelectDropList',
      objectRef: toStrictRef(s.targetRef),
      data: selectedText,
      ts,
    });
  }

  commitValue(ts: number, targetRef: ObjectRef | undefined, selectedText: string): void {
    if (!this.session) return;
    if (targetRef && !sameTargetRef(this.session.targetRef, targetRef)) return;
    this.finalizeSync(ts, selectedText);
  }

  flush(): void {
    if (!this.session) return;
    const s = this.session;
    this.session = null;
    if (this.callbacks.readControlValue) {
      this.callbacks.readControlValue(s.targetRef).then((v) => {
        if (!s.interacted && v === s.initialValue) return;
        this.callbacks.onStep({
          keyword: 'SelectDropList',
          objectRef: toStrictRef(s.targetRef),
          data: v ?? '',
          ts: s.startTs,
        });
      });
    } else if (s.interacted) {
      this.callbacks.onStep({
        keyword: 'SelectDropList',
        objectRef: toStrictRef(s.targetRef),
        data: s.initialValue,
        ts: s.startTs,
      });
    }
  }
}
