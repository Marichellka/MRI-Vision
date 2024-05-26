import torch.nn as nn
from torch import Tensor
import math

import logging

class DeConvBlock(nn.Module):
    def __init__(self, in_channels: int, out_channels: int, kernel: int = 3, stride: int = 2):
        super(DeConvBlock, self).__init__()

        self.conv_block = nn.Sequential(
            nn.ConvTranspose3d(in_channels, out_channels, kernel+1, stride=stride, padding=1),
            nn.ELU(),
            nn.Conv3d(out_channels, out_channels, kernel, padding=1),
            nn.ELU()
        )
        logging.info(f"Added convolution block to decoder (channels: {in_channels}->{out_channels})")

    
    def forward(self, input: Tensor) -> Tensor:
        return self.conv_block(input)


class DeResBlock(nn.Module):
    '''Residual block of 2 convolutional layers and skip connection used to upsample data'''
    def __init__(self, in_channels: int, out_channels: int, kernel: int = 3, stride: int = 2):
        super(DeResBlock, self).__init__()

        self.conv_block = nn.Sequential(
            nn.ConvTranspose3d(in_channels, out_channels, kernel+1, stride=stride, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(out_channels),
            nn.Conv3d(out_channels, out_channels, kernel, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(out_channels)
        )

        self.skip =  nn.Sequential(
            nn.ConvTranspose3d(in_channels, out_channels, kernel+1, stride=stride, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(out_channels),
        )
        logging.info(f"Added residual block to decoder (channels: {in_channels}->{out_channels})")
    

    def forward(self, input: Tensor)-> Tensor:
        '''Upsample data'''
        return self.skip(input) + self.conv_block(input)


class Decoder(nn.Module):
    '''Used to decode MRI picture using DeResBlocks'''

    def __init__(self, in_size: tuple[int, int, int], 
                 channels: int = 16, blocks: int = 4):
        super(Decoder, self).__init__()

        self.out_conv = nn.Conv3d(channels, 1, 3, padding=1)

        conv_layers = []
        for i in range(blocks):
            in_channels = channels * (2**(blocks-i))
            out_channels = in_channels//2
            conv_layers.append(DeResBlock(in_channels, out_channels))

        self.decode = nn.Sequential(*conv_layers)

        self.in_h, self.in_w, self.in_d = [math.ceil(size/(2**blocks)) for size in in_size]
        self.in_channels = channels*(2**blocks)
        self.flat_size = self.in_h*self.in_w*self.in_d*self.in_channels
        self.dense_out = nn.Linear(self.in_channels*2, self.flat_size)


    def forward(self, z: Tensor) -> Tensor:
        '''Reconstruct image from latent space'''
        z = self.dense_out(z)
        unflatten = z.view(-1, self.in_channels, self.in_h, self.in_w, self.in_d)
        decoded = self.decode(unflatten)
        out = self.out_conv(decoded)
        return out
