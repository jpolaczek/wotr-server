#!/usr/bin/env python3
"""Acceptance of the explicitly local Compose stack, including a clean database restore."""
import argparse
import json
import os
import ssl
import subprocess
import sys
import urllib.error
import urllib.request
import uuid
from deploy import Deployment, ROOT

sys.path.insert(0, str(ROOT / "tests/deployment"))
from support import Client, literal, register, restore_override, snapshot


class Acceptance:
    def __init__(self, environment):
        self.environment = environment
        self.deployment = Deployment(environment)
        configuration = json.loads(self.deployment.compose("config", "--format", "json", stdout=subprocess.PIPE).stdout)
        proxy = configuration["services"]["proxy"]
        assert self.deployment.server_details()["database"] == "wotr", "Test requires the original local database."
        assert proxy["environment"]["WOTR_DOMAIN"] == "localhost", "Test seeds are local only."
        ports = proxy["ports"]
        assert all(port.get("host_ip") == "127.0.0.1" for port in ports), "Test requires loopback bindings."
        port = next(port["published"] for port in ports if port["target"] == 443)
        self.folder = ROOT / ".artifacts" / ("deployment-" + uuid.uuid4().hex[:8])
        self.folder.mkdir(mode=0o700)
        certificate = self.folder / "root.crt"
        certificate.write_bytes(self.deployment.compose("exec", "-T", "proxy", "cat",
            "/data/caddy/pki/authorities/local/root.crt", stdout=subprocess.PIPE).stdout)
        self.client = Client(f"https://localhost:{port}", certificate)
        self.match = "deployment-" + uuid.uuid4().hex
        self.database = "wotr_restore_" + uuid.uuid4().hex[:12]

    def prepare(self):
        assert self.client.request("/health/ready")["status"] == "ready"
        try:
            urllib.request.urlopen(self.client.address + "/health", context=ssl.create_default_context(), timeout=5)
            raise AssertionError("Local CA unexpectedly trusted without the explicit certificate.")
        except urllib.error.URLError as error:
            assert isinstance(error.reason, ssl.SSLCertVerificationError)
        self.fp, self.credentials = register(self.deployment, self.client)
        self.sh, _ = register(self.deployment, self.client)
        self.room = self.client.request("/api/rooms/", self.fp, {"side": "FreePeoples"})
        self.client.request("/api/rooms/join", self.sh, {"code": self.room["joinCode"]})
        self.room = self.client.request(f"/api/rooms/{self.room['id']}", self.fp)
        raw = (ROOT / "tests/fixtures/base-regression/retreat.json").read_text()
        state = json.loads(raw.replace("corpus-retreat", self.match))
        self.deployment.sql("INSERT INTO matches(id,fp_account,sh_account,revision,state) VALUES ("
            + ",".join([literal(self.match), literal(self.fp["accountId"]), literal(self.sh["accountId"]),
                        str(state["revision"]), literal(json.dumps(state)) + "::jsonb"]) + ");")
        self.command, self.receipt = self.client.commit(self.match, self.fp, "ContinueBattle")
        self.before = snapshot(self.deployment, self.match)
        assert self.before["pending"]["kind"] == "BattleRetreat"
        assert self.before["randomSamples"] and self.before["schemaVersion"] == 2
        self.views = [self.client.request(f"/api/matches/{self.match}", session) for session in [self.fp, self.sh]]
        self.client.notice(self.match, self.sh, self.receipt["revision"])

    def backup_and_restore(self):
        backup = self.deployment.backup(self.folder / "backup")
        self.deployment.restore(backup, self.database)
        try:
            self.deployment.restore(backup, self.database)
            raise AssertionError("Restore overwrote an existing database.")
        except ValueError as error:
            assert "already exists" in str(error)
        assert snapshot(self.deployment, self.match, self.database) == self.before
        for table in ["accounts", "invitations", "sessions", "rooms", "matches", "command_receipts", "match_events", "match_reads", "schema_migrations"]:
            query = f"SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text), '[]') FROM {table} t;"
            assert self.deployment.sql(query) == self.deployment.sql(query, self.database), table + " differs after restore."
        self.restored = restore_override(Deployment(self.environment), self.folder, self.database)
        self.restored.compose("up", "-d", "--wait", "server", stdout=subprocess.DEVNULL)

    def resume(self):
        assert self.client.request("/health/ready")["status"] == "ready"
        for session, expected in zip([self.fp, self.sh], self.views):
            assert self.client.request(f"/api/matches/{self.match}", session) == expected
            self.client.notice(self.match, session, self.receipt["revision"])
        assert self.client.request(f"/api/rooms/{self.room['id']}", self.fp) == self.room
        renewed = self.client.request("/api/auth/login", body=self.credentials)
        assert renewed["accountId"] == self.fp["accountId"]
        path = f"/api/matches/{self.match}"
        assert self.client.request(path + "/receipts/" + self.command["commandId"], self.fp) == self.receipt
        assert self.client.request(path + "/commands", self.fp, self.command) == self.receipt
        assert snapshot(self.restored, self.match, self.database) == self.before, "Retry changed the restored state/RNG."
        self.client.commit(self.match, self.sh, "HoldBattle")
        self.client.commit(self.match, self.fp, "CommitCombatCard")
        _, receipt = self.client.commit(self.match, self.sh, "CommitCombatCard")
        after = snapshot(self.restored, self.match, self.database)
        self.after = after
        assert after["revision"] == self.before["revision"] + 3
        assert len(after["randomSamples"]) > len(self.before["randomSamples"]), "Continuation must roll the next round."
        self.client.notice(self.match, self.fp, receipt["revision"])
        self.report = {"result": "PASS", "tls": "explicit local CA; hostname verified", "wss": True,
                       "restored_database": self.database, "snapshot_schema": after["schemaVersion"],
                       "restored_tables": 9, "before_revision": self.before["revision"], "after_revision": after["revision"],
                       "preserved_rng_samples": len(self.before["randomSamples"]),
                       "new_rng_samples": len(after["randomSamples"]) - len(self.before["randomSamples"]),
                       "retry_mutated_state": False, "existing_database_overwrite_refused": True}

    def backup_after_switch(self):
        backup = self.restored.backup(self.folder / "backup-after-switch")
        manifest = json.loads((backup / "manifest.json").read_text())
        assert manifest["database"] == self.database, "Backup selected the original database after restore."
        target = "wotr_restore_" + uuid.uuid4().hex[:12]
        self.restored.restore(backup, target)
        assert snapshot(self.restored, self.match, target) == self.after, "Second restore lost the continued round."
        assert snapshot(self.deployment, self.match) == self.before, "Original database was modified."
        self.report["backup_after_database_switch"] = True

    def run(self):
        self.prepare()
        try:
            self.backup_and_restore()
            self.resume()
            self.backup_after_switch()
        finally:
            # Return only this test's stack to its original database, including after a failed assertion.
            self.deployment.compose("up", "-d", "--wait", "server", stdout=subprocess.DEVNULL)
        (self.folder / "result.json").write_text(json.dumps(self.report, indent=2) + "\n")
        print("PASS: TLS/WSS, nine restored tables, original receipt, unchanged retry, continued battle with new RNG.")
        print("Report:", self.folder / "result.json")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--env-file", default=str(ROOT / "infra/.env"))
    args = parser.parse_args()
    os.umask(0o077)
    try:
        Acceptance(args.env_file).run()
    except subprocess.CalledProcessError as error:
        print("Failed operation:", " ".join(error.cmd))
        (ROOT / ".artifacts/t38-acceptance-private-error.log").write_bytes(error.stderr or b"")
        raise SystemExit(f"Acceptance command failed (exit {error.returncode}); captured tool output was not disclosed.") from None


if __name__ == "__main__":
    main()
