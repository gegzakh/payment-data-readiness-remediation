#!/bin/sh
# One database per microservice; add a line here when a new service is introduced.
set -eu

for db in pdr_releasenotes pdr_rules pdr_audit pdr_identity pdr_sources pdr_ingestion pdr_validation pdr_remediation pdr_notification; do
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<SQL
SELECT 'CREATE DATABASE $db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$db')\gexec
SQL
done
