#!/usr/bin/env python3
"""Private Compose backup/restore. Restores only into a newly created database."""
import argparse
from datetime import datetime, timezone
import hashlib
import gzip
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


class Deployment:
    def __init__(self, environment):
        self.environment = Path(environment).resolve()
        self.command = ["docker", "compose", "--env-file", str(self.environment),
                        "-f", str(ROOT / "infra/compose.yaml")]

    def compose(self, *args, **kwargs):
        return subprocess.run(self.command + list(args), check=True, cwd=ROOT,
                              stderr=subprocess.PIPE, **kwargs)

    def sql(self, query, database="wotr"):
        user = "postgres" if database == "postgres" else "wotr"
        return self.compose("exec", "-T", "database", "psql", "-X", "-v", "ON_ERROR_STOP=1",
                            "-U", user, "-d", database, "-At", input=query.encode(),
                            stdout=subprocess.PIPE).stdout.decode().strip()

    def server_details(self):
        container = self.compose("ps", "-q", "server", stdout=subprocess.PIPE).stdout.decode().strip()
        if not re.fullmatch(r"[a-f0-9]{12,64}", container):
            raise ValueError("Expected one running server container.")
        database = docker_output("exec", container, "printenv", "WOTR_DATABASE").decode().strip()
        if not re.fullmatch(r"wotr|wotr_restore_[a-z0-9_]{1,40}", database):
            raise ValueError("Running server must declare its WoTR deployment database.")
        image = docker_output("inspect", "--format", "{{.Image}}", container).decode().strip()
        if not re.fullmatch(r"sha256:[a-f0-9]{64}", image):
            raise ValueError("Expected an immutable server image ID.")
        return {"container": container, "database": database, "image": image}

    def backup(self, destination):
        server = self.server_details()
        destination = Path(destination).resolve()
        destination.mkdir(mode=0o700, parents=True, exist_ok=False)
        dump = destination / "database.dump"
        with dump.open("wb") as output:
            self.compose("exec", "-T", "database", "pg_dump", "-U", "wotr", "-d", server["database"],
                         "--format=custom", "--no-owner", "--no-acl", stdout=output)
        export_image(server["image"], destination / "server-image.tar.gz")
        content = docker_output("exec", server["container"], "cat", "/app/release-content.sha256")
        (destination / "release-content.sha256").write_bytes(content)
        for name in ["compose.yaml", "Caddyfile", "Caddyfile.local", "init-database.sql", ".env.example"]:
            shutil.copyfile(ROOT / "infra" / name, destination / name)
        manifest = {
            "format": 1,
            "created_utc": datetime.now(timezone.utc).isoformat(),
            "server_image": server["image"],
            "database": server["database"],
            "postgres_major": 17,
            "migrations": self.sql("SELECT string_agg(version::text, ',' ORDER BY version) FROM schema_migrations;",
                                   server["database"]),
            "files": {path.name: digest(path) for path in sorted(destination.iterdir())},
        }
        if self.server_details() != server:
            raise ValueError("Server changed during backup; no complete manifest was written. Retry without deploying an update.")
        (destination / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
        return destination

    def restore(self, source, target):
        if not re.fullmatch(r"wotr_restore_[a-z0-9_]{1,40}", target):
            raise ValueError("Target must be a new database named wotr_restore_<suffix>.")
        source = Path(source).resolve()
        manifest = json.loads((source / "manifest.json").read_text())
        if manifest.get("format") != 1 or manifest.get("postgres_major") != 17:
            raise ValueError("Unsupported backup format or PostgreSQL major version.")
        if not {"database.dump", "server-image.tar.gz", "release-content.sha256"}.issubset(manifest.get("files", {})):
            raise ValueError("Backup manifest is incomplete.")
        for name, expected in manifest["files"].items():
            if Path(name).name != name or digest(source / name) != expected:
                raise ValueError("Backup checksum mismatch.")
        if self.sql(f"SELECT 1 FROM pg_database WHERE datname='{target}';", "postgres"):
            raise ValueError("Target database already exists; restore refuses to overwrite it.")
        self.compose("exec", "-T", "database", "createdb", "-U", "postgres", "-O", "wotr", "-T", "template0", target,
                     stdout=subprocess.DEVNULL)
        with (source / "database.dump").open("rb") as dump:
            self.compose("exec", "-T", "database", "pg_restore", "-U", "wotr", "-d", target,
                         "--single-transaction", "--exit-on-error", "--no-owner", "--no-acl",
                         stdin=dump, stdout=subprocess.DEVNULL)
        return target


def docker_output(*args):
    return subprocess.run(["docker", *args], check=True, stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE).stdout


def export_image(image, destination):
    # A file avoids a full stderr pipe blocking the exporter while stdout is copied.
    with tempfile.TemporaryFile() as errors:
        with subprocess.Popen(["docker", "image", "save", image], stdout=subprocess.PIPE,
                              stderr=errors) as export:
            try:
                with gzip.open(destination, "wb") as archive:
                    shutil.copyfileobj(export.stdout, archive)
                export.wait()
            except BaseException:
                # In particular, disk-full must not leave docker blocked on its output pipe.
                export.kill()
                export.wait()
                raise
            if export.returncode:
                errors.seek(0)
                raise subprocess.CalledProcessError(export.returncode, export.args, stderr=errors.read())


def digest(path):
    result = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(block)
    return result.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--env-file", default=str(ROOT / "infra/.env"))
    commands = parser.add_subparsers(dest="command", required=True)
    backup = commands.add_parser("backup")
    backup.add_argument("directory")
    restore = commands.add_parser("restore")
    restore.add_argument("directory")
    restore.add_argument("--database", required=True)
    args = parser.parse_args()
    os.umask(0o077)
    deployment = Deployment(args.env_file)
    try:
        if args.command == "backup":
            result = deployment.backup(args.directory)
            print(f"Backup complete: {result}")
        else:
            result = deployment.restore(args.directory, args.database)
            print(f"Restored to {result}. The running server was not switched.")
    except subprocess.CalledProcessError as error:
        # Database tools can include SQL/data in errors. Keep captured stderr private.
        raise SystemExit(f"Deployment operation failed (exit {error.returncode}); active database unchanged.") from None
    except (ValueError, FileNotFoundError) as error:
        raise SystemExit(str(error)) from None
    except OSError:
        raise SystemExit("Deployment I/O failed. Check free space and permissions; no complete backup was accepted.") from None


if __name__ == "__main__":
    main()
