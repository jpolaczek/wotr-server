# Pure rules source package

`python3 scripts/package_rules.py --output .artifacts/rules-package` packages a
clean committed version of Contracts, Domain and the content embedded by Domain.
`content/rules-package.json` owns the independent package version (initially
`0.1.0`); the lock records the exact server commit, Contracts version, protocol,
rules/content versions, SHA-256 of every file and SHA-256 of the archive.

The deterministic source tarball preserves the original project paths and MIT
notices. Consumers can build it with their own application from a clean checkout
without downloading a sibling game repository or receiving database credentials.
The private bot vendors this reviewed archive and lock until an operator artifact
registry is configured. This is a private-service build input, not a Unity/player
dependency; existing player package versions and pins do not change.

The allowlist consists of the two pure projects, their C# sources and lockfiles,
the root build properties/SDK pin, notices, and exact embedded resource patterns
from the Domain project. It excludes the legacy `Domain/Demo` policy, server,
database, endpoints, private strategies, local task state and build artifacts.
Only the simulation module of a consumer may reference Domain. Receiving pure
state types does not authorize obtaining a live game state: reconstruct hypothetical
states from an ordinary seat observation.

Build two archives from the same commit to verify byte identity; inspect the
lock's file list and run `python3 -m unittest discover -s tests/packages -v`.
Never replace a different artifact in an existing output directory. A changed
published input needs a reviewed new package version and explicit consumer pin.
No upload or release is performed by this script.
