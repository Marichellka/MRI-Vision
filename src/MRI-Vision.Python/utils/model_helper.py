import torch
import sys, os

script_dir = os.path.dirname(__file__)
sys.path.append(os.path.join(script_dir, '../'))

from model.autoencoder import AutoEncoder
from utils.load_config import Config 
from utils.image_helper import ImageHelper

class ModelHelper:
    '''Helper class to use AutoEncoder'''

    @staticmethod
    def load_model(path: str) -> AutoEncoder:
        '''Load AutoEncoder from a file'''
        device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
        config = Config.load_config()
        model = AutoEncoder(tuple(config['size']), device, blocks=config['blocks'])
        model.load(path)

        return model
    
    @staticmethod
    def analyze_image(path: str, model: AutoEncoder) -> tuple[list, list]:
        '''Load and analyze image using AutoEncoder'''
        image = ImageHelper.preprocess_image(path)
        with torch.no_grad():
            input = image.to(model.device)
            encoded = model.encoder(input)
            restored = model.decoder(encoded)
        
        [[input]] = input
        [[restored]] = restored
        return input.tolist(), restored.tolist()
