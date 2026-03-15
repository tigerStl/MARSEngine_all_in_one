import { exec } from "child_process";
import { promisify } from "util";

const pExec = promisify(exec);

export async function runCommand(
  command: string
): Promise<{ stdout: string; stderr: string }> {
  const { stdout, stderr } = await pExec(command, { encoding: "utf8" });
  return { stdout: stdout.trim(), stderr: stderr.trim() };
}

