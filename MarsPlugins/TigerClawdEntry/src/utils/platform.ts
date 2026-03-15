export function getOS(): string {
  const platform = process.platform;
  if (platform === "win32") return "Windows";
  if (platform === "darwin") return "macOS";
  return "Linux";
}

