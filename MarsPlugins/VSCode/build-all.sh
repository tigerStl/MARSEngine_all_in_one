#!/usr/bin/env bash
# Build all: extension, java agent, C# ProcessInfo
# Run from repo root: ./build-all.sh

set -e
cd "$(dirname "$0")"

echo "=== 1/3 Extension (TypeScript) ==="
npm run compile

echo ""
echo "=== 2/3 Java Agent (Maven) ==="
cd java && mvn clean package -DskipTests && cd ..

echo ""
echo "=== 3/3 C# ProcessInfo (dotnet) ==="
cd ProcessInfo && dotnet publish -c Release && cd ..

echo ""
echo "=== Build all done ==="
