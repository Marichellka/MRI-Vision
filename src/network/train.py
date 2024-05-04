from glob import glob

import torch
from torch.utils.tensorboard import SummaryWriter
from monai.data import CacheDataset, DataLoader
from monai.transforms import (
    Compose,
    LoadImage,
    AddChannel,
    ScaleIntensity,
    EnsureType,
    Orientation,
    Resize
)

import random
import logging

from model.autoencoder import AutoEncoder
from utils.load_config import load_config

SAVED_MODEL_PATH = './content/saved_model/'
TENSOR_BOARD_PATH = './content/tensor_board'

logging.basicConfig(level=20)

def train(train_loader: DataLoader, eval_loader: DataLoader, 
          model: AutoEncoder, epochs : int, epoch: int = 0, best_loss: int = 100):
    with SummaryWriter(TENSOR_BOARD_PATH) as writer:
        step = 0
        for epoch in range(epoch, epochs):
            logging.info(f"epoch = {epoch}")

            model.encoder.train()
            model.decoder.train()

            epoch_loss = 0
            for data in train_loader:
                input = data.to(model.device)
                encoded = model.encoder(input)
                restored = model.decoder(encoded)

                step_loss = model.loss(restored, input)
                model.opimizer.zero_grad()
                step_loss.backward()
                model.opimizer.step()

                epoch_loss += step_loss.item()

                writer.add_scalar('step_loss', step_loss, step)
                logging.info(f"Step loss: {step_loss.item()}")
                step+=1

            epoch_loss = epoch_loss/len(train_loader)

            logging.info(f"epoch loss: {epoch_loss}")
            writer.add_scalar("epoch_loss", epoch_loss, epoch)

            loss = evaluate_loss(eval_loader, model)
            if  loss < best_loss:
                best_loss = loss
                logging.info(f"New best lost: {best_loss}")
                model.save(SAVED_MODEL_PATH+"best_model.pth")

            model.save(SAVED_MODEL_PATH+"model.pth", epoch, best_loss)


def evaluate_loss(data_loader: DataLoader, model: AutoEncoder):
    model.encoder.eval()
    model.decoder.eval()

    loss = 0.0
    with torch.no_grad():
        for data in data_loader:
            input = data.to(model.device)
            encoded = model.encoder(input)
            restored = model.decoder(encoded)

            loss+=model.loss(restored, input).item()

    return loss/len(data_loader)


if __name__ == '__main__':
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    
    data_dir = './content/data/IXI-T2-extracted/'
    data_files = glob(data_dir+'*.nii.gz')
    random.shuffle(data_files)

    test_size = int(0.2 * len(data_files))
    train_size = len(data_files) - test_size
    test_files = data_files[:test_size]
    train_files = data_files[-train_size:]
    logging.info(f"Train size: {len(train_files)}\nTest size: {len(test_files)}")

    config = load_config()

    batch_size = 4
    workers = 12
    epochs = 200

    size = tuple(config['size'])

    pre_process = Compose([
        LoadImage(image_only=True),
        AddChannel(),
        Orientation(axcodes=config['orientation']),
        Resize(spatial_size=size), 
        ScaleIntensity(),
        EnsureType(),
    ])

    train_ds = CacheDataset(train_files, pre_process, num_workers=workers)
    train_loader = DataLoader(train_ds, batch_size=batch_size, shuffle=True)
    val_ds = CacheDataset(test_files, pre_process, num_workers=workers)
    val_loader = DataLoader(val_ds, batch_size=batch_size, shuffle=False)

    model = AutoEncoder(size, device, lr=1e-3, blocks=config['blocks'])
    start_epoch, best_loss = model.train_load(SAVED_MODEL_PATH+"model.pth")

    train(train_loader, val_loader, model, epochs, epoch=start_epoch+1, best_loss=best_loss)
