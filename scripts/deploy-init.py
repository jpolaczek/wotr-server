#!/usr/bin/env python3
"""Create private Compose secrets once; never print credentials or overwrite them."""
from pathlib import Path
import os
import secrets
import shutil

ROOT = Path(__file__).resolve().parents[1]


def main():
    directory = ROOT / "infra/secrets"
    directory.mkdir(mode=0o700, parents=True, exist_ok=True)
    directory.chmod(0o700)
    password_file = directory / "db_password"
    connection_file = directory / "db_connection"
    admin_file = directory / "db_admin_password"
    if any(path.exists() for path in [password_file, connection_file, admin_file]):
        raise SystemExit("Secrets already exist; initialization refuses to replace either file.")
    password = secrets.token_hex(32)
    connection = f"Host=database;Database=wotr;Username=wotr;Password={password};Include Error Detail=false"
    for path, value in [(password_file, password), (connection_file, connection), (admin_file, secrets.token_hex(32))]:
        descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
        with os.fdopen(descriptor, "w") as output:
            output.write(value + "\n")
        # The parent directory remains private; non-root container users can read the mounted file.
        path.chmod(0o444)
    environment = ROOT / "infra/.env"
    if not environment.exists():
        shutil.copyfile(ROOT / "infra/.env.example", environment)
        environment.chmod(0o600)
    print("Created private secrets and local deployment settings. No credentials were printed.")


if __name__ == "__main__":
    main()
