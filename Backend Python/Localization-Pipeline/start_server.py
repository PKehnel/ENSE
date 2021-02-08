import argparse

from backendServer import start_server

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--project_name", type=str, required=False)
    args = parser.parse_args()
    start_server(project_name=args.project_name)
