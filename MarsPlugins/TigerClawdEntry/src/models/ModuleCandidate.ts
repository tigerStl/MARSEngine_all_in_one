import type { ModuleCategory } from "./ModuleCategory";

export interface ModuleCandidate {
  id: string;
  category: ModuleCategory;
  name: string;
  typicalUsage: string;
  strengths: string;
  whenToUse: string;
  tags?: string[];
}

