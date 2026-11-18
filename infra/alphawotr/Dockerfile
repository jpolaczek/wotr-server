# DIST-20: AlphaWOTR v0.2.0 (MIT, https://github.com/Weyoun5/AlphaWOTR) behind the protocol sidecar: the CPU of base-game
# CPU rooms (BOT-219, BOT-225). A pinned base and pinned packages (CPU torch), the release's commit with every file
# checked against its RELEASE_MANIFEST.tsv; no WOTR-Gandalf client, no git history. Built on the host, no registry:
#   infra/production.sh build alphawotr      (the image wotr-alphawotr:0.2.0)
# The bot runs the release's pure-Python search, its reference (the native search library is its author's speed-up and
# is not built here). The licences stay: /opt/alphawotr/LICENSE and each package's own in site-packages.
FROM python:3.12.12-slim-bookworm@sha256:593bd06efe90efa80dc4eee3948be7c0fde4134606dd40d8dd8dbcade98e669c AS source
ARG ALPHAWOTR_TAG=v0.2.0
ARG ALPHAWOTR_COMMIT=3dd687463336304a472ec32edab643d720d64d19
RUN apt-get update && apt-get install -y --no-install-recommends git ca-certificates && rm -rf /var/lib/apt/lists/*
RUN git -c advice.detachedHead=false clone --quiet --branch "$ALPHAWOTR_TAG" --depth 1 https://github.com/Weyoun5/AlphaWOTR /opt/alphawotr \
    && test "$(git -C /opt/alphawotr rev-parse HEAD)" = "$ALPHAWOTR_COMMIT"
COPY infra/alphawotr/check_manifest.py /check_manifest.py
RUN python3 /check_manifest.py /opt/alphawotr \
    && rm -rf /opt/alphawotr/.git /opt/alphawotr/wotr_bridge/play_client

FROM python:3.12.12-slim-bookworm@sha256:593bd06efe90efa80dc4eee3948be7c0fde4134606dd40d8dd8dbcade98e669c
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1 PIP_NO_CACHE_DIR=1 PIP_DISABLE_PIP_VERSION_CHECK=1 \
    MPLBACKEND=Agg MPLCONFIGDIR=/tmp/matplotlib OMP_NUM_THREADS=2 MKL_NUM_THREADS=2
COPY infra/alphawotr/requirements.txt /requirements.txt
RUN pip install --index-url https://pypi.org/simple --extra-index-url https://download.pytorch.org/whl/cpu \
        --only-binary=:all: -r /requirements.txt
COPY --from=source /opt/alphawotr /opt/alphawotr
COPY scripts/protocol_sidecar.py /opt/sidecar/protocol_sidecar.py
COPY infra/alphawotr/content_hash.py /opt/sidecar/content_hash.py
# Owner, 2026-11-18: AlphaWOTR keeps its reasoning. /var/log/alphawotr is its user's, so a named volume mounted there
# starts writable (production: the volume alphawotr-logs); an empty folder leaves the content hash unchanged.
RUN useradd --uid 1655 --no-create-home --shell /usr/sbin/nologin alphawotr \
    && mkdir -p /var/log/alphawotr && chown 1655:1655 /var/log/alphawotr
USER 1655
WORKDIR /opt/alphawotr
EXPOSE 7311
# --check: the bot must answer hello before the sidecar listens; --health: the container's health check (no bot started).
ENTRYPOINT ["python3", "/opt/sidecar/protocol_sidecar.py", "--check", "--listen", "0.0.0.0:7311", "--health", "127.0.0.1:7312", \
    "--cwd", "/opt/alphawotr", "--logs", "-"]
CMD ["--max-games", "3", "--", "python3", "-I", "protocol/wotr_protocol_bot.py"]
