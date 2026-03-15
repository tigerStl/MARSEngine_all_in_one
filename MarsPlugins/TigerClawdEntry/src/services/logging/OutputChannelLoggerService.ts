import * as vscode from "vscode";
import type { LoggerService } from "./LoggerService";
import { getStrings, resolveLocale } from "../../constants/locale";

export class OutputChannelLoggerService implements LoggerService {
  private readonly channel: vscode.OutputChannel;
  private readonly buffer: string[] = [];
  private readonly maxBuffer = 200;

  constructor() {
    this.channel = vscode.window.createOutputChannel("TigerClawdEntry");
  }

  info(message: string): void {
    this.log("INFO", message);
  }

  warn(message: string): void {
    this.log("WARN", message);
  }

  error(message: string, error?: unknown): void {
    const extra = error instanceof Error ? `: ${error.message}` : "";
    this.log("ERROR", `${message}${extra}`);
  }

  getRecentLogs(limit = 50): string[] {
    return this.buffer.slice(-limit);
  }

  private levelLabel(level: string): string {
    const lang = getStrings(resolveLocale(vscode.env.language));
    if (level === "ERROR") return lang.logLevelError;
    if (level === "WARN") return lang.logLevelWarn;
    return lang.logLevelInfo;
  }

  private log(level: string, message: string): void {
    const levelLabel = this.levelLabel(level);
    const line = `[${new Date().toISOString()}] [${levelLabel}] ${message}`;
    this.channel.appendLine(line);
    this.buffer.push(line);
    if (this.buffer.length > this.maxBuffer) {
      this.buffer.shift();
    }
  }
}

