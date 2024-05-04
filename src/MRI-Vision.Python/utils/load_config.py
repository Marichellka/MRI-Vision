import yaml
import os

def load_config() -> dict:
    script_dir = os.path.dirname(__file__)
    config_path = os.path.join(script_dir, '../config.yml')
    with open(config_path) as f:
        config = yaml.load(f, Loader=yaml.FullLoader)
        return config['AutoEncoder']
