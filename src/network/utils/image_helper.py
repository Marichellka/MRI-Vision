from monai.transforms import (
    Compose,
    LoadImage,
    AddChannel,
    ScaleIntensity,
    EnsureType,
    Resize,
    Orientation
)

import sys, os

script_dir = os.path.dirname(__file__)
sys.path.append(os.path.join(script_dir, '../'))

from utils.load_config import load_config

config = load_config()

def load_image(path: str):
    print(config)
    loadImage = Compose([
        LoadImage(image_only=True), 
        AddChannel(),
        Orientation(axcodes=config['orientation']),
        Resize(spatial_size=config['size']),
    ])
    [image] = loadImage(path).array
    return image.tolist()


def preprocess_image(path: str):
    preprocessImage = Compose([
        LoadImage(image_only=True),
        AddChannel(),
        Orientation(axcodes=config['orientation']),
        Resize(spatial_size=config['size']), 
        ScaleIntensity(),
        EnsureType(),
        AddChannel(),
    ])

    return preprocessImage(path)

