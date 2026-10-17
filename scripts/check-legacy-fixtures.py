#!/usr/bin/env python3
"""Rebuild the historical engine and verify the T37 migration fixtures."""
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import tarfile
import tempfile


ROOT = Path(__file__).resolve().parent.parent
FIXTURES = ROOT / "tests/fixtures/contract-legacy"
PATHS = ["Directory.Build.props", "global.json", "src/Wotr.Contracts", "src/Wotr.Domain",
         "content/base-board.json", "content/base-cards.json", "content/base-characters.json"]


def main():
    manifest = json.loads((FIXTURES / "manifest.json").read_text())
    for name, expected in manifest["files"].items():
        if hashlib.sha256((FIXTURES / name).read_bytes()).hexdigest() != expected:
            raise SystemExit(f"Fixture checksum differs: {name}")
    artifacts = ROOT / ".artifacts"
    artifacts.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="legacy-check-", dir=artifacts) as directory:
        workspace = Path(directory)
        archive = subprocess.check_output(["git", "archive", manifest["sourceCommit"], *PATHS], cwd=ROOT)
        with tarfile.open(fileobj=io.BytesIO(archive)) as package:
            package.extractall(workspace, filter="data")
        project = workspace / "Export"
        project.mkdir()
        (project / "Program.cs").write_bytes((FIXTURES / "export.cs").read_bytes())
        (project / "Export.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><ProjectReference Include="../src/Wotr.Domain/Wotr.Domain.csproj" /></ItemGroup>
</Project>
''')
        output = workspace / "output"
        environment = os.environ.copy()
        environment.update(DOTNET_CLI_HOME=str(artifacts / "dotnet-cli"), DOTNET_CLI_TELEMETRY_OPTOUT="1")
        subprocess.run(["bash", str(ROOT / "scripts/dotnet.sh"), "run", "--project", str(project / "Export.csproj"),
                        "-c", "Release", "--", str(output)], cwd=ROOT, env=environment, check=True)
        for name, expected in manifest["files"].items():
            if name == "export.cs":
                continue
            if hashlib.sha256((output / name).read_bytes()).hexdigest() != expected:
                raise SystemExit(f"Historical export differs: {name}")
    print("PASS: historical engine reproduces all T37 save/command/receipt fixture bytes.")


if __name__ == "__main__":
    main()
