from glob import glob

import torch
from torch import Tensor
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
from utils.load_config import Config

SAVED_MODEL_PATH = './content/saved_model/'
TENSOR_BOARD_PATH = './content/tensor_board'

logging.basicConfig(level=20)

class TrainHelper:
    '''Helper class to train AutoEncoder'''

    def __init__(self, data_dir: str):
        # Load data
        data_files = glob(data_dir+'*.nii.gz')
        random.shuffle(data_files)

        test_size = int(0.2 * len(data_files))
        train_size = len(data_files) - test_size
        test_files = data_files[:test_size]
        train_files = data_files[-train_size:]
        logging.info(f"Train size: {len(train_files)}\nTest size: {len(test_files)}")

        config = Config.load_config()

        batch_size = 4
        workers = 12

        # Preprocess data
        pre_process = Compose([
            LoadImage(image_only=True),
            AddChannel(),
            Orientation(axcodes=config['orientation']),
            Resize(spatial_size=size), 
            ScaleIntensity(),
            EnsureType(),
        ])

        train_ds = CacheDataset(train_files, pre_process, num_workers=workers)
        self.train_loader = DataLoader(train_ds, batch_size=batch_size, shuffle=True)
        val_ds = CacheDataset(test_files, pre_process, num_workers=workers)
        self.val_loader = DataLoader(val_ds, batch_size=batch_size, shuffle=False)


    def train(self, model: AutoEncoder, epochs : int, epoch: int = 0, best_loss: int = 100) -> AutoEncoder:
        '''Train AutoEncoder for {epochs} starting from {epoch} with {best_loss}

        Returns:
            model: AutoEncoder
                Trained model
        '''
        with SummaryWriter(TENSOR_BOARD_PATH) as writer:
            for epoch in range(epoch, epochs):
                logging.info(f"epoch = {epoch}")
                epoch_loss = self.epoch(model, writer) # Run epoch
                writer.add_scalar("epoch_loss", epoch_loss, epoch)

                loss = self.evaluate_loss() # evaluate model
                if  loss < best_loss:
                    best_loss = loss
                    logging.info(f"New best lost: {best_loss}")
                    model.save(SAVED_MODEL_PATH+"best_model.pth") # save best model

                model.save(SAVED_MODEL_PATH+"model.pth", epoch, best_loss) # save last model
        
        return model
    
    def epoch(self, model: AutoEncoder, writer: SummaryWriter) -> float:
        '''Perform one epoch fo training'''
        step = 0
        model.encoder.train()
        model.decoder.train()

        epoch_loss = 0
        for data in self.train_loader:
            step_loss = self.step(model, data)
            epoch_loss += step_loss.item()
            writer.add_scalar('step_loss', step_loss, step)
            logging.info(f"Step {step} loss: {step_loss.item()}")
            step+=1

        epoch_loss = epoch_loss/len(self.train_loader)

        logging.info(f"epoch loss: {epoch_loss}")
        return epoch_loss

    
    def step(self, model: AutoEncoder, data: Tensor) -> Tensor:
        '''Perform one step of training'''
        input = data.to(model.device)
        encoded = model.encoder(input)
        restored = model.decoder(encoded)

        step_loss = model.loss(restored, input)
        step_loss.backward()
        model.opimizer.zero_grad()
        model.opimizer.step()

        return step_loss


    def evaluate_loss(self, model: AutoEncoder) -> float:
        '''Evaluate loss of model on validation data'''
        model.encoder.eval()
        model.decoder.eval()

        loss = 0.0
        with torch.no_grad():
            for data in self.val_loader:
                input = data.to(model.device)
                encoded = model.encoder(input)
                restored = model.decoder(encoded)

                loss+=model.loss(restored, input).item()

        return loss/len(self.val_loader)


if __name__ == '__main__':
    data_dir = './content/data/IXI-T2-extracted/'
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    config = Config.load_config()
    size = tuple(config['size'])

    model = AutoEncoder(size, device, lr=1e-3, blocks=config['blocks'])
    start_epoch, best_loss = model.train_load(SAVED_MODEL_PATH+"model.pth")
    train_helper = TrainHelper(data_dir)
    train_helper.train()
