/**
 * Locale-aware display order and display names for runtime modules.
 * EN: ChatGPT (OpenAI) first, then DeepSeek, etc.
 * ZH: DeepSeek first, then others.
 */

export type Locale = "en" | "zh";

/** Candidate id order per category per locale. First = preferred at top. */
export const MODULE_ORDER: Record<string, Record<Locale, string[]>> = {
  LLM: {
    en: [
      "llm-openai-compatible",   // ChatGPT
      "llm-deepseek-compatible",
      "llm-anthropic-compatible",
      "llm-gemini",
      "llm-ollama",
      "llm-lmstudio",
      "llm-vllm"
    ],
    zh: [
      "llm-deepseek-compatible", // DeepSeek first in Chinese
      "llm-openai-compatible",
      "llm-anthropic-compatible",
      "llm-gemini",
      "llm-ollama",
      "llm-lmstudio",
      "llm-vllm"
    ]
  },
  Agent: {
    en: [
      "agent-langchain",
      "agent-langgraph",
      "agent-simple-workflow",
      "agent-autogen",
      "agent-crewai",
      "agent-opendevin-style"
    ],
    zh: [
      "agent-langchain",
      "agent-langgraph",
      "agent-simple-workflow",
      "agent-autogen",
      "agent-crewai",
      "agent-opendevin-style"
    ]
  },
  Code: {
    en: ["code-vscode", "code-cursor", "code-oss", "code-cli", "code-jetbrains-future"],
    zh: ["code-vscode", "code-cursor", "code-oss", "code-cli", "code-jetbrains-future"]
  },
  VectorDB: {
    en: [
      "vectordb-chroma",
      "vectordb-faiss",
      "vectordb-sqlite-vec",
      "vectordb-pgvector",
      "vectordb-milvus",
      "vectordb-weaviate",
      "vectordb-pinecone"
    ],
    zh: [
      "vectordb-chroma",
      "vectordb-faiss",
      "vectordb-sqlite-vec",
      "vectordb-pgvector",
      "vectordb-milvus",
      "vectordb-weaviate",
      "vectordb-pinecone"
    ]
  },
  Tools: {
    en: [
      "tools-file",
      "tools-shell",
      "tools-git",
      "tools-test",
      "tools-browser",
      "tools-database",
      "tools-media",
      "tools-mars-future",
      "runtime-openclaw"
    ],
    zh: [
      "tools-file",
      "tools-shell",
      "tools-git",
      "tools-test",
      "tools-browser",
      "tools-database",
      "tools-media",
      "tools-mars-future",
      "runtime-openclaw"
    ]
  }
};

/** Display name overrides per locale (optional). Key = candidate id. */
export const MODULE_DISPLAY_NAMES: Record<Locale, Record<string, string>> = {
  en: {
    "llm-openai-compatible": "ChatGPT / OpenAI",
    "llm-deepseek-compatible": "DeepSeek",
    "llm-anthropic-compatible": "Claude / Anthropic"
  },
  zh: {
    "llm-openai-compatible": "OpenAI / ChatGPT",
    "llm-deepseek-compatible": "DeepSeek",
    "llm-anthropic-compatible": "Claude / Anthropic"
  }
};
