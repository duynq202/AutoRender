import io
import threading
import unittest
from unittest.mock import patch
from autorender import Stopped
from autorender_worker import serve


class WorkerTests(unittest.TestCase):
    def test_stop_command_cancels_runner(self):
        observed = threading.Event()

        def run(runner, config):
            if runner.stop.wait(1):
                observed.set()
                raise Stopped()
            raise AssertionError("Stop command was not received")

        with patch("autorender_worker.Runner.run", run):
            self.assertEqual(serve({}, None, io.StringIO("stop\n"), lambda _: None), 2)
        self.assertTrue(observed.is_set())

    def test_frontend_disconnect_cancels_runner(self):
        def run(runner, config):
            if runner.stop.wait(1):
                raise Stopped()
            raise AssertionError("EOF should cancel")

        with patch("autorender_worker.Runner.run", run):
            self.assertEqual(serve({}, None, io.StringIO(""), lambda _: None), 2)

    def test_backend_failure_has_nonzero_exit(self):
        with patch("autorender_worker.Runner.run", side_effect=RuntimeError("export failed")):
            self.assertEqual(serve({}, None, io.StringIO(""), lambda _: None), 1)

    def test_success_exit(self):
        with patch("autorender_worker.Runner.run"):
            self.assertEqual(serve({}, None, io.StringIO(""), lambda _: None), 0)
