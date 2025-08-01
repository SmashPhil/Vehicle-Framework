#!/usr/bin/env bash

# Usage: ./ExtractTranslations.sh 1 6

set -euo pipefail

MAJOR=$1
MINOR=$2
ZIP_URL="https://script.google.com/macros/s/AKfycbyVEQ30eVxP-zVWqGXuiPfuVEZYtJp4PYWuArJZxNiSxthxX3FqYssAvUDsrVPQuHt6/exec"
ZIP_PATH="translations.zip"
FOLDER="${MAJOR}.${MINOR}/Languages/English/Keyed"

cd "$(dirname "$0")/.."
if [[ ! -d "$FOLDER" ]]; then
  echo "Folder $FOLDER not found" >&2
  exit 1
fi

echo "Extracting translation files."
URL=$(curl -sL "$ZIP_URL")
URL+="&confirm=t"
rm -rf "$FOLDER"/*
curl -L "$URL" -o "$ZIP_PATH"

# Check if downloaded file is google sign-in page
if head -n 1 "$ZIP_PATH" | grep -q '<!DOCTYPE'; then
  cp "$ZIP_PATH" dump.html
  echo "File returned HTML (likely Google sign-in page). Dumped as dump.html" >&2
  exit 1
fi

echo "Unpacking zip"
unzip -o "$ZIP_PATH" -d "$FOLDER"
rm -f "$ZIP_PATH"