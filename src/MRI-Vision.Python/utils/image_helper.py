from torch import Tensor
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
    '''Helper to read and process MRI picture'''

    config = Config.load_config()
    
    @staticmethod
    def load_image(path: str) -> list:
        '''Load image from the file to raw data list'''
        loadImage = Compose([
            LoadImage(image_only=True), 
            AddChannel(),
            Orientation(axcodes=ImageHelper.config['orientation']),
            Resize(spatial_size=ImageHelper.config['size']),
        ])
        [image] = loadImage(path).array
        return image.tolist()

    @staticmethod
    def preprocess_image(path: str) -> Tensor:
        '''Load and preprocess image for analysis'''
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
