#!/bin/bash

REPO="johnpc/jellyfin-plugin-smart-collections"
OUTPUT_FILE="manifest.json"
GITHUB_API_URL="https://api.github.com/repos/$REPO/releases"

echo "Fetching releases from $REPO..."
RELEASES=$(curl -s "$GITHUB_API_URL")

if [[ $RELEASES == *"API rate limit exceeded"* ]]; then
  echo "Error: GitHub API rate limit exceeded. Try again later or use a token."
  exit 1
fi

echo "Processing releases..."
RELEASE_TAGS=$(echo "$RELEASES" | grep -o '"tag_name": "[^"]*' | sed 's/"tag_name": "//')

FIRST_TAG=$(echo "$RELEASE_TAGS" | head -n 1)
FIRST_MANIFEST_URL="https://github.com/$REPO/releases/download/$FIRST_TAG/manifest.json"
FIRST_MANIFEST=$(curl -s -L -H "Accept: application/json" "$FIRST_MANIFEST_URL")

if [[ -z "$FIRST_MANIFEST" || "$FIRST_MANIFEST" == "Not Found" ]]; then
  echo "Error: Could not fetch first manifest from $FIRST_MANIFEST_URL"
  exit 1
fi

BASE_MANIFEST=$(echo "$FIRST_MANIFEST" | jq '.[0] | del(.versions)')

ALL_VERSIONS="[]"
for TAG in $RELEASE_TAGS; do
  echo "Processing release $TAG..."
  MANIFEST_URL="https://github.com/$REPO/releases/download/$TAG/manifest.json"
  
  RELEASE_MANIFEST=$(curl -s -L -H "Accept: application/json" "$MANIFEST_URL")
  
  if [[ -z "$RELEASE_MANIFEST" || "$RELEASE_MANIFEST" == "Not Found" ]]; then
    echo "Warning: Could not fetch manifest for $TAG, skipping..."
    continue
  fi
  
  VERSION_ENTRY=$(echo "$RELEASE_MANIFEST" | jq '.[0].versions[0]')
  
  if [[ "$VERSION_ENTRY" == "null" || -z "$VERSION_ENTRY" ]]; then
    echo "Warning: No valid version entry found in manifest for $TAG, skipping..."
    continue
  fi
  
  if [[ "$ALL_VERSIONS" == "[]" ]]; then
    ALL_VERSIONS="[$VERSION_ENTRY]"
  else
    ALL_VERSIONS=$(echo "$ALL_VERSIONS" | jq ". + [$VERSION_ENTRY]")
  fi
done

echo "Sorting versions..."
SORTED_VERSIONS=$(echo "$ALL_VERSIONS" | jq 'sort_by(.version | split(".") | map(tonumber)) | reverse')

FINAL_MANIFEST=$(echo "$BASE_MANIFEST" | jq --argjson versions "$SORTED_VERSIONS" '. + {versions: $versions}')
FINAL_MANIFEST="[$FINAL_MANIFEST]"

echo "$FINAL_MANIFEST" | jq '.' > "$OUTPUT_FILE"

echo "Generated manifest.json with $(echo "$SORTED_VERSIONS" | jq 'length') versions"
