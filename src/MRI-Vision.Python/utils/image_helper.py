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

from utils.load_config import Config

class ImageHelper:
    config = Config.load_config()
    
    @staticmethod
    def load_image(path: str) -> list:
        loadImage = Compose([
            LoadImage(image_only=True), 
            AddChannel(),
            Orientation(axcodes=ImageHelper.config['orientation']),
            Resize(spatial_size=ImageHelper.config['size']),
        ])
        [image] = loadImage(path).array
        return image.tolist()

    @staticmethod
    def preprocess_image(path: str):
        preprocessImage = Compose([
            LoadImage(image_only=True),
            AddChannel(),
            Orientation(axcodes=ImageHelper.config['orientation']),
            Resize(spatial_size=ImageHelper.config['size']), 
            ScaleIntensity(),
            EnsureType(),
            AddChannel(),
        ])

        return preprocessImage(path)
