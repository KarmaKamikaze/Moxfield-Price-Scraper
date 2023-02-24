from os.path import exists
from dotenv import dotenv_values


class Settings:
    def __init__(self):
        if not exists('.env'):
            print("No .env file exist!")
            exit()
        config = dotenv_values(".env")  # take environment variables from .env
        # User specific settings
        self.price_target = float(config['PRICE_TARGET'])
        self.update_frequency = int(config['UPDATE_FREQUENCY'])
        self.moxfield_username = config['MOXFIELD_USERNAME']
        self.moxfield_password = config['MOXFIELD_PASSWORD']
        self.webdriver_path = config['WEBDRIVER_PATH']
        # Email settings
        self.send_mails = True if config['SEND_MAILS'].lower() == 'true' else False
        self.sender_email = config['SENDER_EMAIL_ADDRESS']
        self.email_password = config['SENDER_EMAIL_PASSWORD']
        self.receiver_email = config['RECEIVER_EMAIL_ADDRESS']