"""Test-only local TLS and Compose helpers. Never emits account/session payloads."""
import json
from pathlib import Path
import re
import ssl
import subprocess
import urllib.request
import uuid
from websockets.sync.client import connect

ROOT = Path(__file__).resolve().parents[2]
PROTOCOL = re.search(r"const int Version = (\d+);", (ROOT / "src/Wotr.Contracts/Runtime/Protocol.cs").read_text()).group(1)
# DIST-05: production rejects clients without an identity; the probe declares the current protocol.
CLIENT_HEADERS = {"X-Wotr-Client-Version": "0.0.0-deployment", "X-Wotr-Protocol-Version": PROTOCOL}


class Client:
    def __init__(self, address, certificate):
        self.address = address
        self.tls = ssl.create_default_context(cafile=str(certificate))

    def request(self, path, session=None, body=None, method=None):
        headers = {"Content-Type": "application/json", **CLIENT_HEADERS}
        if session:
            headers["Authorization"] = "Bearer " + session["token"]
        request = urllib.request.Request(self.address + path, headers=headers, method=method,
                                         data=None if body is None else json.dumps(body).encode())
        with urllib.request.urlopen(request, context=self.tls, timeout=10) as response:
            return json.load(response)

    def notice(self, match, session, revision):
        address = self.address.replace("https:", "wss:") + f"/api/matches/{match}/updates"
        with connect(address, ssl=self.tls, proxy=None, close_timeout=1,
                     additional_headers={"Authorization": "Bearer " + session["token"], **CLIENT_HEADERS}) as socket:
            notice = json.loads(socket.recv(timeout=8))
            assert set(notice) == {"matchId", "revision", "privateVersion"}
            assert notice["matchId"] == match and notice["revision"] == revision

    def commit(self, match, session, kind):
        view = self.request(f"/api/matches/{match}", session)
        assert any(option["kind"] == kind for option in view["options"]), "Expected continuation unavailable."
        command = {"matchId": match, "commandId": uuid.uuid4().hex,
                   "expectedRevision": view["revision"], "decisionId": view["decision"]["id"],
                   "kind": kind, "payload": {}}
        receipt = self.request(f"/api/matches/{match}/commands", session, command)
        assert receipt["accepted"], "Server rejected the test continuation."
        return command, receipt


def literal(value):
    return "'" + str(value).replace("'", "''") + "'"


def snapshot(deployment, match, database="wotr"):
    return json.loads(deployment.sql(f"SELECT state::text FROM matches WHERE id={literal(match)};", database))


def register(deployment, client):
    output = deployment.compose("exec", "-T", "server", "dotnet", "Wotr.Server.dll", "--create-invite",
                                stdout=subprocess.PIPE).stdout.decode()
    invitation = next(line.rsplit(": ", 1)[-1] for line in output.splitlines() if line.startswith("Invitation ("))
    credentials = {"username": "deploy-" + uuid.uuid4().hex[:18], "password": "Test-" + uuid.uuid4().hex}
    session = client.request("/api/auth/register", body=dict(credentials, invitation=invitation))
    return session, credentials


def restore_override(deployment, folder, database):
    override = folder / "restore.compose.json"
    override.write_text(json.dumps({
        "services": {"server": {"environment": {"WOTR_DATABASE": database}}}
    }))
    deployment.command += ["-f", str(override)]
    return deployment
