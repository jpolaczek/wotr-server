#!/usr/bin/env python3
"""Two test accounts over verified HTTPS/WSS, without changing the deployed stack."""
import argparse
from contextlib import ExitStack
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import sys
from urllib.parse import urlparse

from deploy import Deployment, ROOT

sys.path.insert(0, str(ROOT / 'tests/deployment'))
from support import Client
from tls_proxy import LoopbackTlsProxy
from websockets.sync.client import connect


def notice(socket, match, revision):
    value = json.loads(socket.recv(timeout=10))
    assert set(value) == {'matchId', 'revision', 'privateVersion'}, 'WSS sent more than a revision notice.'
    assert value['matchId'] == match and value['revision'] == revision, 'Unexpected WSS revision.'


def open_socket(client, match, session):
    return connect(client.address.replace('https:', 'wss:') + f'/api/matches/{match}/updates',
                   ssl=client.tls, proxy=None, close_timeout=1,
                   additional_headers={'Authorization': 'Bearer ' + session['token']})


def play(client, fixture):
    sessions = [client.request('/api/auth/login', body={
        'username': fixture[key], 'password': fixture['Password']})
        for key in ('FreePeoplesUser', 'ShadowUser')]
    fp, sh = sessions
    assert fp['token'] != sh['token'] and fp['accountId'] != sh['accountId']
    room = client.request('/api/rooms/', fp, {'side': 'FreePeoples'})
    client.request('/api/rooms/join', sh, {'code': room['joinCode']})
    room = client.request('/api/rooms/' + room['id'], fp)
    path = '/api/rooms/' + room['id']
    for session in sessions:
        client.request(path + '/confirm', session, {'expectedVersion': room['version']})
    started = client.request(path + '/start', fp, {'expectedVersion': room['version']})
    match = started['matchId']
    before = [client.request('/api/matches/' + match, session) for session in sessions]
    assert [view['you'] for view in before] == ['FreePeoples', 'Shadow']
    assert before[0]['units'] == before[1]['units']
    assert before[0]['hand'] and before[1]['hand'], 'Normal setup must draw cards for both roles.'
    assert all(card.startswith('base.fp.') for card in before[0]['hand'])
    assert all(card.startswith('base.sh.') for card in before[1]['hand'])
    with ExitStack() as stack:
        sockets = [stack.enter_context(open_socket(client, match, session)) for session in sessions]
        for socket, view in zip(sockets, before):
            notice(socket, match, view['revision'])
        command, receipt = client.commit(match, fp, 'ContinueFellowship')
        for socket in sockets:
            notice(socket, match, receipt['revision'])
    assert client.request(f'/api/matches/{match}/commands', fp, command) == receipt
    assert client.request(f'/api/matches/{match}/receipts/' + command['commandId'], fp) == receipt
    after = [client.request('/api/matches/' + match, session) for session in sessions]
    for session, view in zip(sessions, after):
        assert view['revision'] == receipt['revision'] == before[0]['revision'] + 1
        client.notice(match, session, receipt['revision'])
    return {'roles': ['FreePeoples', 'Shadow'], 'initialRevision': before[0]['revision'],
            'finalRevision': receipt['revision'], 'command': command['kind'],
            'privateHandsSeparated': True, 'wssNoticeContainsOnlyRevisionKeys': True,
            'bothLiveWssNotifications': True, 'bothWssReconnects': True,
            'duplicateCommandPreservesReceiptAndRevision': True}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fixture', type=Path, default=ROOT / '.artifacts/client-integration.json')
    parser.add_argument('--env-file', type=Path, default=ROOT / 'infra/.env')
    parser.add_argument('--output', type=Path, default=ROOT / '.artifacts/map17-tls.json')
    args = parser.parse_args()
    os.umask(0o077)
    fixture = json.loads(args.fixture.read_text())
    address = urlparse(fixture['Address'])
    if address.scheme != 'http' or address.hostname != '127.0.0.1' or address.port != 5088:
        raise ValueError('TLS smoke accepts only the isolated test fixture at http://127.0.0.1:5088.')
    if not all(fixture[key].startswith("scenario-") for key in ("FreePeoplesUser", "ShadowUser")):
        raise ValueError("TLS smoke accepts only generated scenario accounts.")
    with LoopbackTlsProxy(Deployment(args.env_file), address.port) as proxy:
        client = Client(proxy.address, proxy.certificate)
        assert client.tls.check_hostname and client.tls.verify_mode.name == 'CERT_REQUIRED'
        assert client.request('/health/ready')['status'] == 'ready'
        result = play(client, fixture)
        result.update(result='PASS', recordedAt=datetime.now(timezone.utc).isoformat(),
                      transport='HTTPS and WSS through an isolated loopback TLS proxy',
                      certificateValidation='Existing public local CA and verified localhost hostname; no validation bypass',
                      publicCaSha256=hashlib.sha256(proxy.certificate.read_bytes()).hexdigest(),
                      clients='Two Python transport clients; Unity HTTP/WS behavior is tested separately.',
                      deployedStackModified=False, temporaryLeafPrivateKeyRemovedOnExit=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print('PASS: two HTTPS/WSS sessions, private views, live update, reconnect and exact receipt.')
    print('Report:', args.output)


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        # HTTP/session exceptions can carry payloads. Keep credentials and keys out
        # of the console; a failing check is identified only by its exception type.
        import traceback
        frame = traceback.extract_tb(error.__traceback__)[-1]
        raise SystemExit(f'TLS smoke failed: {type(error).__name__} at {Path(frame.filename).name}:{frame.lineno}. No private payload logged.') from None
