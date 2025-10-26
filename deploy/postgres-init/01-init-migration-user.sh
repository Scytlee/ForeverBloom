#!/bin/sh
# postgres-init/01-init-migration-user.sh
# This script creates the migration user and grants it necessary permissions.

# Exit immediately if a command exits with a non-zero status.
set -e

# Basic check if variables seem set
if [ -z "$MIGRATION_USER" ]; then
  echo "Error: MIGRATION_USER environment variable is not set"
  exit 1
fi

if [ -z "$MIGRATION_PASSWORD" ]; then
  echo "Error: MIGRATION_PASSWORD environment variable is not set"
  exit 1
fi

if [ -z "$POSTGRES_DB" ]; then
  echo "Error: POSTGRES_DB environment variable is not set"
  exit 1
fi

if [ -z "$POSTGRES_USER" ]; then
  echo "Error: POSTGRES_USER environment variable is not set"
  exit 1
fi

echo "01-init-migration-user: Creating migration user with necessary permissions"

# Create the migration user and grant permissions
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Create the migration user
    CREATE ROLE "${MIGRATION_USER}" WITH LOGIN PASSWORD '${MIGRATION_PASSWORD}';

    -- Ensure the migration user can connect to the database
    GRANT CONNECT ON DATABASE "${POSTGRES_DB}" TO "${MIGRATION_USER}";

    -- Ensure the migration user can manage schemas
    GRANT CREATE ON DATABASE "${POSTGRES_DB}" TO "${MIGRATION_USER}";

    -- Ensure the migration user can manage roles
    ALTER ROLE "${MIGRATION_USER}" CREATEROLE;

    GRANT CREATE ON SCHEMA public TO "${MIGRATION_USER}";

    -- Revoke connect to default databases from public role
    REVOKE CONNECT ON DATABASE postgres FROM PUBLIC;
    REVOKE CONNECT ON DATABASE template0 FROM PUBLIC;
    REVOKE CONNECT ON DATABASE template1 FROM PUBLIC;
EOSQL

echo "01-init-migration-user: Migration user created"
