from glob import glob

import torch
from torch.utils.tensorboard import SummaryWriter
from monai.data import CacheDataset, DataLoader
from monai.transforms import (
    Compose,
    LoadImageD,
    AddChannelD,
    ScaleIntensityD,
    EnsureTypeD,
    ResizeD
)

import random
import logging

from model.autoencoder import AutoEncoder

SAVED_MODEL_PATH = './results/saved_model/model.pth'
TENSOR_BOARD_PATH = './logs/tensor_board'

logging.basicConfig(level=20)

def train(train_loader: DataLoader, eval_loader: DataLoader, 
          model: AutoEncoder, epochs : int, epoch: int = 0):
    with SummaryWriter(TENSOR_BOARD_PATH) as writer:
        step = 0
        best_loss = 100
        for epoch in range(epoch, epochs):
            logging.info(f"epoch = {epoch}")

            model.encoder.train()
            model.decoder.train()

            epoch_loss = 0
            for data in train_loader:
                input = data["file"].to(model.device)
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
                model.save(SAVED_MODEL_PATH, epoch)


def evaluate_loss(data_loader: DataLoader, model: AutoEncoder):
    model.encoder.eval()
    model.decoder.eval()

    loss = 0.0
    with torch.no_grad():
        for data in data_loader:
            input = data["file"].to(model.device)
            encoded = model.encoder(input)
            restored = model.decoder(encoded)

            loss+=model.loss(restored, input).item()

    return loss/len(data_loader)


if __name__ == '__main__':
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    
    data_dir = './data/IXI-T2-extracted/'
    data_files = glob(data_dir+'*.nii.gz')
    data_files = data_files[:len(data_files)//3]
    random.shuffle(data_files)

    test_size = int(0.3 * len(data_files))
    train_size = len(data_files) - test_size
    test_files = [{"file": file} for file in data_files[:test_size]]
    train_files = [{"file": file} for file in data_files[-train_size:]]
    logging.info(f"Train size: {train_size}\nTest size: {test_size}")

    batch_size = 4
    workers = 12
    epochs = 200

    size = (160, 160, 96)

    pre_process = Compose([
        LoadImageD(keys=["file"]),
        AddChannelD(keys=["file"]),
        ResizeD(keys=["file"], spatial_size=size), # mode=('trilinear', 'nearest')
        ScaleIntensityD(keys=["file"]),
        EnsureTypeD(keys=["file"]),
    ])

    train_ds = CacheDataset(train_files, pre_process, num_workers=workers)
    train_loader = DataLoader(train_ds, batch_size=batch_size, shuffle=True)
    val_ds = CacheDataset(test_files, pre_process, num_workers=workers)
    val_loader = DataLoader(val_ds, batch_size=batch_size, shuffle=False)

    model = AutoEncoder(size, 1e-3, device, [0], blocks=5)
    start_epoch = model.load(SAVED_MODEL_PATH)+1

    train(train_loader, val_loader, model, epochs, epoch=start_epoch)
