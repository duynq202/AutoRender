import copy
import threading
import unittest
from unittest.mock import patch
from autorender_core import DEFAULT, Runner, Stopped, validate


class FakeGUI:
    class ImageNotFoundException(Exception):
        pass

    def __init__(self, matches):
        self.matches = iter(matches)
        self.clicks = []

    def click(self, *point):
        self.clicks.append(list(point))

    def locateOnScreen(self, *args, **kwargs):
        result = next(self.matches)
        if result == "missing":
            raise self.ImageNotFoundException()
        return result


class RunnerTests(unittest.TestCase):
    def config(self):
        config = copy.deepcopy(DEFAULT)
        config["projects"].append({"name": "Project 2", "position": [600, 300]})
        return config

    def test_queue_exports_in_order_and_waits_for_success(self):
        config = self.config()
        gui = FakeGUI([None, "missing", (1, 2), None, (1, 2)])
        runner = Runner(gui, threading.Event(), lambda _: None)
        with patch.object(runner, "wait"):
            runner.run(config)
        expected = []
        for project in config["projects"]:
            expected.extend([project["position"], config["export_button"], config["confirm_export_button"],
                             config["close_completion_button"], config["home_button"]])
        self.assertEqual(gui.clicks, expected)

    def test_stale_success_stops_before_confirming_export(self):
        gui = FakeGUI([(1, 2)])
        runner = Runner(gui, threading.Event(), lambda _: None)
        with patch.object(runner, "wait"), self.assertRaises(RuntimeError):
            runner.run(self.config())
        self.assertEqual(len(gui.clicks), 2)

    def test_timeout_does_not_start_next_project(self):
        gui = FakeGUI([None, None])
        runner = Runner(gui, threading.Event(), lambda _: None)
        with patch.object(runner, "wait"), patch("autorender_core.time.monotonic", side_effect=[0, 99999]), self.assertRaises(TimeoutError):
            runner.run(self.config())
        self.assertEqual(len(gui.clicks), 3)

    def test_stop_prevents_clicks(self):
        stop = threading.Event()
        stop.set()
        gui = FakeGUI([])
        with self.assertRaises(Stopped):
            Runner(gui, stop, lambda _: None).run(self.config())
        self.assertEqual(gui.clicks, [])

    def test_missing_completion_image_is_rejected(self):
        with self.assertRaises(ValueError):
            validate(copy.deepcopy(DEFAULT))


if __name__ == "__main__":
    unittest.main()
