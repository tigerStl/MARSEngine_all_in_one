import type { ModuleCategory } from "./ModuleCategory";

export interface ScenarioTemplate {
  id: string;
  name: string;
  shortDescription: string;
  fullDescription: string;
  targetUsers: string[];
  recommendedModules: {
    [K in ModuleCategory]?: string[];
  };
  installNotes: string;
  futureNotes: string;
  complexity: "Low" | "Medium" | "High";
  resourceProfile: "Lightweight" | "Balanced" | "Enterprise";
  bestFor: string;
  level: "Beginner" | "Intermediate" | "Advanced";
}

