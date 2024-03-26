import torch
import torch.nn as nn
import torch.optim as optim

from collections import OrderedDict

from .encoder import Encoder
from .decoder import Decoder

class AutoEncoder:
    def __init__(self, in_size: tuple[int, int, int], lr: float, device, devide_ids):
        encoder = Encoder(in_size).to(device)
        decoder = Decoder(in_size).to(device)

        self.encoder = nn.DataParallel(encoder, devide_ids)
        self.decoder = nn.DataParallel(decoder, devide_ids)

        parameters = list(self.encoder.parameters()) + list(self.decoder.parameters())
        self.opimizer = optim.Adam(parameters, lr)
        self.device = device

        self.loss = nn.MSELoss() 

    def load(self, path: str):
        model = torch.load(path)
        encoder_state_dict = OrderedDict()
        decoder_state_dict = OrderedDict()

        for key, value in model['encoder']:
            encoder_state_dict[key[7:]] = value
        
        for key, value in model['decoder']:
            decoder_state_dict[key[7:]] = value
        
        self.encoder.load_state_dict(encoder_state_dict)
        self.decoder.load_state_dict(decoder_state_dict)
