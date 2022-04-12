import torch
import torch.nn as nn
import torch.optim as optim
import numpy as np

import math

from typing import Optional

# Generators
class EvenGenerator(nn.Module):

    def __init__(self, input_length: int):
        super(EvenGenerator, self).__init__()
        self.dense_layer = nn.Linear(int(input_length), int(input_length))
        self.activation = nn.Sigmoid()

    def forward(self, x):
        return self.activation(self.dense_layer(x))


class VertGenerator(nn.Module):

    def __init__(self, input_length: int):
        super(VertGenerator, self).__init__()

        # DC Layers
        self.layers_list = []
        # self.layers_list.append(nn.Linear(in_features=(1024*3), out_features=(1024*3)))
        self.layers_list.append(nn.Linear(in_features=(1024), out_features=(1024)))
        self.layers_list.append(nn.Hardtanh(-10.0, 10.0))

        # self.layers_list.append(nn.Upsample (scale_factor=2))

        # self.layers_list.append(nn.BatchNorm2d(128))

        self.layers = nn.ModuleList(self.layers_list)

    def forward(self, x):
        for i, layer in enumerate(self.layers):
            x = layer(x)
        return x


class DCVertGenerator(nn.Module):
    def __init__(self, input_length: int, n_channels: int,  num_base_filters: Optional[int]):
        super(DCVertGenerator, self).__init__()

        # Calculates the total number of layers
        number_of_layers = int(math.log(32, 2) - 3)

        self.num_base_filters = num_base_filters

        if self.num_base_filters is None:
            self.num_base_filters = 32 * 2 ** number_of_layers

        # Create the list to hold all sequential layers
        self.layers_list = []

        # Add the initial layer
        self.layers_list.append(nn.Linear(input_length, num_base_filters))
        self.layers_list.append(nn.ReLU())

        # Add a scaled number of layers
        # We apply batch normalization after each layer to reduce covariant shift
        self.layers_list.append(nn.BatchNorm2d(n_channels))
        self.layers_list.append(nn.Upsample(scale_factor=2))

        # First Convolution layer
        self.layers_list.append(nn.Conv2d(n_channels, 32, 32, stride=1, padding=1))
        self.layers_list.append(nn.BatchNorm2d(32, 0.8))
        self.layers_list.append(nn.LeakyReLU(0.2, inplace=True))
        self.layers_list.append(nn.Upsample(scale_factor=2))
        
        # Second Convolution
        self.layers_list.append(nn.Conv2d(32, 16, n_channels, stride=1, padding=1))
        self.layers_list.append(nn.BatchNorm2d(16, 0.8))
        self.layers_list.append(nn.LeakyReLU(0.2, inplace=True))

        # Third convolution
        self.layers_list.append(nn.Conv2d(16, n_channels, n_channels, stride=1, padding=1))
        self.layers_list.append(nn.BatchNorm2d(16, 0.8))
        self.layers_list.append(nn.LeakyReLU(0.2, inplace=True))

        # Final activation, Hardtan between max and min distance from origin (in this case 10)
        self.layers_list.append(nn.Hardtanh(-10.0, 10.0))

        self.layers = nn.ModuleList(self.layers_list)

    def forward(self, x):
        for i, layer in enumerate(self.layers):
            x = layer(x)
        return x

# Discriminators
class EvenDiscriminator(nn.Module):
    def __init__(self, input_length: int):
        super(EvenDiscriminator, self).__init__()
        # self.dense = nn.Linear(1024*3, 1)

        self.model = nn.Sequential(
            nn.Linear(input_length, 1),
            nn.Sigmoid()
        )

    def forward(self, x):
        return self.model(x)
        return self.activation(self.dense(x))

class Discriminator(nn.Module):
    def __init__(self, input_length: int):
        super(Discriminator, self).__init__()
        # self.dense = nn.Linear(1024*3, 1)

        self.model = nn.Sequential(
            nn.Linear(1024, 1),
            nn.Sigmoid()
        )

    def forward(self, x):
        return self.model(x)
        return self.activation(self.dense(x))

class DCVertDiscriminator(nn.Module):
    def __init__(self, image_size: int, input_channels: int):
        super(DCVertDiscriminator, self).__init__()
        self.model = nn.Sequential(
            nn.Conv2d(input_channels, 32, 3, stride=2, padding=1),
            nn.ELU(),
            nn.Dropout2d(0.2),
            nn.Conv2d(32, 64, 3, stride=2, padding=1),
            nn.ELU(),
            nn.BatchNorm2d(64, 0.8),
            nn.Conv2d(64, 128, 3, stride=2, padding=1),
            nn.ELU(),
            nn.BatchNorm2d(128, 0.8),
            nn.Conv2d(128, input_channels, 3, stride=2, padding=1),
            nn.ELU(),
            nn.Linear(2, 1),
            nn.Sigmoid()
        )

    def forward(self, x):
        return self.model(x)