import shutil
from datetime import datetime
from pathlib import Path

from hloc import extract_features, match_features, localize_sfm, visualization
from hloc import reconstruction
from hloc import triangulation
from utils import (
    create_gps_dict,
    load_obj,
    find_via_gps,
    write_loc_pairs,
    prepare_image,
)


class Localization:
    """
    Contains everything related to model creation and the pose estimation.
    """

    def __init__(
        self,
        project_name: str,
        feature_name: str = "superpoint_real_time",
        matcher_name: str = "superglue_real_time",
    ):
        """
        Variables and parameters are initiated and can be changed here. This included the config files,
        the number of matches and the localization pairs.
        :param project_name: Name of the project.
        :param feature_name: Name of the configuration for the feature file.
        :param matcher_name: Name of the configuration for the matcher file.
        """
        # superglue_real_time
        self.project_name = project_name
        self.dataset = Path("datasets") / project_name
        # images used to create the model
        self.images = self.dataset / "images/"
        self.query_images = self.dataset / "query_images/"
        self.input_image_name = self.query_images / "query_image.jpg"
        self.dataset_image_name = self.images / "query_image.jpg"
        self.camera_intrinsics_folder = self.dataset / "camera_intrinsics"
        self.camera_intrinsics_file = (
            self.camera_intrinsics_folder / "queries_with_intrinsics.txt"
        )

        self.outputs = Path("outputs") / project_name
        self.output_dir = self.outputs / "localization"

        self.number_of_matches = 1
        self.current_time = None
        self.result = None
        self.set_current_time_and_result()

        self.final_model_folder = self.outputs / "final_model"
        self.final_model = self.final_model_folder / "model"
        self.feature_conf = extract_features.confs[feature_name]
        self.matcher_conf = match_features.confs[matcher_name]
        self.features = self.feature_conf["output"]
        self.feature_file = f"{self.features}.h5"
        self.match_file = f"{self.features}_{self.matcher_conf['output']}.h5"
        self.loc_pairs = self.dataset / "localization_pairs.txt"

    def set_current_time_and_result(self):
        self.current_time = datetime.now().strftime("%m_%d_%Y_%H:%M:%S")
        self.result = self.output_dir / f"{self.current_time}_{self.project_name}"

    def create_model(self):
        """
        Run SfM reconstruction from scratch on a set of images.
        Extract features, match features, then reconstruct using Colmap, finally triangulate the model.
        """
        sfm_pairs = self.outputs / "pairs-exhaustive.txt"
        sfm_dir = self.outputs / "sfm_superpoint+superglue"
        reference_sfm = sfm_dir / "models" / "0"
        extract_features.main(self.feature_conf, self.images, self.outputs)
        match_features.main(
            self.matcher_conf, sfm_pairs, self.features, self.outputs, exhaustive=True
        )

        reconstruction.main(
            sfm_dir,
            self.images,
            sfm_pairs,
            self.outputs / self.feature_file,
            self.outputs / self.match_file,
        )

        triangulation.main(
            self.final_model_folder,
            reference_sfm,
            self.images,
            sfm_pairs,
            self.outputs / self.feature_file,
            self.outputs / self.match_file,
            colmap_path="colmap",
        )
        create_gps_dict(self.images, self.outputs)
        self.create_folder_structure()

    def localize_image(self, image: bytes = []):
        """
        First extract features of the query image, pointing to the same feature file as the reference images.
        Generate localization pairs between query and one or several references images via GPS data.
        Match_features using the created list of pairs, pointing to the same match file as the reference images;
        Generate a list of queries with intrinsics
        Localize with the existing model.
        """
        self.set_current_time_and_result()

        gps_data = prepare_image(
            image=image,
            image_name=self.input_image_name,
            dataset=self.dataset_image_name,
            intrinsics=self.camera_intrinsics_file,
        )

        project_name = find_via_gps(gps_data, gps_coords=gps_dict)[0][0]
        gps_dict = load_obj(str(self.outputs) + "/GPS_Dict.pkl")
        loc_image = find_via_gps(gps_data, gps_dict)
        write_loc_pairs(
            loc_image,
            self.input_image_name,
            file_path=self.loc_pairs,
            number_of_matches=self.number_of_matches,
        )

        extract_features.main(self.feature_conf, self.query_images, self.output_dir)
        match_features.main(
            self.matcher_conf, self.loc_pairs, self.features, self.output_dir
        )

        localize_sfm.main(
            self.final_model,
            self.camera_intrinsics_file,
            self.loc_pairs,
            self.output_dir / self.feature_file,
            self.output_dir / self.match_file,
            self.result,
            covisibility_clustering=False,
        )

        with open(self.result) as f:
            data = f.read()
        return data

    def visualize_matching(self):
        try:
            visualization.visualize_loc(
                self.result,
                self.images,
                self.final_model,
                top_k_db=self.number_of_matches,
                image_location=self.output_dir / self.current_time,
            )
        except KeyError:
            pass

    def create_folder_structure(self):

        self.camera_intrinsics_folder.mkdir(parents=True, exist_ok=True)
        self.query_images.mkdir(parents=True, exist_ok=True)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.reset_feature_matcher_files()

    def reset_feature_matcher_files(self):
        shutil.copyfile(
            self.outputs / f"{self.feature_conf['output']}.h5",
            self.output_dir / f"{self.feature_conf['output']}.h5",
        )
        shutil.copyfile(
            self.outputs
            / f"{self.feature_conf['output']}_{self.matcher_conf['output']}.h5",
            self.output_dir
            / f"{self.feature_conf['output']}_{self.matcher_conf['output']}.h5",
        )
