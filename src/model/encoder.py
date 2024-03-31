import torch.nn as nn
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

    
    def forward(self, input):
        return self.conv_block(input)


class EnResBlock(nn.Module):
    def __init__(self, in_channels: int, out_channels: int, kernel: int = 3, stride: int = 2):
        super(EnResBlock, self).__init__()

        self.conv_block = nn.Sequential(
            nn.Conv3d(in_channels, in_channels, kernel, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(),
            nn.Conv3d(in_channels, out_channels, kernel, stride=stride, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(),
        )

        self.skip = nn.Sequential(
            nn.Conv3d(in_channels, out_channels, kernel, stride=stride, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(),
        )
        logging.info(f"Added residual block to encoder (channels: {in_channels}->{out_channels})")
    

    def forward(self, input):
        return self.skip(input) + self.conv_block(input)


class Encoder(nn.Module):
    def __init__(self, in_size: tuple[int, int, int], 
                 channels: int = 16, blocks: int = 4):
        super(Encoder, self).__init__()

        self.input_conv = nn.Sequential(
            nn.Conv3d(1, channels, 3, padding=1),
            nn.ReLU(),
            nn.BatchNorm3d(),
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


    def forward(self, x):
        x = self.input_conv(x)
        encoded = self.encode(x)
        flatten = encoded.view(-1, self.flat_size)
        z = self.dense(flatten)
        return z
