import logging
from http.server import BaseHTTPRequestHandler
from http.server import HTTPServer

from apscheduler.schedulers.background import BackgroundScheduler

from localization import Localization
from utils import allocate_sight


def to_json(answer):
    """
    Gets the rotation and position and transforms it to a JSON format.
    Default answer, in case something went wrong and we still want to see a model, this can be removed.
    :param answer: String containing rotation and position coordinates.
    :return: Answer in Json Format
    """
    import json

    default_answer = {
        "rotation": [
            0.9933328925064184,
            0.10586746702934283,
            0.044306814130220924,
            -0.010897261640138333,
        ],
        "position": [0.6256692252915096, 0.4400523835631032, 4.285847850723913, ],
    }
    answer = answer.split()[1:]
    if len(answer) == 7:
        default_answer = {
            "rotation": [
                float(answer[0]),
                float(answer[1]),
                float(answer[2]),
                float(answer[3]),
            ],
            "position": [float(answer[4]), float(answer[5]), float(answer[6])],
        }
    json_answer = json.dumps(default_answer, sort_keys=True, indent=4, )

    return json_answer


class BasicHandler(BaseHTTPRequestHandler):
    def __init__(self):
        self.request_over = False
        self.current_name = ''
        sched = BackgroundScheduler(daemon=True)
        sched.add_job(self.clean_up, trigger="interval", seconds=5, id="cleaner")
        sched.start()
        logging.getLogger("apscheduler.executors.default").setLevel(logging.WARNING)

    def __call__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)

    def finish(self):
        if not self.wfile.closed:
            self.wfile.flush()
        self.wfile.close()
        self.rfile.close()

    def _set_headers(self):
        # triggers a log message
        self.send_response(200)
        self.send_header("Content-type", "json")
        self.end_headers()

    def do_POST(self):
        """
        Handling of the Post Request. First determines the sight against which to match.
        Then start the matching process and send the computed pose as answer.
        Afterwards the matching is visualized, this can be turned of for production.
        """
        print("Got a Post Request")
        self._set_headers()
        content_length = int(self.headers["Content-Length"])
        json_data = self.rfile.read(content_length)
        project_name = allocate_sight(json_data)
        try:
            getattr(self, project_name)
        except AttributeError:
            setattr(self, project_name, Localization(project_name))

        answer = getattr(self, project_name).localize_image(json_data)

        json_answer = to_json(answer)
        self.wfile.write(bytes(json_answer, "utf-8"))
        self.current_name = project_name
        self.request_over = True

    def clean_up(self):
        """
        Creating the visualization and resetting the files.
        """
        if self.request_over:
            try:
                getattr(self, self.current_name).visualize_matching()
            except FileNotFoundError:
                pass
            getattr(self, self.current_name).reset_feature_matcher_files()
            self.request_over = False
            print("clean up done")


def start_server(
        project_name: str, server_address=("0.0.0.0", 40020),
):
    """
    Start the sever at a given address and port.
    :param server_address: Address consisting of the ip, and the port.
    """
    handler = BasicHandler()
    httpd = HTTPServer(server_address, handler)
    print("Backend Server running ...")
    httpd.serve_forever()
