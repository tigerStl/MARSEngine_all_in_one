import type { AppState } from "./AppState";

export class StateManager {
  private state: AppState = { scenarios: [] };

  getState(): AppState {
    return this.state;
  }

  update(partial: Partial<AppState>): void {
    this.state = { ...this.state, ...partial };
  }
}

