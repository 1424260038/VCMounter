#!/bin/bash
# build_vcmounter.sh - Build VCMounter.exe from VCMounter.cs
# Usage: bash build_vcmounter.sh
set -e

SDK="C:/Program Files/dotnet/sdk/9.0.305"
CSC="$SDK/Roslyn/bincore/csc.dll"
FW="C:/Windows/Microsoft.NET/Framework64/v4.0.30319"
SRC="G:/VeraCrypt/VCMounter.cs"
MANIFEST="G:/VeraCrypt/app.manifest"
OUT="G:/VeraCrypt/VCMounter_new.exe"

echo "[1/2] Compiling $SRC -> $OUT"
dotnet "$CSC" \
  -nologo \
  -target:winexe \
  -platform:anycpu \
  -out:"$OUT" \
  -win32manifest:"$MANIFEST" \
  -r:"$FW/mscorlib.dll" \
  -r:"$FW/System.dll" \
  -r:"$FW/System.Drawing.dll" \
  -r:"$FW/System.Windows.Forms.dll" \
  "$SRC"

echo "[2/2] Build OK: $OUT"
