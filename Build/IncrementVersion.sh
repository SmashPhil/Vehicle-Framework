#!/usr/bin/env bash

# Usage: ./IncrementVersion.sh major minor start-date outputRevision outputVersionTxt
# Example: ./IncrementVersion.sh 1 6 07-DEC-2019 false true
set -euo pipefail

MAJOR="$1"
MINOR="$2"
START_DATE="${3:-07-DEC-2019}"
USE_REVISION="${4:-false}"
VERSION_FILE="${5:-false}"

CREATE_VERSION_FILE=false
if [[ "$VERSION_FILE" == "true" ]]; then
  CREATE_VERSION_FILE=true
fi

REVISION=false
if [[ "$USE_REVISION" == "true" ]]; then
  REVISION=true
fi

BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%S")
START_DATE_EPOCH=$(date -d "$START_DATE" +%s)
BUILD_DATE_EPOCH=$(date -d "$BUILD_DATE" +%s)

function get_version_number() {
  local delta_days=$(( (BUILD_DATE_EPOCH - START_DATE_EPOCH) / 86400 ))
  printf "%d.%d.%04d" "$MAJOR" "$MINOR" "$delta_days"
}

function get_version_with_revision() {
  local now=$(date -u)
  local seconds_since_midnight=$(date -u -d "$now" +%s)
  local midnight=$(date -u -d "$(date -u +%F) 00:00:00" +%s)
  local rev=$(( (seconds_since_midnight - midnight) / 2 ))
  printf "%s rev%03d" "$(get_version_number)" "${rev:0:5}"
}

VERSION=$(get_version_number)
VERSION_WITH_REVISION="$VERSION"
if [[ "$REVISION" == true ]]; then
  VERSION_WITH_REVISION=$(get_version_with_revision)
fi

echo "Version: $VERSION_WITH_REVISION"
cd "$(dirname "$0")/.."
ABOUT_FILE_PATH="$(pwd)/About/About.xml"
VERSION_FILE_PATH="$(pwd)/Version.txt"

if [[ "$CREATE_VERSION_FILE" == true ]]; then
  echo "Updating Version.txt"
  echo -n "$VERSION" > "$VERSION_FILE_PATH"
fi

# Replace <modVersion> in About.xml
if [[ -f "$ABOUT_FILE_PATH" ]]; then
  sed -i "s|<modVersion>.*</modVersion>|<modVersion>$VERSION_WITH_REVISION</modVersion>|g" "$ABOUT_FILE_PATH"
  echo "Updating About.xml"
else
  echo "About.xml not found at $ABOUT_FILE_PATH" >&2
  exit 1
fi