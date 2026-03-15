import type { ScenarioTemplate } from "../models/ScenarioTemplate";

export const SCENARIO_TEMPLATES: ScenarioTemplate[] = [
  {
    id: "basic-coding-setup",
    name: "Basic Coding Setup",
    shortDescription: "Baseline configuration for LLM-assisted coding.",
    fullDescription:
      "A simple, low-friction configuration that enables LLM-based completions and basic tooling without extra infrastructure.",
    targetUsers: ["Individual developers", "Teams starting with AI coding"],
    recommendedModules: {
      LLM: ["llm-openai-compatible", "llm-anthropic-compatible"],
      Agent: ["agent-langchain", "agent-simple-workflow"],
      Code: ["code-vscode", "code-cursor"],
      Tools: ["tools-file", "tools-git", "tools-shell", "runtime-openclaw"]
    },
    installNotes:
      "Installs minimal modules focused on code editing. No vector database is required.",
    futureNotes:
      "Can later be upgraded to AI Coding Assistant or Autonomous Development scenarios.",
    complexity: "Low",
    resourceProfile: "Lightweight",
    bestFor: "coding",
    level: "Beginner"
  },
  {
    id: "ai-coding-assistant",
    name: "AI Coding Assistant",
    shortDescription: "Project-aware coding assistant with LangGraph and Chroma.",
    fullDescription:
      "A project-aware coding setup using LangGraph and a lightweight vector database for code context.",
    targetUsers: [
      "Developers building AI-first apps",
      "Teams adopting AI pair programming"
    ],
    recommendedModules: {
      LLM: ["llm-anthropic-compatible"],
      Agent: ["agent-langgraph"],
      Code: ["code-cursor"],
      VectorDB: ["vectordb-chroma"],
      Tools: ["tools-file", "tools-git", "tools-shell", "tools-test"]
    },
    installNotes:
      "Requires Chroma setup for embeddings and indexing the workspace codebase.",
    futureNotes:
      "Can later connect to a MARS runtime for deeper test automation and debugging.",
    complexity: "Medium",
    resourceProfile: "Balanced",
    bestFor: "coding",
    level: "Intermediate"
  },
  {
    id: "retrieval-knowledge-base",
    name: "Retrieval / Knowledge Base",
    shortDescription: "RAG and document Q&A with LangChain.",
    fullDescription:
      "A retrieval-augmented generation setup for document Q&A and knowledge base search.",
    targetUsers: ["Knowledge teams", "Support tooling", "Internal docs search"],
    recommendedModules: {
      LLM: ["llm-openai-compatible"],
      Agent: ["agent-langchain"],
      Code: ["code-vscode"],
      VectorDB: ["vectordb-chroma", "vectordb-milvus"],
      Tools: ["tools-file", "tools-database"]
    },
    installNotes:
      "Requires ingestion pipelines and embedding generation for documents.",
    futureNotes:
      "Can integrate with enterprise vector DBs and MARS test harness for regression of Q&A quality.",
    complexity: "Medium",
    resourceProfile: "Balanced",
    bestFor: "rag",
    level: "Intermediate"
  },
  {
    id: "video-media-workflow",
    name: "Video / Media Workflow",
    shortDescription: "Prompt generation and media pipelines.",
    fullDescription:
      "A multimodal pipeline for handling media prompts, transcription, and transformation.",
    targetUsers: ["Media teams", "Content operations", "Marketing automation"],
    recommendedModules: {
      LLM: ["llm-gemini", "llm-openai-compatible"],
      Agent: ["agent-simple-workflow"],
      Tools: ["tools-file", "tools-media"]
    },
    installNotes:
      "Requires access to media processing tools such as FFmpeg and cloud storage.",
    futureNotes:
      "Future MARS tasks can validate media pipelines and automate regression suites.",
    complexity: "Medium",
    resourceProfile: "Balanced",
    bestFor: "media",
    level: "Intermediate"
  },
  {
    id: "autonomous-development",
    name: "Autonomous Development",
    shortDescription: "Agent-driven development with OpenDevin-style runtime.",
    fullDescription:
      "A full agentic development environment where agents interact with the IDE, Git, and tests.",
    targetUsers: [
      "Advanced teams",
      "Research groups",
      "Automation-heavy organizations"
    ],
    recommendedModules: {
      LLM: ["llm-anthropic-compatible"],
      Agent: ["agent-opendevin-style"],
      Code: ["code-cursor"],
      VectorDB: ["vectordb-chroma"],
      Tools: [
        "tools-file",
        "tools-git",
        "tools-shell",
        "tools-test",
        "tools-browser"
      ]
    },
    installNotes:
      "Requires careful sandboxing and strong test suites; consider running against non-production repos.",
    futureNotes:
      "Future MARS runtimes can provide hardened, policy-aware execution for this scenario.",
    complexity: "High",
    resourceProfile: "Enterprise",
    bestFor: "automation",
    level: "Advanced"
  }
];

