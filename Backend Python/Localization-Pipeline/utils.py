import pickle
from operator import itemgetter
from pathlib import Path

import exifread
import geopy.distance
import numpy as np
from exifread.utils import get_gps_coords


def create_gps_dict(image_folder, output_file):
    """
    Create a dictionary containing the GPS coordinates for all images in the given filepath.
    :param output_file: Place to save the dictionary as pickle object.
    :param image_folder: Filepath to the images, should only contain JPG data.
    """
    gps_coords = {}
    for filepath in image_folder.iterdir():
        image_name = filepath.stem
        tags = exifread.process_file(open(filepath, "rb"))
        coords = get_gps_coords(tags)
        if coords is not None:
            gps_coords[image_name] = coords
    mean_coords = np.mean(list(gps_coords.values()), axis=0)
    add_sight_gps(output_file.stem, tuple(mean_coords))
    save_obj(gps_coords, str(output_file) + "/GPS_Dict.pkl")


def add_sight_gps(sight_name: str, mean_coords: tuple):
    """
    Add a sight to a global dictionary, containing all created models.
    :param sight_name:
    :param mean_coords:
    """
    sights_gps_dict_path = Path("outputs/sights_gps_dict.pkl")
    if not sights_gps_dict_path.is_file():
        save_obj(dict(), str(sights_gps_dict_path))
    sights_gps_dict = load_obj(sights_gps_dict_path)
    sights_gps_dict[sight_name] = mean_coords
    save_obj(sights_gps_dict, sights_gps_dict_path)


def find_via_gps_image(image, gps_coords: dict):
    """
    Creates an Array containing the distance between the given image and all images in a given dictionary.
    :param image: Jpeg Image with Exif Data.
    :param gps_coords: Dictionary with GPS Data for images.
    :return: Sorted array with distances between input and dictionary images.
    """
    tags = exifread.process_file(open(image, "rb"))
    coords = get_gps_coords(tags)
    pairs_distance = []
    for key, value in gps_coords.items():
        distance = geopy.distance.distance(value, coords).meters
        pairs_distance.append([key, distance])
    pairs_distance = sorted(pairs_distance, key=itemgetter(1))
    return pairs_distance


def find_via_gps(coords, gps_coords: dict):
    """
   Creates an Array containing the distance between the given coordinates and all images in a given dictionary.
   :param coords: GPS Coordinates.
   :param gps_coords: Dictionary with GPS Data for images.
   :return: Sorted array with distances between input and dictionary images.
    """
    pairs_distance = []
    for key, value in gps_coords.items():
        distance = geopy.distance.distance(value, coords).meters
        pairs_distance.append([key, distance])
    pairs_distance = sorted(pairs_distance, key=itemgetter(1))
    return pairs_distance


def save_obj(obj, name):
    with open(name, "wb") as f:
        pickle.dump(obj, f, pickle.HIGHEST_PROTOCOL)


def load_obj(name):
    with open(name, "rb") as f:
        return pickle.load(f)


def write_loc_pairs(
    nearest_image, query_image, file_path, number_of_matches=2,
):
    """
    Create  a list of localization pairs between the query image and one or several references images.
    :param nearest_image: Reference images, closest to the query via gps.
    :param query_image: Query Image to localize.
    :param file_path: Location to store the file.
    :param number_of_matches: Number of images that should be considered.
    """
    with open(file_path, "w+") as f:
        for i in range(number_of_matches):
            expression = query_image.stem + ".jpg" + " " + nearest_image[i][0] + ".jpg"
            if i + 1 < number_of_matches:
                expression += "\n"
            f.write(expression)


def prepare_image(
    image: bytes, image_name: Path, dataset: Path, intrinsics: Path
) -> tuple:
    """
    Processes a byte data that contains a JPG and additional information.
    Creating an JPG image writing it to a file, thereby also creating a file containing
    the camera intrinsics. Finally extracting and returning the GPS data of the image.
    :param image: Jpeg Image as bytes.
    :param image_name: Name and location under which the image will be saved.
    :param dataset: Second location where the image will be saved
    :param intrinsics: Path where the intrinsics will be saved.
    :return: Tuple containing GPS data of the image.
    """
    import PIL.Image as Image
    import io

    index = image.find("rotationZ".encode()) + len("rotationZ")
    rotation = str(image[index : index + 4])
    end = rotation.find(",")
    rotation = int(rotation[2:end])
    index = image.find("jpg".encode()) + 4 + 4
    raw_image = Image.open(io.BytesIO(image[index:-48]))
    rotated_image = raw_image.rotate(rotation, expand=True)
    rotated_image.save(image_name)
    rotated_image.save(dataset)

    with open(intrinsics, "w+") as f:

        """
        # Intrinsics file is build in the following order:
        #         'model': camera_model,
        #         'width': width,
        #         'height': height,
        #         'params':
        # the parameter order depends on colmap: https://github.com/colmap/colmap/blob/master/src/base/camera_models.h
        """

        model = ["PINHOLE", "SIMPLE_RADIAL", "SIMPLE_PINHOLE"]
        words = ["resolution", "focalLength", "principalPoint"]
        text = image_name.name + " " + model[0]
        for word in words:
            index = image.find(word.encode()) + len(word)
            values = str(image[index : index + 16])
            start = values.find("(") + 1
            separator = values.find(",")
            end = values.find(")")
            value1, value2 = values[start:separator], values[separator + 2 : end]
            if rotation == 90 or rotation == 270:
                value1, value2 = value2, value1
            text += " " + value1 + " " + value2
        f.write(text)

    gps = extract_gps(image)
    return gps


def extract_gps(image) -> tuple:
    """
    Extract GPS of the image.
    :param image: Jpeg Image as bytes.
    :return: GPS (latitude, longitude) as tuple value.
    """
    gps = {"latitude": None, "longitude": None}
    for word in gps.keys():
        index = image.find(word.encode()) + len(word)
        values = str([image[index : index + 12]])
        end = max(values.find(","), values.find("\\r"))
        gps[word] = float(values[3:end])
    return tuple(gps.values())


def allocate_sight(json_data: bytes) -> str:
    """
    Using the GPS of the image, determine against which model to run the matching.
    :param json_data: Jpeg Image as bytes.
    :return: Project name as string.
    """
    gps_coords = load_obj(Path("outputs/sights_gps_dict.pkl"))
    coords = extract_gps(json_data)
    project_name = find_via_gps(coords=coords, gps_coords=gps_coords)[0][0]
    return project_name
