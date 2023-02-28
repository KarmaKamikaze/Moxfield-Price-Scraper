import json
from os import makedirs
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
        # Load decklist
        self.__decklist_path = './Data/decklist.json'
        self.decklist = self.__load_decks()

    def __load_decks(self) -> dict:
        if not exists(self.__decklist_path):
            return dict()
        return json.load(open(self.__decklist_path))

    def __save_decks(self) -> None:
        if not exists(self.__decklist_path):
            try:
                makedirs('./Data')
            except OSError:
                print('Decklist cannot be saved. Insufficient privileges.')
                exit()

        with open(self.__decklist_path, 'w', encoding='utf-8') as decks_file:
            json.dump(self.decklist, decks_file, ensure_ascii=False, indent=4)

    def add_deck(self, name: str, url: str) -> bool:
        if name in self.decklist:
            return False
        else:
            self.decklist.update({name: url})
            self.__save_decks()
            return True

    def delete_deck(self, name: str) -> bool:
        if name in self.decklist:
            self.decklist.pop(name)
            self.__save_decks()
            return True
        else:
            return False
