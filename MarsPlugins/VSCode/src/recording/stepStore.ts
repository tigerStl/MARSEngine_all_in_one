/**
 * Single source of truth for recorded steps. Notify on change for UI sync.
 */

import type { RecordedStep } from '../protocol/javaAgentProtocol';

export interface StepStoreState {
  steps: RecordedStep[];
  selectedStepId: string | null;
  recordingState: 'idle' | 'recording';
}

const defaultState: StepStoreState = {
  steps: [],
  selectedStepId: null,
  recordingState: 'idle',
};

let state: StepStoreState = { ...defaultState };
const listeners: Array<(s: StepStoreState) => void> = [];

function notify() {
  const s = { ...state };
  listeners.forEach((l) => l(s));
}

export function getStepStoreState(): StepStoreState {
  return { ...state };
}

export function addStep(step: RecordedStep): void {
  state.steps = [...state.steps, step];
  notify();
}

export function setSteps(steps: RecordedStep[]): void {
  state.steps = steps;
  notify();
}

export function setSelectedStepId(id: string | null): void {
  state.selectedStepId = id;
  notify();
}

export function setRecordingState(recordingState: StepStoreState['recordingState']): void {
  state.recordingState = recordingState;
  notify();
}

export function clearSteps(): void {
  state.steps = [];
  state.selectedStepId = null;
  notify();
}

export function subscribe(listener: (s: StepStoreState) => void): () => void {
  listeners.push(listener);
  return () => {
    const i = listeners.indexOf(listener);
    if (i >= 0) listeners.splice(i, 1);
  };
}
