"""UI-independent CapCut export queue used by the WPF worker."""
from __future__ import annotations

import time
from pathlib import Path

DEFAULT = {
    "projects": [{"name": "Project 1", "position": [300, 300]}],
    "export_button": [1700, 60],
    "confirm_export_button": [1200, 850],
    "completion_image": "",
    "completion_region": None,
    "close_completion_button": [1200, 850],
    "home_button": [30, 30],
    "open_delay_seconds": 12,
    "dialog_delay_seconds": 3,
    "home_delay_seconds": 5,
    "export_timeout_seconds": 3600,
    "poll_seconds": 2,
}


def validate(config):
    def point(value):
        return isinstance(value, list) and len(value) == 2 and all(
            type(n) is int and n >= 0 for n in value)
    projects = config.get("projects")
    if not isinstance(projects, list) or not projects:
        raise ValueError("Hàng đợi phải có ít nhất một project.")
    for project in projects:
        if not isinstance(project, dict) or not project.get("name") or not point(project.get("position")):
            raise ValueError("Mỗi project cần name và position [x, y].")
    for key in ("export_button", "confirm_export_button", "close_completion_button", "home_button"):
        if not point(config.get(key)):
            raise ValueError(f"{key} phải là [x, y] không âm.")
    for key in ("open_delay_seconds", "dialog_delay_seconds", "home_delay_seconds", "export_timeout_seconds", "poll_seconds"):
        value = config.get(key)
        if type(value) not in (int, float) or not 0 < value <= 86400:
            raise ValueError(f"{key} phải lớn hơn 0 và không quá 86400.")
    region = config.get("completion_region")
    if region is not None and (not isinstance(region, list) or len(region) != 4
                              or any(type(n) is not int or n < 0 for n in region)
                              or region[2] == 0 or region[3] == 0):
        raise ValueError("completion_region cần [x, y, width, height], hoặc null.")
    if not isinstance(config.get("completion_image"), str) or not Path(config["completion_image"]).is_file():
        raise ValueError("Chọn ảnh PNG đã cắt riêng thông báo xuất thành công của CapCut.")
    return config


class Stopped(Exception):
    pass


class Runner:
    def __init__(self, automation, stop, log):
        self.gui, self.stop, self.log = automation, stop, log

    def wait(self, seconds):
        if self.stop.wait(seconds):
            raise Stopped()

    def click(self, point):
        if self.stop.is_set():
            raise Stopped()
        self.gui.click(*point)

    def completed(self, config):
        try:
            return self.gui.locateOnScreen(config["completion_image"],
                                          region=config.get("completion_region"), grayscale=False) is not None
        except self.gui.ImageNotFoundException:
            return False

    def run(self, config):
        self.log("Bắt đầu sau 5 giây. Chuyển sang màn hình chính CapCut.")
        self.wait(5)
        for index, project in enumerate(config["projects"], 1):
            self.log(f'{index}/{len(config["projects"])}: {project["name"]}')
            self.click(project["position"])
            self.wait(config["open_delay_seconds"])
            self.click(config["export_button"])
            self.wait(config["dialog_delay_seconds"])
            # An old success dialog must never count as this export completing.
            if self.completed(config):
                raise RuntimeError("Ảnh thành công đã hiện trước khi xuất. Kiểm tra lại trạng thái CapCut.")
            self.click(config["confirm_export_button"])
            deadline = time.monotonic() + config["export_timeout_seconds"]
            while not self.completed(config):
                if time.monotonic() >= deadline:
                    raise TimeoutError(f'Hết thời gian chờ xuất: {project["name"]}. Hàng đợi đã dừng.')
                self.wait(config["poll_seconds"])
            self.log(f'Đã nhận diện thông báo thành công: {project["name"]}')
            self.click(config["close_completion_button"])
            self.wait(config["dialog_delay_seconds"])
            self.click(config["home_button"])
            self.wait(config["home_delay_seconds"])
        self.log("Hoàn tất hàng đợi. Kiểm tra video trong thư mục xuất của CapCut.")
