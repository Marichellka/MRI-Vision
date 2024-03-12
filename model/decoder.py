import torch.nn as nn
import math

import logging

class DeConvBlock(nn.Module):
    def __init__(self, in_channels: int, out_channels: int, kernel: int = 3, stride: int = 2):
        super(DeConvBlock, self).__init__()

        self.conv_block = nn.Sequential(
            nn.Conv3d(in_channels, out_channels, kernel, padding=kernel//2),
            nn.ELU(),
            nn.ConvTranspose3d(out_channels, out_channels, kernel+1, stride=stride, padding=kernel//2),
            nn.ELU()
        )
    
    def forward(self, input):
        return self.conv_block(input)
    

class Decoder(nn.Module):
    def __init__(self, in_size: tuple[int, int, int], z_dim: int = 512, 
                 channels: int = 16, blocks: int = 4):
        super(Decoder, self).__init__()

        self.out_conv = nn.Conv3d(channels, 1, 1),

        conv_layers = []
        for i in range(blocks):
            in_channels = channels * (2**(blocks-i))
            out_channels = in_channels/2
            conv_layers.append(DeConvBlock(in_channels, out_channels))
            logging.info(f"Added convolution block to decoder (channels: {in_channels}->{out_channels})")

        self.decode = nn.Sequential(*conv_layers)

        self.in_h, self.in_w, self.in_d = [math.ceil(size/(2**blocks)) for size in in_size]
        self.in_channels = channels*(2**blocks)
        self.flat_size = self.in_h*self.in_w*self.in_d*self.in_channels
        self.dense = nn.Linear(self.flat_size, z_dim)


    def forward(self, z):
        z = self.dense(z)
        unflatten = input.view(-1, self.in_channels, self.in_h, self.in_w, self.in_d)
        decoded = self.decode(unflatten)
        out = self.out_conv(decoded)
        return out
