#!/usr/bin/env python3
"""BOT-225: the operator's protocol sidecar.

Listens on a local TCP port and, for every connection the game server opens (one per CPU game), starts one protocol bot
as a child process and joins the connection to the bot's standard streams, one JSON line each way (WotR Bot Protocol,
SPEC §2: "a plain TCP stream is the same protocol"). The bot's stderr goes to a log file per connection.

    python3 scripts/protocol_sidecar.py --listen 127.0.0.1:7311 --cwd /srv/alphawotr --logs /srv/alphawotr/logs \
        --max-games 4 -- /srv/alphawotr/venv/bin/python -I protocol/wotr_protocol_bot.py

The bot command follows "--". The sidecar listens on the loopback address only unless told otherwise, refuses connections
beyond --max-games (the server then answers with its own runtime and counts it), and never reads or changes what passes.
"""
import argparse
import datetime
import os
import socket
import subprocess
import sys
import threading


def pump_to_bot(conn, proc, done):
    try:
        while True:
            data = conn.recv(65536)
            if not data:
                break
            proc.stdin.write(data)
            proc.stdin.flush()
    except (OSError, ValueError):
        pass
    finally:
        done.set()
        try:
            proc.stdin.close()
        except OSError:
            pass


def pump_to_server(conn, proc, done):
    try:
        while True:
            data = proc.stdout.read1(65536)
            if not data:
                break
            conn.sendall(data)
    except (OSError, ValueError):
        pass
    finally:
        done.set()


def serve(conn, address, args, slots, number):
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    log_path = os.path.join(args.logs, f"bot-{stamp}-{number}.log") if args.logs else os.devnull
    with open(log_path, "ab") as log:
        proc = subprocess.Popen(args.command, cwd=args.cwd, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=log,
                                env=dict(os.environ, MPLBACKEND="Agg"))
        done = threading.Event()
        threads = [threading.Thread(target=pump_to_bot, args=(conn, proc, done), daemon=True),
                   threading.Thread(target=pump_to_server, args=(conn, proc, done), daemon=True)]
        for thread in threads:
            thread.start()
        done.wait()
        try:
            conn.shutdown(socket.SHUT_RDWR)
        except OSError:
            pass
        conn.close()
        try:
            proc.wait(timeout=10)
        except subprocess.TimeoutExpired:
            proc.kill()
            proc.wait()
    slots.release()


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--listen", default="127.0.0.1:7311")
    parser.add_argument("--cwd", default=".")
    parser.add_argument("--logs", default="")
    parser.add_argument("--max-games", type=int, default=4)
    parser.add_argument("command", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    if args.command and args.command[0] == "--":
        args.command = args.command[1:]
    if not args.command:
        parser.error("the bot command follows --")
    if args.logs:
        os.makedirs(args.logs, exist_ok=True)
    host, port = args.listen.rsplit(":", 1)
    slots = threading.BoundedSemaphore(args.max_games)
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((host, int(port)))
    server.listen(16)
    print(f"protocol sidecar on {host}:{port}, at most {args.max_games} games: {' '.join(args.command)}", flush=True)
    number = 0
    while True:
        conn, address = server.accept()
        if not slots.acquire(blocking=False):
            conn.close()
            continue
        number += 1
        conn.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        threading.Thread(target=serve, args=(conn, address, args, slots, number), daemon=True).start()


if __name__ == "__main__":
    sys.exit(main())
