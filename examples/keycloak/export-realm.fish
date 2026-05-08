#!/usr/bin/fish

# --- Configuration ---
set CONTAINER_NAME "lighthouse-keycloak"
set REALM_NAME "Lighthouse"
set OUTPUT_FILE "realm-export.json"
set TEMP_DATA_DIR "./temp-keycloak-data"

# 1. Check if the container exists
if not docker ps -a --format '{{.Names}}' | grep -q "^$CONTAINER_NAME\$"
    echo (set_color red)"Error: Container '$CONTAINER_NAME' not found."(set_color normal)
    exit 1
end

# 2. Detect if it's currently running
set IS_RUNNING (docker inspect -f '{{.State.Running}}' $CONTAINER_NAME)

if test "$IS_RUNNING" = "true"
    echo (set_color yellow)"Container is running. Stopping it to release database locks..."(set_color normal)
    docker stop $CONTAINER_NAME
end

# 3. Extract the data and run the export
echo "Extracting database state..."
docker cp "$CONTAINER_NAME:/opt/keycloak/data" $TEMP_DATA_DIR

echo "Launching temporary export container..."
docker run --rm \
  -v (pwd)/$TEMP_DATA_DIR:/opt/keycloak/data \
  -v (pwd):/export \
  quay.io/keycloak/keycloak:latest \
  export \
  --realm $REALM_NAME \
  --users same_file \
  --file /export/$OUTPUT_FILE

# 4. Cleanup and Restore
echo "Cleaning up temporary files..."
rm -rf $TEMP_DATA_DIR

if test "$IS_RUNNING" = "true"
    echo (set_color green)"Restarting $CONTAINER_NAME..."(set_color normal)
    docker start $CONTAINER_NAME
end

echo (set_color blue --bold)"✔ Export complete: $OUTPUT_FILE"(set_color normal)