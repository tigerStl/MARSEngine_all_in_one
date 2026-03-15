#!/usr/bin/env bash
set -e

echo "[Prereq] Checking for Node.js and npm..."

if command -v node >/dev/null 2>&1 && command -v npm >/dev/null 2>&1; then
  node -v
  npm -v
  echo "[Prereq] Node.js and npm are available."
  exit 0
fi

echo "[Prereq] Node.js or npm not found."

if command -v brew >/dev/null 2>&1; then
  echo "[Prereq] Installing Node.js via Homebrew..."
  brew install node
  echo "[Prereq] Done. Restart the terminal if npm is not in PATH."
  exit 0
fi

echo "[Prereq] Please install Node.js manually: https://nodejs.org"
echo "[Prereq] Or on macOS/Linux: brew install node"
exit 1
