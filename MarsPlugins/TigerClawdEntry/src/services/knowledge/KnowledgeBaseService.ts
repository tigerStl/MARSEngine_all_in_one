import type { ModuleCategoryInfo } from "../../models/ModuleCategoryInfo";
import type { ModuleCandidate } from "../../models/ModuleCandidate";
import type { ModuleCategory } from "../../models/ModuleCategory";

export interface KnowledgeBaseService {
  getAllCategories(): ModuleCategoryInfo[];
  getCategory(category: ModuleCategory): ModuleCategoryInfo | undefined;
  getCandidateById(id: string): ModuleCandidate | undefined;
}

