/**
 * Rule 8: MenuSelectionSession – only final MenuItem click => SelectMenuItem.
 * data = "Item;SubMenu;RootMenu" (item to root).
 */

import type { ObjectRef } from '../../protocol/javaAgentProtocol';
import { toStrictRef, type SemanticStep } from '../types';
import { isMenuItem, isMenuBar } from './recordFilter';

export interface MenuSelectionSessionCallbacks {
  onStep: (step: SemanticStep) => void;
  readMenuPath?: (itemRef: ObjectRef) => Promise<string[]>;
}

export class MenuSelectionSession {
  private callbacks: MenuSelectionSessionCallbacks;

  constructor(callbacks: MenuSelectionSessionCallbacks) {
    this.callbacks = callbacks;
  }

  onMenuItemClick(ts: number, itemRef: ObjectRef, menuBarOrRootRef: ObjectRef, pathItems: string[]): void {
    if (!itemRef?.self || !isMenuItem(itemRef)) return;

    const data = Array.isArray(pathItems) && pathItems.length ? pathItems.join(';') : (itemRef.self?.javaName ?? '');
    this.callbacks.onStep({
      keyword: 'SelectMenuItem',
      objectRef: toStrictRef(menuBarOrRootRef),
      data,
      ts,
      meta: { clickType: 'Click' },
    });
  }

  /** When agent sends menu path (e.g. from Java). */
  commitMenuPath(ts: number, menuBarRef: ObjectRef | undefined, pathStr: string): void {
    if (!menuBarRef?.self || !isMenuBar(menuBarRef)) return;
    this.callbacks.onStep({
      keyword: 'SelectMenuItem',
      objectRef: toStrictRef(menuBarRef),
      data: pathStr,
      ts,
    });
  }

  flush(): void {}
}
