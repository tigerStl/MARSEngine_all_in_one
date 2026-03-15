import type { KnowledgeBaseService } from "./KnowledgeBaseService";
import { MODULE_KNOWLEDGE_BASE } from "../../constants/moduleKnowledgeBase";
import type { ModuleCategoryInfo } from "../../models/ModuleCategoryInfo";
import type { ModuleCandidate } from "../../models/ModuleCandidate";
import type { ModuleCategory } from "../../models/ModuleCategory";

export class DefaultKnowledgeBaseService implements KnowledgeBaseService {
  private readonly byId: Map<string, ModuleCandidate>;

  constructor() {
    this.byId = new Map();
    for (const cat of MODULE_KNOWLEDGE_BASE) {
      for (const c of cat.candidates) {
        this.byId.set(c.id, c);
      }
    }
  }

  getAllCategories(): ModuleCategoryInfo[] {
    return MODULE_KNOWLEDGE_BASE;
  }

  getCategory(category: ModuleCategory): ModuleCategoryInfo | undefined {
    return MODULE_KNOWLEDGE_BASE.find(c => c.category === category);
  }

  getCandidateById(id: string): ModuleCandidate | undefined {
    return this.byId.get(id);
  }
}

