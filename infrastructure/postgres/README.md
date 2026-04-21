# Pseudo Markets PostgreSQL Infrastructure

This folder is reserved for shared PostgreSQL infrastructure assets used by the Pseudo Markets NextGen platform.

The platform-local Docker stack creates the shared database as `pseudomarkets_db`. EF Core migrations for this database currently live in `pseudomarkets-nextgen-shared-entities`, and the transaction processing service applies them at startup.

Examples of files that can live here in the future:

- initialization scripts
- seed scripts
- shared migration helpers
- backup or restore helpers

The current implementation does not require shared PostgreSQL scripts yet, so this folder is currently just a reserved location for future platform-level Postgres assets.

## Local Data Note

Docker only applies `POSTGRES_DB` when the data directory is initialized for the first time. If an existing local `./.docker-data/postgres` directory was created before the platform database was renamed to `pseudomarkets_db`, recreate the local Postgres data directory or manually create `pseudomarkets_db` in that existing instance.
