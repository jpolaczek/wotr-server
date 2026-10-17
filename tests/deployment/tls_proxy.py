"""Temporary loopback TLS tunnel for the existing isolated HTTP test server."""
import asyncio
from contextlib import AbstractContextManager
from pathlib import Path
import ssl
import tempfile
import threading


class LoopbackTlsProxy(AbstractContextManager):
    def __init__(self, deployment, upstream_port):
        self.deployment = deployment
        self.upstream_port = upstream_port
        self.folder = None
        self.loop = None
        self.thread = None
        self.ready = threading.Event()
        self.failure = None
        self.server = None
        self.connections = set()

    def __enter__(self):
        self.folder = tempfile.TemporaryDirectory(prefix='wotr-map17-tls-')
        try:
            root = Path(self.folder.name)
            for name, remote in {
                'ca.crt': '/data/caddy/pki/authorities/local/root.crt',
                'localhost.crt': '/data/caddy/certificates/local/localhost/localhost.crt',
                'localhost.key': '/data/caddy/certificates/local/localhost/localhost.key'
            }.items():
                target = root / name
                # Capture certificate/key bytes directly into a private temporary file;
                # never return them to logs or copy the CA signing key.
                with target.open('xb') as output:
                    target.chmod(0o600)
                    self.deployment.compose('exec', '-T', 'proxy', 'cat', remote, stdout=output)
            self.certificate = root / 'ca.crt'
            context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
            context.minimum_version = ssl.TLSVersion.TLSv1_2
            context.set_alpn_protocols(['http/1.1'])
            context.load_cert_chain(root / 'localhost.crt', root / 'localhost.key')
            self.thread = threading.Thread(target=self._serve, args=(context,), daemon=True)
            self.thread.start()
            if not self.ready.wait(10) or self.failure:
                raise RuntimeError('The isolated loopback TLS proxy could not start.')
            self.address = f'https://localhost:{self.server.sockets[0].getsockname()[1]}'
            return self
        except BaseException:
            self.__exit__(None, None, None)
            raise

    def _serve(self, context):
        try:
            self.loop = asyncio.new_event_loop()
            asyncio.set_event_loop(self.loop)
            self.server = self.loop.run_until_complete(asyncio.start_server(
                self._connection, '127.0.0.1', 0, ssl=context))
            self.ready.set()
            self.loop.run_forever()
        except BaseException as error:
            self.failure = type(error).__name__
            self.ready.set()
        finally:
            if self.loop:
                self.loop.close()

    async def _connection(self, reader, writer):
        task = asyncio.current_task()
        self.connections.add(task)
        upstream = None
        try:
            incoming, upstream = await asyncio.open_connection('127.0.0.1', self.upstream_port)
            pumps = [asyncio.create_task(self._copy(reader, upstream)),
                     asyncio.create_task(self._copy(incoming, writer))]
            try:
                await asyncio.wait(pumps, return_when=asyncio.FIRST_COMPLETED)
            finally:
                for pump in pumps:
                    pump.cancel()
                await asyncio.gather(*pumps, return_exceptions=True)
        except (ConnectionError, OSError):
            pass
        finally:
            for stream in (upstream, writer):
                if stream:
                    stream.close()
            self.connections.discard(task)

    @staticmethod
    async def _copy(reader, writer):
        while data := await reader.read(65536):
            writer.write(data)
            await writer.drain()

    async def _close(self):
        if self.server:
            self.server.close()
            await self.server.wait_closed()
        active = list(self.connections)
        for task in active:
            task.cancel()
        await asyncio.gather(*active, return_exceptions=True)

    def __exit__(self, *_):
        try:
            if self.loop and self.loop.is_running():
                future = asyncio.run_coroutine_threadsafe(self._close(), self.loop)
                try:
                    future.result(timeout=10)
                finally:
                    self.loop.call_soon_threadsafe(self.loop.stop)
                    self.thread.join(timeout=10)
        finally:
            if self.folder:
                self.folder.cleanup()
        return False
