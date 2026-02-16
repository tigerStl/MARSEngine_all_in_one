/**
 * Rule 5: EditSession – TextField/TextArea merge to single FillEdit on focusLost or Enter.
 * data = "{HOME}" + "{DEL}"*30 + finalText.
 */

import type { ObjectRef } from '../../protocol/javaAgentProtocol';
import { toStrictRef, sameTargetRef, type SemanticStep } from '../types';
import { isEditControl } from './recordFilter';

const DEL_PREFIX = '{HOME}' + '{DEL}'.repeat(30);

export interface EditSessionCallbacks {
  onStep: (step: SemanticStep) => void;
  readControlValue?: (ref: ObjectRef) => Promise<string>;
}

export class EditSession {
  private session: { targetRef: ObjectRef; startTs: number; keyCount: number } | null = null;
  private callbacks: EditSessionCallbacks;

  constructor(callbacks: EditSessionCallbacks) {
    this.callbacks = callbacks;
  }

  onFocusGained(ts: number, targetRef: ObjectRef): void {
    if (!targetRef?.self || !isEditControl(targetRef)) return;
    this.session = { targetRef, startTs: ts, keyCount: 0 };
  }

  onKey(_ts: number, _targetRef: ObjectRef | undefined): void {
    if (this.session) this.session.keyCount += 1;
  }

  async onFocusLost(ts: number, currentTarget: ObjectRef | undefined): Promise<void> {
    if (!this.session) return;
    if (currentTarget && !sameTargetRef(this.session.targetRef, currentTarget)) return;
    await this.finalize(ts, 'blur');
  }

  async onEnter(ts: number, targetRef: ObjectRef | undefined): Promise<void> {
    if (!this.session) return;
    if (targetRef && !sameTargetRef(this.session.targetRef, targetRef)) return;
    await this.finalize(ts, 'enter');
  }

  private async finalize(ts: number, reason: string): Promise<void> {
    const s = this.session;
    this.session = null;
    if (!s) return;

    let finalText = '';
    if (this.callbacks.readControlValue) {
      try {
        finalText = await this.callbacks.readControlValue(s.targetRef);
      } catch {
        finalText = '';
      }
    }

    if (s.keyCount === 0 && !finalText) return;

    this.callbacks.onStep({
      keyword: 'FillEdit',
      objectRef: toStrictRef(s.targetRef),
      data: DEL_PREFIX + finalText,
      ts,
      meta: reason === 'enter' ? { emitEnter: false } : undefined,
    });
  }

  /** Called when agent sends final text (e.g. from focusLost handler). */
  commitFinalText(ts: number, targetRef: ObjectRef | undefined, finalText: string): void {
    if (!this.session) return;
    if (targetRef && !sameTargetRef(this.session.targetRef, targetRef)) return;
    this.callbacks.onStep({
      keyword: 'FillEdit',
      objectRef: toStrictRef(this.session.targetRef),
      data: DEL_PREFIX + finalText,
      ts,
    });
    this.session = null;
  }

  flush(): void {
    this.session = null;
  }
}
