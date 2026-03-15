import type { ModuleCategory } from "./ModuleCategory";
import type { ModuleCandidate } from "./ModuleCandidate";

export interface ModuleCategoryInfo {
  category: ModuleCategory;
  description: string;
  purpose: string;
  candidates: ModuleCandidate[];
}

