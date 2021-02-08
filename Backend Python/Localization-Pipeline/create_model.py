import argparse

from localization import Localization

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--project_name", type=str, required=True)
    args = parser.parse_args()
    loc = Localization(project_name=args.project_name)
    loc.create_model()
