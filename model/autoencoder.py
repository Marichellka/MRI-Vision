import torch.nn as nn
import torch.optim as optim

from encoder import Encoder
from decoder import Decoder

class AutoEncoder:
    def __init__(self, in_size: tuple[int, int, int], lr: float, device, devide_ids):
        self.encoder = Encoder(in_size).to(device)
        self.decoder = Decoder(in_size).to(device)

        self.encoder = nn.DataParallel(self.encoder, devide_ids)
        self.decoder = nn.DataParallel(self.decoder, devide_ids)

        parameters = list(self.encoder.parameters()) + list(self.decoder.parameters())
        self.opimizer = optim.Adam(parameters, lr)
        self.device = device

        self.loss = nn.MSELoss() 
    
    

