export interface LoggerService {
  info(message: string): void;
  warn(message: string): void;
  error(message: string, error?: unknown): void;
  getRecentLogs(limit?: number): string[];
}

