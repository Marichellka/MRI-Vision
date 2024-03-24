from glob import glob

import torch.nn as nn
import torch
from torch.utils.tensorboard import SummaryWriter
from monai.data import CacheDataset, DataLoader
from monai.transforms import (
    Compose,
    LoadImageD,
    EnsureChannelFirstD,
    ScaleIntensityD,
    EnsureTypeD,
    ResizeD
)
import monai.transforms

import random
import logging

from autoencoder import AutoEncoder

BASE_PATH = './model/'
SAVED_MODEL_PATH = BASE_PATH+'saved_model/model.pth'
TENSOR_BOARD_PATH = BASE_PATH+'tensor_board'

logging.basicConfig(level=20)

def train(train_ds, eval_ds, model: AutoEncoder, epochs : int):
    writer = SummaryWriter(TENSOR_BOARD_PATH)

    step = 0
    best_loss = 100
    for epoch in range(epochs):
        logging.info(f"epoch = {epoch}")

        model.encoder.train()
        model.decoder.train()

        epoch_loss = 0
        for data in train_ds:
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

        epoch_loss = epoch_loss/len(train_ds)

        logging.info(f"epoch loss: {epoch_loss}")
        writer.add_scalar("epoch_loss", epoch_loss, epoch)

        loss = evaluate_loss(eval_ds, model)
        if  loss < best_loss:
            best_loss = loss
            logging.info(f"New best lost: {best_loss}")

            torch.save({
                'epoch': epoch,
                'encoder': model.encoder.state_dict(),
                'decoder': model.decoder.state_dict()
            }, SAVED_MODEL_PATH)

    writer.close()

def evaluate_loss(dataset, model: AutoEncoder):
    model.encoder.eval()
    model.decoder.eval()

    loss = 0.0
    with torch.no_grad():
        for data in dataset:
            input = data["file"].to(model.device)
            encoded = model.encoder(input)
            restored = model.decoder(encoded)

            loss+=model.loss(restored, input)

    return loss/len(dataset)


if __name__ == '__main__':
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    
    data_dir = './data/IXI-T2/'
    data_files = glob(data_dir+'*.nii.gz')
    data_files = data_files[:len(data_files)//2]
    random.shuffle(data_files)

    test_size = int(0.3 * len(data_files))
    train_size = len(data_files) - test_size
    test_files = [{"file": file} for file in data_files[:test_size]]
    train_files = [{"file": file} for file in data_files[-train_size:]]
    logging.info(f"Train size: {train_size}\nTest size: {test_size}")

    batch_size = 8
    workers = 12
    z_dim = 512
    epochs = 200

    size = (256, 256, 128)

    pre_process = Compose([
        LoadImageD(keys=["file"]),
        EnsureChannelFirstD(keys=["file"], channel_dim="no_channel"),
        ResizeD(keys=["file"], spatial_size=size), # mode=('trilinear', 'nearest')
        ScaleIntensityD(keys=["file"]),
        EnsureTypeD(keys=["file"])
    ])

    train_ds = CacheDataset(train_files, pre_process, num_workers=workers)
    train_loader = DataLoader(train_ds, batch_size=batch_size, shuffle=True)
    val_ds = CacheDataset(test_files, pre_process, num_workers=workers)
    val_loader = DataLoader(val_ds, batch_size=batch_size, shuffle=False)

    model = AutoEncoder(size, 1e-3, device, [0,1])

    train(train_ds, val_ds, model, epochs)



