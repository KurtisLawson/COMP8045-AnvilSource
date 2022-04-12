import torch
import torch.nn as nn
import torch.optim as optim
import numpy as np

from typing import Optional
import math
import random
from random import choice, randrange
from typing import List, Tuple

from Utils import load_island_data, MeshData

from Models import *

import matplotlib.pyplot as plt


# Creates a binary list from any integer
def create_binary_list_from_int(number: int) -> List[int]:
    if number < 0 or type(number) is not int:
        raise ValueError("Only Positive integers are allowed")

    return [int(x) for x in list(bin(number))[2:]]

# 
def convert_float_matrix_to_int_list(
    float_matrix: np.array, threshold: float = 0.5
) -> List[int]:
    """Converts generated output in binary list form to a list of integers
    Args:
        float_matrix: A matrix of values between 0 and 1 which we want to threshold and convert to
            integers
        threshold: The cutoff value for 0 and 1 thresholding.
    Returns:
        A list of integers.
    """
    return [
        int("".join([str(int(y)) for y in x]), 2) for x in float_matrix >= threshold
    ]

# A function to sample random batch of data from our true distribution.
def sample_island_data(labels, islands, batch_size):

    indices = []

    lab = []
    lab_channels = []
    isl = []

    # Grab N samples from the true distribution
    for j in range(0, batch_size):
        # Choose random number not already chosen
        sample = choice([i for i in range(0,len(islands)) if i not in indices])
        indices.append(sample)

        channels = []
        channels.append( islands[sample].x_channel )
        channels.append( islands[sample].y_channel )
        channels.append( islands[sample].z_channel )
        lab_channels.append(channels)

        lab.append(labels[sample])
        # isl.append(islands[sample].verts_vector)
        isl.append(islands[sample].x_channel)

        # Check if there are still samples remaining:
        if len(indices) >= len(islands):
            break

    # Once N samples are collected, return.
    return lab, isl, lab_channels

def generate_even_data(max_int: int, batch_size: int=16) -> Tuple[List[int], List[List[int]]]:
    # Get the number of binary places needed to represent the maximum number
    max_length = int(math.log(max_int, 2))

    # Sample batch_size number of integers in range 0-max_int
    sampled_integers = np.random.randint(0, int(max_int / 2), batch_size)

    # create a list of labels all ones because all numbers are even, this is our true distribution
    labels = [1] * batch_size

    # Generate a list of binary numbers for training.
    data = [create_binary_list_from_int(int(x * 2)) for x in sampled_integers]
    data = [([0] * (max_length - len(x))) + x for x in data]

    return labels, data

# Linear, Random Numbers Train function
def train_even_gen(max_int: int = 128,
    batch_size: int = 16,
    training_steps: int = 500,
    learning_rate: float = 0.001,
    print_output_every_n_steps: int = 10,
) -> Tuple[nn.Module]:
    """Trains the even GAN
    Args:
        max_int: The maximum integer our dataset goes to.  It is used to set the size of the binary
            lists
        batch_size: The number of examples in a training batch
        training_steps: The number of steps to train on.
        learning_rate: The learning rate for the generator and discriminator
        print_output_every_n_steps: The number of training steps before we print generated output
    Returns:
        generator: The trained generator model
        discriminator: The trained discriminator model
    """

    # This should be 7
    input_length = int(math.log(max_int, 2))

    # Model Init
    generator = EvenGenerator(input_length)
    discriminator = EvenDiscriminator(input_length)

    # DC Models
    # generator = DCGenerator(input_length, 3, None)
    # discriminator = DCDiscriminator(input_length, 3)

    # Optimizers
    generator_optimizer = torch.optim.Adam(generator.parameters(), lr=learning_rate)
    discriminator_optimizer = torch.optim.Adam(
        discriminator.parameters(), lr=learning_rate
    )

    # loss
    loss = nn.BCELoss()
    # true_isl_labels, true_isl_data = load_island_data()

    # print("Island Labels: ")
    # print(true_isl_labels)
    # print(true_isl_data)
    g_loss = []
    d_loss = []
    iterations = []

    for i in range(training_steps):
        # zero the gradients on each iteration
        generator_optimizer.zero_grad()

        # Create noisy input for generator
        # Need float type instead of int
        noise = torch.randint(0, 2, size=(batch_size, input_length)).float()
        # print("Generated noise:")
        # print(noise)

        # Send our noise into the generator.
        generated_data = generator(noise)

        # Generate examples of even real data
        true_labels, true_data = generate_even_data(max_int, batch_size=batch_size)
        true_labels = torch.tensor(true_labels).unsqueeze(1).float()
        true_data = torch.tensor(true_data).unsqueeze(1).float()

        # Train the generator
        # We invert the labels here and don't train the discriminator because we want the generator
        #   to make things the discriminator classifies as true.
        generator_discriminator_out = discriminator(generated_data)
        
        # Here's where the Generator's performance is evaluated against our true labels,
        #       based on the evaluation of the discriminator.
        generator_loss = loss(generator_discriminator_out, true_labels)
        g_loss.append(generator_loss.detach().numpy())
        print("Generator Accuracy: " + str(generator_loss.detach().numpy()))
        
        # The performance is backpropagated
        generator_loss.backward()
        generator_optimizer.step()

        # Train the discriminator on the true/generated data
        discriminator_optimizer.zero_grad()
        true_discriminator_out = discriminator(true_data)
        # print("\nDiscriminator's output on true data: ")
        # print (true_discriminator_out.size())

        true_discriminator_out = torch.reshape(true_discriminator_out, (batch_size, 1))
        # print (true_discriminator_out)
        true_discriminator_loss = loss(true_discriminator_out, true_labels)
        # print("True Discriminator Loss: ")
        # print(true_discriminator_loss)

        # add .detach() here think about this
        generator_discriminator_out = discriminator(generated_data.detach())
        # print(generator_discriminator_out.size())
        # print(torch.zeros(batch_size, 1).size())
        generator_discriminator_loss = loss(generator_discriminator_out, torch.zeros(batch_size, 1))

        discriminator_loss = (true_discriminator_loss + generator_discriminator_loss) / 2
        d_loss.append(discriminator_loss.detach().numpy())
        discriminator_loss.backward()
        discriminator_optimizer.step()

        iterations.append(i)
        if i % print_output_every_n_steps == 0:
            print(convert_float_matrix_to_int_list(generated_data))

    # Plot the loss/accuracy over the course of the training.
    plt.plot(iterations, g_loss, label='G Accuracy')
    plt.plot(iterations, d_loss, label="D Loss")
    plt.legend()
    plt.ylabel('Performance')
    plt.xlabel('Iterations')
    plt.show()

    return generator, discriminator

def generate_noise(batch_size):
    # return (torch.rand(batch_size, 1024*3)-0.5)*20 # 1 Vectorized Tensor, Size 3072 (32x32)
    return (torch.rand(batch_size, 1024)-0.5) # 1 Vectorized Tensor, Size 3072 (32x32)
    # return (torch.rand(3, 1024)-0.5)*20 # 3 Channels of size 1024 (32x32)


def generate_4D_noise(batch_size, num_channels, resolution):
    rand_data = (torch.rand((batch_size, num_channels, resolution, resolution))-0.5)

    # print("\nconstruct_4D_tensor : RANDOM DATA")
    # print( rand_data.size() )
    # print( rand_data )
    return rand_data

# Data must be [3][1024] - Channels / Data
def construct_4D_tensor(data, batch_size, num_channels, resolution):
    data_lists = []
    # print(data)

    # For each channel, construct a list.

    for i in range(0, batch_size):
        # print("\nconstruct_4D_tensor : BATCH " + str(i+1))

        batch = []

        for j in range(0, num_channels):
            
            # print("\tconstruct_4D_tensor : CHANNEL " + str(j+1))

            channel_data = data[i][j]
            channel_rows = [] # Append 32 lists of size 32.

            # Each channel has 1024 values.
            current_val_index = 0
            for y in range(0, resolution):
                channel_cols = []

                for x in range (0, resolution):
                    channel_cols.append(channel_data[current_val_index])
                    current_val_index+=1

                channel_rows.append(channel_cols)
            # At the end of this loop, we have channel data constructed as a 32x32.

            # print(channel_rows)
            batch.append(channel_rows)
            
        # At the end of this loop, we have the full batch
        data_lists.append(batch)

    tensor = torch.tensor(data_lists)

    # print("\nconstruct_4D_tensor : DATA")
    # print(tensor.size())
    # print(tensor)

    return tensor

# Linear, Island Train function
def train_vert_gen(max_int: int = 128,
    batch_size: int = 32,
    image_resolution: int = 32,
    num_channels: int = 1,
    training_steps: int = 10001,
    learning_rate: float = 0.002,
    print_output_every_n_steps: int = 1000,
) -> Tuple[nn.Module]:
    """Trains the even GAN
    Args:
        max_int: The maximum integer our dataset goes to.  It is used to set the size of the binary
            lists
        batch_size: The number of examples in a training batch
        training_steps: The number of steps to train on.
        learning_rate: The learning rate for the generator and discriminator
        print_output_every_n_steps: The number of training steps before we print generated output
    Returns:
        generator: The trained generator model
        discriminator: The trained discriminator model
    """
    input_length = int(math.log(max_int, 2))

    # Models
    generator = VertGenerator(input_length)
    discriminator = Discriminator(input_length)

    # dc_generator = DCVertGenerator(image_resolution*image_resolution*num_channels, num_channels, None)
    # dc_discriminator = DCVertDiscriminator(image_resolution, num_channels)

    # DC Models
    # generator = DCGenerator(3074, 3, None)
    # discriminator = DCDiscriminator(3074, 3)

    # Optimizers
    generator_optimizer = torch.optim.Adam(generator.parameters(), lr=learning_rate)
    discriminator_optimizer = torch.optim.Adam(discriminator.parameters(), lr=learning_rate)

    # dc_generator_optimizer = torch.optim.Adam(dc_generator.parameters(), lr=learning_rate)
    # dc_discriminator_optimizer = torch.optim.Adam(dc_discriminator.parameters(), lr=learning_rate)

    # loss
    loss = nn.BCELoss()
    # loss = nn.MarginRankingLoss()
    # loss = nn.L1Loss()
    
    true_isl_labels, true_isl_data = load_island_data()

    # print("Island Labels: ")
    # print(true_isl_labels)
    # print(true_isl_data)
    g_loss_over_time = []
    d_loss_over_time = []
    time = []

    for i in range(training_steps):
        # zero the gradients on each iteration
        generator_optimizer.zero_grad()

        # Create noisy input for generator
        # Need float type instead of int
        
        # Generate noise, this is where we can adjust the dimensions
        noise = generate_noise(batch_size)
        dc_noise = generate_4D_noise(batch_size, num_channels, image_resolution)

        # print("\nGenerated noise:")
        # print(noise.size())
        # print(noise)

        generated_data = generator(noise)
        # dc_generated_data = dc_generator(dc_noise)

        print("\nGenerated Data")
        print(generated_data.size())
        print(generated_data)

        # Generate examples of even real data
        true_labels, true_data, true_channels = sample_island_data(true_isl_labels, true_isl_data, batch_size)
        true_labels = torch.tensor(true_labels).unsqueeze(1).float()
        true_data = torch.tensor(true_data).unsqueeze(1).float()

        true_4D_data = construct_4D_tensor(true_channels, batch_size, num_channels, image_resolution) # 32x32, 1 channel, 4D Tensor
        # true_channels = torch.tensor(true_channels).unsqueeze(1).float()

        # print("\nTrue data (1D) (1, 1024):")
        # print( true_data.size() )
        # print( true_data )

        # print("\nTrue data (4D) (3, 1024):")
        # print( true_channels.size() )
        # print( true_channels )

        # Train the generator
        # We invert the labels here and don't train the discriminator because we want the generator
        #   to make things the discriminator classifies as true.
        #   For this to work the discriminator has to be decent.
        generator_discriminator_out = discriminator(generated_data)
        # dc_generator_discriminator_out = dc_discriminator(dc_generated_data)


        # print("\nDiscriminator's output on random data: ")
        # print (dc_generator_discriminator_out.size())
        # print (dc_generator_discriminator_out)

        generator_loss = loss(generator_discriminator_out, true_labels)
        g_loss_over_time.append(generator_loss.detach().numpy())
        # dc_generator_loss = loss(dc_generator_discriminator_out, true_labels)

        print("Random Generator Loss: ")
        print (generator_loss.detach().numpy())
        generator_loss.backward()
        generator_optimizer.step()

        # dc_generator_loss.backward()
        
        # Train the discriminator on the true/generated data
        discriminator_optimizer.zero_grad()
        true_discriminator_out = discriminator(true_data)
        # print("\nDiscriminator's output on true data: ")
        # print (true_discriminator_out)

        true_discriminator_out = torch.reshape(true_discriminator_out, (batch_size, 1))
        # print (true_discriminator_out)
        true_discriminator_loss = loss(true_discriminator_out, true_labels)
        # print("True Discriminator Loss: ")
        # print(true_discriminator_loss)

        # add .detach() here think about this
        generator_discriminator_out = discriminator(generated_data.detach())
        # print(generator_discriminator_out.size())
        # print(torch.zeros(batch_size, 1).size())
        generator_discriminator_loss = loss(generator_discriminator_out, torch.zeros(batch_size, 1))

        discriminator_loss = (
            true_discriminator_loss + generator_discriminator_loss
        ) / 2
        d_loss_over_time.append(discriminator_loss.detach().numpy())
        discriminator_loss.backward()
        discriminator_optimizer.step()

        time.append(i+1)

        # This is just to see progress as more training occurs
        if i % print_output_every_n_steps == 0:
            plt.plot(time, g_loss_over_time, label='G')
            plt.plot(time, d_loss_over_time, label="D")
            plt.legend()
            plt.ylabel('Loss')
            plt.xlabel('Iterations')
            plt.show()

    # END LOOP


    return generator, discriminator

# __name__ is a special variable that is used to ensure we only execute in the main file
if __name__ == "__main__":
    # loaded_labels, loaded_data = load_island_data()
    even_generator, even_discriminator = train_even_gen()
    vert_generator, vert_discriminator = train_vert_gen()

    # input_length = int(math.log(128, 2))
    # noise = torch.randint(0, 2, size=(16, input_length)).float()
    # generated_data = vert_generator(noise)


    print(vert_generator)
    print(vert_discriminator)

    torch.save(vert_generator, "vert_gen.model")
    torch.save(vert_discriminator, "vert_disc.model")


    # print(convert_float_matrix_to_int_list(generated_data))



