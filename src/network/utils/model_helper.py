import torch
import sys, os

script_dir = os.path.dirname(__file__)
sys.path.append(os.path.join(script_dir, '../'))

from model.autoencoder import AutoEncoder
from utils.load_config import load_config
from utils.image_helper import preprocess_image

device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

def load_model(path: str) -> AutoEncoder:
    config = load_config()
    model = AutoEncoder(tuple(config['size']), device, blocks=5)
    model.load(path)

    return model    

def analyze_image(path: str, model: AutoEncoder) -> tuple:
    image = preprocess_image(path)
    with torch.no_grad():
        input = image.to(model.device)
        encoded = model.encoder(input)
        restored = model.decoder(encoded)
    
    [[input]] = input
    [[restored]] = restored
    return input.tolist(), restored.tolist()