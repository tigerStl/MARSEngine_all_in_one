/**
 * Single TimerQueue for all delayed actions (dblclick, tree selection read, etc.).
 */

const timers = new Map<string, ReturnType<typeof setTimeout>>();

export function schedule(id: string, delayMs: number, fn: () => void): void {
  cancel(id);
  timers.set(
    id,
    setTimeout(() => {
      timers.delete(id);
      fn();
    }, delayMs)
  );
}

export function cancel(id: string): void {
  const t = timers.get(id);
  if (t) {
    clearTimeout(t);
    timers.delete(id);
  }
}

export function cancelAll(): void {
  for (const t of timers.values()) clearTimeout(t);
  timers.clear();
}
