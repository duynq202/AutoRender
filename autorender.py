"""Sequential CapCut UI automation. Run on an interactive Windows desktop."""
from __future__ import annotations

import json
import queue
import threading
import time
from pathlib import Path
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

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


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("AutoRender — CapCut Windows")
        self.geometry("900x780")
        self.stop_event = threading.Event()
        self.events = queue.Queue()
        self.worker = None
        ttk.Label(self, text="Giữ nguyên kích thước cửa sổ, độ phân giải và vị trí các project trong CapCut.").pack(pady=8)
        bar = ttk.Frame(self)
        bar.pack(fill="x", padx=10)
        for title, command in (("Mở cấu hình", self.load), ("Lưu cấu hình", self.save),
                               ("Chọn ảnh thành công", self.choose_image), ("Lấy tọa độ sau 5s", self.capture)):
            ttk.Button(bar, text=title, command=command).pack(side="left", padx=3)
        self.editor = tk.Text(self, height=25, undo=True)
        self.editor.pack(fill="both", expand=True, padx=10, pady=8)
        self.replace(DEFAULT)
        bar = ttk.Frame(self)
        bar.pack(fill="x", padx=10)
        self.start_button = ttk.Button(bar, text="Chạy hàng đợi", command=self.start)
        self.start_button.pack(side="left")
        ttk.Button(bar, text="Dừng", command=self.stop_event.set).pack(side="left", padx=8)
        ttk.Label(bar, text="Khẩn cấp: đưa chuột về góc trên bên trái màn hình.").pack(side="left")
        self.logs = tk.Text(self, height=9, state="disabled")
        self.logs.pack(fill="x", padx=10, pady=8)
        self.protocol("WM_DELETE_WINDOW", self.close)
        self.after(100, self.poll)

    def replace(self, value):
        self.editor.delete("1.0", "end")
        self.editor.insert("1.0", json.dumps(value, indent=2, ensure_ascii=False))

    def config(self):
        return json.loads(self.editor.get("1.0", "end"))

    def load(self):
        path = filedialog.askopenfilename(filetypes=[("JSON", "*.json")])
        if path:
            try:
                self.replace(json.loads(Path(path).read_text(encoding="utf-8")))
            except Exception as exc:
                messagebox.showerror("Lỗi", str(exc))

    def save(self):
        try:
            config = self.config()
            path = filedialog.asksaveasfilename(defaultextension=".json", filetypes=[("JSON", "*.json")])
            if path:
                Path(path).write_text(json.dumps(config, indent=2, ensure_ascii=False), encoding="utf-8")
        except Exception as exc:
            messagebox.showerror("Lỗi", str(exc))

    def choose_image(self):
        path = filedialog.askopenfilename(filetypes=[("PNG", "*.png")])
        if path:
            try:
                config = self.config()
                config["completion_image"] = path
                self.replace(config)
            except Exception as exc:
                messagebox.showerror("Lỗi", str(exc))

    def capture(self):
        self.events.put("Đưa chuột đến vị trí cần lấy; đọc tọa độ trong nhật ký sau 5 giây.")
        self.after(5000, self.capture_now)

    def capture_now(self):
        try:
            import pyautogui
            x, y = pyautogui.position()
            self.events.put(f"Tọa độ: [{x}, {y}]")
        except Exception as exc:
            self.events.put(f"Lỗi lấy tọa độ: {exc}")

    def start(self):
        if self.worker and self.worker.is_alive():
            return
        try:
            import sys
            if sys.platform != "win32":
                raise RuntimeError("Tự động hóa CapCut cần chạy trên Windows có desktop tương tác.")
            import pyautogui
            config = validate(self.config())
            width, height = pyautogui.size()
            points = [p["position"] for p in config["projects"]] + [config[k] for k in
                      ("export_button", "confirm_export_button", "close_completion_button", "home_button")]
            if any(x >= width or y >= height for x, y in points):
                raise ValueError("Có tọa độ nằm ngoài màn hình chính.")
            pyautogui.FAILSAFE = True
            pyautogui.PAUSE = 0.2
        except Exception as exc:
            messagebox.showerror("Không thể chạy", str(exc))
            return
        self.stop_event.clear()
        self.start_button.configure(state="disabled")
        def work():
            try:
                Runner(pyautogui, self.stop_event, self.events.put).run(config)
            except Stopped:
                self.events.put("Đã dừng. CapCut có thể vẫn đang xuất video; kiểm tra trước khi chạy lại.")
            except Exception as exc:
                self.events.put(f"Đã dừng do lỗi: {type(exc).__name__}: {exc}")
            finally:
                self.events.put(None)
        self.worker = threading.Thread(target=work, daemon=True)
        self.worker.start()

    def poll(self):
        try:
            while True:
                event = self.events.get_nowait()
                if event is None:
                    self.start_button.configure(state="normal")
                else:
                    self.logs.configure(state="normal")
                    self.logs.insert("end", f"{time.strftime('%H:%M:%S')} {event}\n")
                    self.logs.see("end")
                    self.logs.configure(state="disabled")
        except queue.Empty:
            pass
        self.after(100, self.poll)

    def close(self):
        self.stop_event.set()
        if self.worker and self.worker.is_alive():
            self.after(100, self.close)
        else:
            self.destroy()


if __name__ == "__main__":
    App().mainloop()
