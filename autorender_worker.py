"""Line-oriented backend for the WPF application. stdin 'stop' cancels the queue."""
import json
import sys
import threading
from pathlib import Path
from autorender import Runner, Stopped, validate


def serve(config, gui, commands=sys.stdin, log=print):
    stop = threading.Event()

    def listen():
        for line in commands:
            if line.strip().lower() == "stop":
                stop.set()
                return
        # If the frontend disappears, do not keep clicking unattended.
        stop.set()

    threading.Thread(target=listen, daemon=True).start()
    try:
        Runner(gui, stop, log).run(config)
        return 0
    except Stopped:
        log("Đã dừng. Kiểm tra trạng thái CapCut trước khi chạy lại.")
        return 2
    except Exception as exc:
        log(f"Lỗi: {type(exc).__name__}: {exc}")
        return 1


def main():
    try:
        if sys.platform != "win32":
            raise RuntimeError("Backend xuất CapCut chỉ hỗ trợ Windows có desktop tương tác.")
        if len(sys.argv) != 2:
            raise ValueError("Cần đường dẫn file cấu hình JSON.")
        import pyautogui
        config = validate(json.loads(Path(sys.argv[1]).read_text(encoding="utf-8")))
        width, height = pyautogui.size()
        points = [p["position"] for p in config["projects"]] + [config[k] for k in
                  ("export_button", "confirm_export_button", "close_completion_button", "home_button")]
        if any(x >= width or y >= height for x, y in points):
            raise ValueError("Có tọa độ nằm ngoài màn hình chính.")
        pyautogui.FAILSAFE = True
        pyautogui.PAUSE = 0.2
        return serve(config, pyautogui)
    except Exception as exc:
        print(f"Không thể chạy: {exc}", flush=True)
        return 1


if __name__ == "__main__":
    sys.exit(main())
