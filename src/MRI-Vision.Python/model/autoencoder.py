import torch
import torch.nn as nn
import torch.optim as optim

import logging

from .encoder import Encoder
from .decoder import Decoder

class AutoEncoder:
    '''AutoEncoder to analyze MRI pictures'''
    
    def __init__(self, in_size: tuple[int, int, int], device: torch.device, lr: float = 1e-3, 
                 blocks:int = 4, device_ids: list[int] = [0]):
        
        encoder = Encoder(in_size, blocks=blocks).to(device)
        decoder = Decoder(in_size, blocks=blocks).to(device)

        self.encoder = nn.DataParallel(encoder, device_ids)
        self.decoder = nn.DataParallel(decoder, device_ids)

        parameters = list(self.encoder.parameters()) + list(self.decoder.parameters())
        self.opimizer = optim.Adam(parameters, lr)
        self.device = device

        self.loss = nn.MSELoss() 


    def train_load(self, path: str) -> tuple[int, float]:
        ''' 
        Load saved AutoEncoder from path for training.
        Model should contain state dictionaries for Endoder, Decoder, 
        Optim, Loss, best loss and last epoch 
        
        Returns:
            epoch: int
                Last epoch
            Best loss: int
                Last best loss
        '''
        model = torch.load(path, map_location=self.device)
        
        self.encoder.load_state_dict(model['encoder'])
        self.decoder.load_state_dict(model['decoder'])
        self.opimizer.load_state_dict(model['opimizer'])
        self.loss.load_state_dict(model['loss'])
        epoch = model['epoch']
        logging.info(f"Loaded model from epoch {epoch}")

        return epoch, model['best_loss']
    

    def load(self, path: str) -> None:
        '''
        Load AutoEncoder from path to use for analysis
        Model should contain state dictionaries for Endoder and Decoder
        '''
        model = torch.load(path, map_location=self.device)
        
        self.encoder.load_state_dict(model['encoder'])
        self.decoder.load_state_dict(model['decoder'])
    

    def save(self, path: str, epoch: int, best_loss: float) -> None:
        '''
        Save AutoEncoder to reuse for future traing
        '''
        torch.save({
            'epoch': epoch,
            'best_loss': best_loss,
            'encoder': self.encoder.state_dict(),
            'decoder': self.decoder.state_dict(),
            'opimizer': self.opimizer.state_dict(),
            'loss': self.loss.state_dict()
        }, path)


    def save(self, path: str) -> None:
        '''
        Save AutoEncoder to use for analysis
        '''
        torch.save({
            'encoder': self.encoder.state_dict(),
            'decoder': self.decoder.state_dict(),
        }, path)
