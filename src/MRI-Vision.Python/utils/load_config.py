import yaml
import os

class Config:
    '''Helper class to load config'''

    @staticmethod
    def load_config(path: str = '../config.yml') -> dict:
        '''Load config data'''
        script_dir = os.path.dirname(__file__)
        config_path = os.path.join(script_dir, path)
        with open(config_path) as f:
            config = yaml.load(f, Loader=yaml.FullLoader)
            return config['AutoEncoder']
