import type { ModuleCategoryInfo } from "../models/ModuleCategoryInfo";

export const MODULE_KNOWLEDGE_BASE: ModuleCategoryInfo[] = [
  // LLM
  {
    category: "LLM",
    purpose: "Model reasoning and text/code generation.",
    description:
      "LLM providers power all higher-level agent logic, code suggestions, and RAG reasoning.",
    candidates: [
      {
        id: "llm-openai-compatible",
        category: "LLM",
        name: "OpenAI Compatible API",
        typicalUsage:
          "Hosted API for chat, completion, and code models via OpenAI-style HTTP APIs.",
        strengths:
          "Mature ecosystem, wide model availability, strong tooling and SDK support.",
        whenToUse:
          "Use when you want managed infrastructure, predictable latency, and easy cloud integration.",
        tags: ["cloud", "managed"]
      },
      {
        id: "llm-anthropic-compatible",
        category: "LLM",
        name: "Anthropic Compatible API",
        typicalUsage:
          "Hosted API for safety-focused, high-quality reasoning models.",
        strengths:
          "Excellent for long-context tasks, reasoning-heavy agents, and safer outputs.",
        whenToUse:
          "Use for advanced coding agents, analysis, and scenarios needing high reliability.",
        tags: ["cloud", "anthropic"]
      },
      {
        id: "llm-deepseek-compatible",
        category: "LLM",
        name: "DeepSeek Compatible API",
        typicalUsage:
          "OpenAI-style API for DeepSeek models (code and chat).",
        strengths:
          "Strong code and reasoning, cost-effective, widely used in CN.",
        whenToUse:
          "Use for coding assistants and general chat with DeepSeek endpoints.",
        tags: ["cloud", "openai-compatible"]
      },
      {
        id: "llm-gemini",
        category: "LLM",
        name: "Gemini",
        typicalUsage:
          "Multimodal models for text, code, and media prompts via Google Cloud.",
        strengths:
          "Strong multimodal support for images and media pipelines.",
        whenToUse:
          "Use when workflows include media understanding or Google Cloud services.",
        tags: ["multimodal"]
      },
      {
        id: "llm-ollama",
        category: "LLM",
        name: "Ollama",
        typicalUsage: "Local model runner for desktop-friendly LLM experimentation.",
        strengths:
          "Local-first, private, easy to switch between multiple open models.",
        whenToUse:
          "Use when you want offline experimentation or tighter data control.",
        tags: ["local"]
      },
      {
        id: "llm-lmstudio",
        category: "LLM",
        name: "LM Studio",
        typicalUsage:
          "Desktop UI and runtime for local models with OpenAI-compatible APIs.",
        strengths:
          "Good UX for local models, supports many community models and presets.",
        whenToUse:
          "Use when developers prefer GUI-driven local LLM management.",
        tags: ["local"]
      },
      {
        id: "llm-vllm",
        category: "LLM",
        name: "vLLM",
        typicalUsage:
          "High-throughput inference server for open models deployed on your own infra.",
        strengths:
          "Optimized serving, good for production workloads and multi-tenant deployments.",
        whenToUse:
          "Use when you need scalable self-hosted inference infrastructure.",
        tags: ["self-hosted", "high-throughput"]
      }
    ]
  },
  // Agent
  {
    category: "Agent",
    purpose: "Task orchestration and tool-calling runtime.",
    description:
      "Agent runtimes coordinate calls to LLMs, tools, and memory to accomplish higher-level workflows.",
    candidates: [
      {
        id: "agent-langchain",
        category: "Agent",
        name: "LangChain",
        typicalUsage:
          "Library for building LLM apps with chains, tools, and memory.",
        strengths:
          "Large ecosystem, many integrations, flexible composition for RAG and agents.",
        whenToUse:
          "Use when you want a general-purpose toolkit for LLM workflows with moderate complexity.",
        tags: ["library", "python", "typescript"]
      },
      {
        id: "agent-langgraph",
        category: "Agent",
        name: "LangGraph",
        typicalUsage:
          "Graph-based orchestration for multi-step agents and workflows.",
        strengths:
          "Deterministic graphs, stateful agents, great for production-grade AI workflows.",
        whenToUse:
          "Use for coding assistants and automation where you need explicit flow control.",
        tags: ["graph", "orchestration"]
      },
      {
        id: "agent-autogen",
        category: "Agent",
        name: "AutoGen",
        typicalUsage: "Multi-agent collaboration and tool use.",
        strengths: "Supports agent teams and conversation-focused setups.",
        whenToUse:
          "Use when experimenting with multi-agent patterns and collaborative workflows.",
        tags: ["multi-agent"]
      },
      {
        id: "agent-crewai",
        category: "Agent",
        name: "CrewAI",
        typicalUsage: "Agent crews with roles and tasks.",
        strengths:
          "Opinionated structure for multi-role agents and projects.",
        whenToUse:
          "Use for automation scenarios like research or content production.",
        tags: ["multi-agent"]
      },
      {
        id: "agent-opendevin-style",
        category: "Agent",
        name: "OpenDevin-style runtime",
        typicalUsage:
          "Autonomous development agents controlling tools and the IDE environment.",
        strengths: "End-to-end agentic workflows for coding and debugging.",
        whenToUse:
          "Use for advanced autonomous development where agents drive the dev loop.",
        tags: ["autonomous", "dev"]
      },
      {
        id: "agent-simple-workflow",
        category: "Agent",
        name: "Simple Workflow Agent",
        typicalUsage: "Minimal orchestration over LLM + tools.",
        strengths:
          "Low complexity, easy to reason about, suitable for small teams.",
        whenToUse:
          "Use when starting with simple task flows without heavy frameworks.",
        tags: ["lightweight"]
      }
    ]
  },
  // Code
  {
    category: "Code",
    purpose: "IDE and workspace integration.",
    description:
      "Code integration defines where developers work and how agents interact with files and projects.",
    candidates: [
      {
        id: "code-vscode",
        category: "Code",
        name: "VS Code",
        typicalUsage: "Primary IDE with extensions and debugging.",
        strengths:
          "Huge ecosystem, robust debugging, works well with extension-based agents.",
        whenToUse:
          "Use for general-purpose development and extension-hosted AI tools.",
        tags: ["ide"]
      },
      {
        id: "code-cursor",
        category: "Code",
        name: "Cursor",
        typicalUsage: "AI-native editor based on VS Code.",
        strengths:
          "Built-in AI pairing, agent integration, strong code understanding.",
        whenToUse:
          "Use when focusing on AI-assisted coding workflows.",
        tags: ["ide", "ai-native"]
      },
      {
        id: "code-oss",
        category: "Code",
        name: "Code OSS",
        typicalUsage: "Open-source core of VS Code.",
        strengths:
          "Custom distributions with more control over licensing and packaging.",
        whenToUse:
          "Use for custom IDE distributions or constrained environments.",
        tags: ["open-source"]
      },
      {
        id: "code-cli",
        category: "Code",
        name: "CLI Mode",
        typicalUsage:
          "Terminal-only workflows using editors like vim, nano, or CLI tools.",
        strengths: "Resource-light, scriptable, server-friendly.",
        whenToUse:
          "Use for remote servers or automation-first setups.",
        tags: ["cli"]
      },
      {
        id: "code-jetbrains-future",
        category: "Code",
        name: "JetBrains (future)",
        typicalUsage: "Future integration point with JetBrains IDEs.",
        strengths: "Deep language tooling and refactoring support.",
        whenToUse:
          "Planned for teams standardizing on JetBrains tools.",
        tags: ["future"]
      }
    ]
  },
  // Vector DB
  {
    category: "VectorDB",
    purpose: "Semantic search and retrieval memory.",
    description:
      "Vector databases store embeddings for documents, code, and events used in RAG and memory.",
    candidates: [
      {
        id: "vectordb-faiss",
        category: "VectorDB",
        name: "FAISS",
        typicalUsage: "In-process vector index library.",
        strengths: "Very fast retrieval, great for single-node or embedded use.",
        whenToUse:
          "Use for local experiments and small-to-medium datasets.",
        tags: ["lightweight", "library"]
      },
      {
        id: "vectordb-chroma",
        category: "VectorDB",
        name: "Chroma",
        typicalUsage: "Developer-friendly vector DB for RAG apps.",
        strengths: "Simple APIs, local and server modes, good for prototypes.",
        whenToUse:
          "Use for coding assistants and small RAG systems.",
        tags: ["lightweight"]
      },
      {
        id: "vectordb-sqlite-vec",
        category: "VectorDB",
        name: "SQLite-Vec",
        typicalUsage: "Vector search on top of SQLite.",
        strengths:
          "Zero-ops, file-based DB, great for desktop and embedded apps.",
        whenToUse:
          "Use when you want minimal infra and simple deployment.",
        tags: ["lightweight", "embedded"]
      },
      {
        id: "vectordb-pgvector",
        category: "VectorDB",
        name: "pgvector",
        typicalUsage: "PostgreSQL extension for vector search.",
        strengths:
          "Integrates with relational data and existing Postgres clusters.",
        whenToUse:
          "Use when apps already rely on Postgres.",
        tags: ["balanced"]
      },
      {
        id: "vectordb-milvus",
        category: "VectorDB",
        name: "Milvus",
        typicalUsage: "Distributed vector database.",
        strengths:
          "Scales to large collections and high QPS.",
        whenToUse:
          "Use for production RAG serving large corpora.",
        tags: ["enterprise"]
      },
      {
        id: "vectordb-weaviate",
        category: "VectorDB",
        name: "Weaviate",
        typicalUsage: "Cloud and self-hosted vector database.",
        strengths: "Schema, hybrid search, and cloud-native features.",
        whenToUse:
          "Use for enterprise RAG with rich metadata needs.",
        tags: ["enterprise"]
      },
      {
        id: "vectordb-pinecone",
        category: "VectorDB",
        name: "Pinecone",
        typicalUsage: "Hosted vector DB as a service.",
        strengths: "Managed infra, high uptime, simple scaling.",
        whenToUse:
          "Use when you want fully managed vector search.",
        tags: ["managed", "enterprise"]
      }
    ]
  },
  // Tools
  {
    category: "Tools",
    purpose: "Execution layer tools.",
    description:
      "Tools let agents interact with files, terminals, version control, tests, browsers, databases, media, and future MARS tooling.",
    candidates: [
      {
        id: "tools-file",
        category: "Tools",
        name: "File tools",
        typicalUsage: "Read, write, and refactor files.",
        strengths:
          "Core for any coding assistant or RAG ingestion pipeline.",
        whenToUse:
          "Always enabled for code editing and document pipelines.",
        tags: ["core"]
      },
      {
        id: "tools-shell",
        category: "Tools",
        name: "Shell tools",
        typicalUsage: "Run commands, scripts, and build steps.",
        strengths:
          "Essential for automation, builds, and running tests.",
        whenToUse:
          "Use for any scenario that needs CLI integration.",
        tags: ["core"]
      },
      {
        id: "tools-git",
        category: "Tools",
        name: "Git tools",
        typicalUsage: "Commit, diff, and branch management.",
        strengths:
          "Required for agent-driven development and code review.",
        whenToUse:
          "Use when agents interact with repositories.",
        tags: ["core"]
      },
      {
        id: "tools-test",
        category: "Tools",
        name: "Test tools",
        typicalUsage: "Run unit, integration, and end-to-end tests.",
        strengths:
          "Critical for regression detection and refactoring safety.",
        whenToUse:
          "Use for quality-focused workflows and CI-like checks.",
        tags: ["quality"]
      },
      {
        id: "tools-browser",
        category: "Tools",
        name: "Browser tools",
        typicalUsage:
          "Drive headless browsers for web testing or scraping.",
        strengths: "Enables E2E testing and web automation.",
        whenToUse:
          "Use for automation and front-end verification.",
        tags: ["automation"]
      },
      {
        id: "tools-database",
        category: "Tools",
        name: "Database tools",
        typicalUsage: "Run queries and migrations.",
        strengths:
          "Makes agents aware of application data and schema.",
        whenToUse:
          "Use when agents manage schema or inspect production-like data.",
        tags: ["data"]
      },
      {
        id: "tools-media",
        category: "Tools",
        name: "Media tools",
        typicalUsage:
          "Convert, transcode, and analyse audio / video / images.",
        strengths:
          "Supports media pipelines and multimodal prompts.",
        whenToUse:
          "Use for media workflows and content pipelines.",
        tags: ["media"]
      },
      {
        id: "tools-mars-future",
        category: "Tools",
        name: "Future MARS tools",
        typicalUsage:
          "Placeholder for MARS-specific runtimes and tools.",
        strengths:
          "Dedicated integration surface for the MARS ecosystem.",
        whenToUse:
          "Reserved for future TigerClawdEntry ↔ MARS runtime integration.",
        tags: ["future", "mars"]
      }
    ]
  },
  // MARS (future)
  {
    category: "MARS",
    purpose: "Future MARS runtime integration.",
    description:
      "V1 category reserved for MARS-specific orchestration, tools, and test automation.",
    candidates: [
      {
        id: "mars-runtime-placeholder",
        category: "MARS",
        name: "MARS Runtime (placeholder)",
        typicalUsage:
          "Represents future MARS runtime and test automation engine.",
        strengths:
          "Designed to host MARS agents, tools, and deep IDE integrations.",
        whenToUse:
          "Future path for advanced test automation scenarios.",
        tags: ["future", "mars"]
      }
    ]
  }
];

