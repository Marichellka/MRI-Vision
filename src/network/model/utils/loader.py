import torch
from monai.data import CacheDataset, DataLoader
from monai.transforms import (
    Compose,
    LoadImage,
    AddChannel,
    ScaleIntensity,
    EnsureType,
    Resize,
    Orientation
)

import sys
sys.path.append('C:\\Users\\maric\\Studying\\Diploma\\Project\\MRI-Vision\\src\\network\\')
from model.autoencoder import AutoEncoder

size = (160, 160, 96)
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

def load_model(path: str) -> AutoEncoder:
    model = AutoEncoder(size, device, blocks=5)
    model.load(path)

    return model


def load_data(files: list[str]) -> CacheDataset:
    pre_process = Compose([
        LoadImage(image_only=True),
        AddChannel(),
        Orientation(axcodes='LAS'),
        Resize(spatial_size=size), 
        ScaleIntensity(),
        EnsureType(),
    ])

    dataset = CacheDataset(files, pre_process)
    return dataset  


def load_image(path: str, size: tuple[int, int, int] = (160, 160, 96)):
    loadImage = Compose([
        LoadImage(image_only=True), 
        AddChannel(),
        Orientation(axcodes='LAS'),
        Resize(spatial_size=size),
    ])
    [image] = loadImage(path).array
    return image.tolist()


def process_image(path: str, model: AutoEncoder) -> tuple:
    loadImage = Compose([
        LoadImage(image_only=True),
        AddChannel(),
        Orientation(axcodes='LAS'),
        Resize(spatial_size=(160, 160, 96)), 
        ScaleIntensity(),
        EnsureType(),
        AddChannel()
    ])
    image = loadImage(path)
    with torch.no_grad():
        input = image.to(model.device)
        encoded = model.encoder(input)
        restored = model.decoder(encoded)

        anomaly = abs(input-restored)
    
    [[input]] = input
    [[anomaly]] = anomaly
    return input.tolist(), anomaly.tolist()
