import torch.nn as nn
from torch.utils.tensorboard import SummaryWriter

import logging

from autoencoder import AutoEncoder

def train(dataset, model: AutoEncoder, epochs : int):
    writer = SummaryWriter()
    compute_loss = nn.MSELoss() # TODO: move to AutoEncoder

    step = 0
    for epoch in range(epochs):
        logging.info(f"epoch = {epoch}")

        model.encoder.train()
        model.decoder.train()

        epoch_loss = 0
        for data in dataset:
            input = data.to(model.device)
            encoded = model.encoder(input)
            restored = model.decoder(encoded)

            step_loss = compute_loss(restored, input)
            model.opimizer.zero_grad()
            step_loss.backward()
            model.opimizer.step()

            epoch_loss += step_loss.item()

            writer.add_scalar('step_loss', step_loss, step)
            step+=1

        epoch_loss = epoch_loss/len(dataset)

        logging.info(f"epoch loss: {epoch_loss}")
        writer.add_scalar("epoch_loss", epoch_loss, epoch)

    writer.close()



