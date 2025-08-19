#!/usr/bin/env bash

# Usage: ./ExtractTranslations.sh 1.6

set -euo pipefail

VERSION=$1
ZIP_URL="https://script.google.com/macros/s/AKfycbyVEQ30eVxP-zVWqGXuiPfuVEZYtJp4PYWuArJZxNiSxthxX3FqYssAvUDsrVPQuHt6/exec"
ZIP_PATH="translations.zip"
SYNC_FILE=".localization"
FOLDER="${VERSION}/Languages/English/Keyed"

if [[ ! -d "$FOLDER" ]]; then
  echo "Folder $FOLDER not found" >&2
  exit 1
fi

echo "Checking localization for updates."
URL="$ZIP_URL"
# if [[ -f "$SYNC_FILE" ]]; then
#   echo "Sync file found. Checking for out of date translations."
#   LAST_SYNCED=$(<"$SYNC_FILE")
#   ENCODED=$(printf '%s' "$LAST_SYNCED" | sed -e 's/:/%3A/g' -e 's/+/%2B/g' -e 's/ /%20/g')
#   URL="${ZIP_URL}?lastSynced=${ENCODED}"
#   echo "ZipUrl=${URL}"
# fi

RESPONSE=$(curl -sL --fail "$URL")
ZIP_FILE_URL=$(echo "$RESPONSE" | sed -E 's/.*"url"[[:space:]]*:[[:space:]]*"([^"]*)".*/\1/')
LAST_UPDATED=$(echo "$RESPONSE" | sed -E 's/.*"lastUpdated"[[:space:]]*:[[:space:]]*"([^"]*)".*/\1/')
STATUS=$(echo "$RESPONSE" | sed -E 's/.*"status"[[:space:]]*:[[:space:]]*"([^"]*)".*/\1/')

if [[ "$STATUS" == "OK" ]]; then
  echo "Translations up to date. No changes needed."
  exit 0
fi

echo "Url = $ZIP_FILE_URL"
echo "Extracting translation files."
curl -sL "$ZIP_FILE_URL" -o "$ZIP_PATH"

HEADER=$(xxd -p -l 2 "$ZIP_PATH")
if [[ "$HEADER" != "504b" ]]; then
  echo "FAILED: Downloaded zip file which is not a zip. URL=$ZIP_FILE_URL"
  rm -f "$ZIP_PATH"
  exit 1
fi

find "$FOLDER" -type f -name '*.xml' -delete
echo "Unpacking zip in $FOLDER"
unzip -qo "$ZIP_PATH" -d "$FOLDER"
rm -f "$ZIP_PATH"

echo -n "$LAST_UPDATED" > "$SYNC_FILE"

echo "Translations downloaded."