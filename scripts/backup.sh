#!/usr/bin/env bash
#
# Backs up the bot's SQLite database: takes a consistent snapshot of the live
# file, checks it and uploads it with rclone. Runs on the Docker host from
# root's crontab and needs sqlite3 and rclone there.
#
# BRANIK_BACKUP_REMOTE is required: the rclone remote and bucket to upload to,
# for example b2:my-bucket.

set -euo pipefail

REMOTE="${BRANIK_BACKUP_REMOTE:?Set BRANIK_BACKUP_REMOTE, for example b2:my-bucket}"
CONTAINER="${BRANIK_CONTAINER:-branik-bot}"
BACKUP_DIR="${BRANIK_BACKUP_DIR:-/var/backups/branik-bot}"
KEEP_DAYS="${BRANIK_BACKUP_KEEP_DAYS:-14}"

# The host folder behind the container's /app/data, whatever the volume is called.
data_dir="$(docker inspect --format \
    '{{range .Mounts}}{{if eq .Destination "/app/data"}}{{.Source}}{{end}}{{end}}' "$CONTAINER")"
database="$data_dir/branik.db"

if [[ ! -f "$database" ]]; then
    echo "Database not found at '$database'" >&2
    exit 1
fi

mkdir -p "$BACKUP_DIR"
snapshot="$BACKUP_DIR/branik-$(date +%F).db"
partial="$snapshot.partial"
trap 'rm -f "$partial"' EXIT

# .backup takes a read lock, so the copy is consistent even if the bot writes meanwhile.
sqlite3 "$database" ".timeout 5000" ".backup '$partial'"

check="$(sqlite3 "$partial" "PRAGMA integrity_check;")"
if [[ "$check" != "ok" ]]; then
    echo "Snapshot failed the integrity check: $check" >&2
    exit 1
fi

mv "$partial" "$snapshot"

# Only uploads, never deletes: the remote keeps every snapshot.
rclone copy "$snapshot" "$REMOTE"

find "$BACKUP_DIR" -name 'branik-*.db' -mtime +"$KEEP_DAYS" -delete

echo "Backed up $(basename "$snapshot") to $REMOTE"
