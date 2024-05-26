import torch.nn as nn
from torch import Tensor
import math

import logging


class EnConvBlock(nn.Module):
    def __init__(self, in_channels: int, out_channels: int, kernel: int = 3, stride: int = 2):
        super(EnConvBlock, self).__init__()

        self.conv_block = nn.Sequential(
            nn.Conv3d(in_channels, in_channels, kernel, padding=1),
            nn.ELU(),
            nn.Conv3d(in_channels, out_channels, kernel, stride=stride, padding=1),
            nn.ELU()
        )
        logging.info(f"Added convolution block to encoder (channels: {in_channels}->{out_channels})")

    
    def forward(self, input: Tensor) -> Tensor:
        return self.conv_block(input)


class EnResBlock(nn.Module):
    '''Residual block of 2 convolutional layers and skip connection used to downsample data'''
    def __init__(self, in_channels: int, out_channels: int, kernel: int = 3, stride: int = 2):
        super(EnResBlock, self).__init__()

        self.conv_block = nn.Sequential(
            nn.Conv3d(in_channels, in_channels, kernel, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(in_channels),
            nn.Conv3d(in_channels, out_channels, kernel, stride=stride, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(out_channels),
        )

        self.skip = nn.Sequential(
            nn.Conv3d(in_channels, out_channels, kernel, stride=stride, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(out_channels),
        )
        logging.info(f"Added residual block to encoder (channels: {in_channels}->{out_channels})")
    

    def forward(self, input: Tensor) -> Tensor:
        '''Downsample data'''
        return self.skip(input) + self.conv_block(input)


class Encoder(nn.Module):
    '''Used to encode MRI picture using EnResBlocks'''

    def __init__(self, in_size: tuple[int, int, int], 
                 channels: int = 16, blocks: int = 4):
        super(Encoder, self).__init__()

        self.input_conv = nn.Sequential(
            nn.Conv3d(1, channels, 3, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(channels),
        )

        conv_layers = []
        for i in range(blocks):
            in_channels = channels * (2**i)
            out_channels = in_channels*2
            conv_layers.append(EnResBlock(in_channels, out_channels))
        self.encode = nn.Sequential(*conv_layers)

        flat_h, flat_w, flat_d = [math.ceil(size/(2**blocks)) for size in in_size]
        self.flat_size = flat_h*flat_w*flat_d*channels*(2**blocks)
        self.dense = nn.Linear(self.flat_size, channels*(2**blocks)*2)


    def forward(self, x: Tensor) -> Tensor:
        '''Encode picture data into 1D latent space'''
        x = self.input_conv(x)
        encoded = self.encode(x)
        flatten = encoded.view(-1, self.flat_size)
        z = self.dense(flatten)
        return z
