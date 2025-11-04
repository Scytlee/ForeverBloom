#!/bin/sh
# postgres-init/01-init-foreverbloom.sh
# Bootstraps the ForeverBloom database: creates the database itself, the
# ForeverBloom migrator user, and the ForeverBloom user with the appropriate
# permissions. Intended to run once when the Postgres instance is provisioned.

set -eu

log() {
  echo "01-init-foreverbloom: $1"
}

escape_literal() {
  printf "%s" "$1" | sed "s/'/''/g"
}

escape_identifier() {
  printf "%s" "$1" | sed 's/\"/\"\"/g'
}

require_env() {
  name="$1"
  eval "value=\${${name}:-}"
  if [ -z "$value" ]; then
    echo "Error: $name environment variable is not set"
    exit 1
  fi
}

require_env "POSTGRES_USER"
require_env "FOREVERBLOOM_DATABASE_NAME"
require_env "FOREVERBLOOM_MIGRATOR_USERNAME"
require_env "FOREVERBLOOM_MIGRATOR_PASSWORD"
require_env "FOREVERBLOOM_USER_USERNAME"
require_env "FOREVERBLOOM_USER_PASSWORD"

fb_database_ident=$(escape_identifier "$FOREVERBLOOM_DATABASE_NAME")
fb_database_literal=$(escape_literal "$FOREVERBLOOM_DATABASE_NAME")
fb_migrator_ident=$(escape_identifier "$FOREVERBLOOM_MIGRATOR_USERNAME")
fb_migrator_literal=$(escape_literal "$FOREVERBLOOM_MIGRATOR_USERNAME")
fb_migrator_password_literal=$(escape_literal "$FOREVERBLOOM_MIGRATOR_PASSWORD")
fb_user_ident=$(escape_identifier "$FOREVERBLOOM_USER_USERNAME")
fb_user_literal=$(escape_literal "$FOREVERBLOOM_USER_USERNAME")
fb_user_password_literal=$(escape_literal "$FOREVERBLOOM_USER_PASSWORD")
postgres_user_ident=$(escape_identifier "$POSTGRES_USER")

log "Ensuring ForeverBloom database \"${FOREVERBLOOM_DATABASE_NAME}\" exists"
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<-EOSQL
  SELECT 'CREATE DATABASE "${fb_database_ident}" WITH OWNER "${postgres_user_ident}" TEMPLATE template0'
  WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = '${fb_database_literal}'
  )\gexec
EOSQL

log "Configuring ForeverBloom migrator user \"${FOREVERBLOOM_MIGRATOR_USERNAME}\" and ForeverBloom user \"${FOREVERBLOOM_USER_USERNAME}\""
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$FOREVERBLOOM_DATABASE_NAME" <<-EOSQL
  -- ========================================================================
  -- Create two separate database identities following principle of least privilege:
  -- 1. Migrator: Can modify schema (DDL) and data (DML) - used by DatabaseManager
  -- 2. Application user: Can only modify data (DML) - used by runtime application
  -- ========================================================================

  -- Create or update the migrator user (idempotent)
  DO \$\$
  BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${fb_migrator_literal}') THEN
      EXECUTE 'CREATE ROLE "${fb_migrator_ident}" WITH LOGIN PASSWORD ''${fb_migrator_password_literal}''';
    ELSE
      EXECUTE 'ALTER ROLE "${fb_migrator_ident}" WITH LOGIN PASSWORD ''${fb_migrator_password_literal}''';
    END IF;
  END
  \$\$;

  -- Create or update the application user (idempotent)
  DO \$\$
  BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${fb_user_literal}') THEN
      EXECUTE 'CREATE ROLE "${fb_user_ident}" WITH LOGIN PASSWORD ''${fb_user_password_literal}''';
    ELSE
      EXECUTE 'ALTER ROLE "${fb_user_ident}" WITH LOGIN PASSWORD ''${fb_user_password_literal}''';
    END IF;
  END
  \$\$;

  -- ========================================================================
  -- Migrator permissions: Full schema control (DDL + DML)
  -- ========================================================================
  GRANT CONNECT ON DATABASE "${fb_database_ident}" TO "${fb_migrator_ident}";
  GRANT CREATE, TEMPORARY ON DATABASE "${fb_database_ident}" TO "${fb_migrator_ident}";
  GRANT USAGE, CREATE ON SCHEMA public TO "${fb_migrator_ident}";

  -- ========================================================================
  -- Application user permissions: Data access only (DML)
  -- ========================================================================
  GRANT CONNECT ON DATABASE "${fb_database_ident}" TO "${fb_user_ident}";
  GRANT USAGE ON SCHEMA public TO "${fb_user_ident}";

  -- Grant access to existing tables and sequences
  GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "${fb_user_ident}";
  GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO "${fb_user_ident}";

  -- Ensure application user automatically gets access to future objects created by migrator
  ALTER DEFAULT PRIVILEGES FOR ROLE "${fb_migrator_ident}" IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO "${fb_user_ident}";
  ALTER DEFAULT PRIVILEGES FOR ROLE "${fb_migrator_ident}" IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO "${fb_user_ident}";

  -- ========================================================================
  -- Security lockdown: Deny public access to all databases
  -- ========================================================================
  REVOKE CONNECT ON DATABASE "${fb_database_ident}" FROM PUBLIC;
  REVOKE CONNECT ON DATABASE postgres FROM PUBLIC;
  REVOKE CONNECT ON DATABASE template0 FROM PUBLIC;
  REVOKE CONNECT ON DATABASE template1 FROM PUBLIC;
EOSQL

log "ForeverBloom database initialization completed"
