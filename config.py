import json
from os import makedirs
from os.path import exists


class Settings:
    def __init__(self):
        if not exists('settings.json'):
            print("No settings.json file exist!")
            exit()
        config = self.load_settings()
        # User specific settings
        self.price_target = config.get('PriceTarget')
        self.update_frequency = config.get('UpdateFrequency')
        self.moxfield_username = config.get('MoxfieldUsername')
        self.moxfield_password = config.get('MoxfieldPassword')
        self.webdriver_path = config.get('WebdriverPath')
        self.tail_logs = config.get('TailLogs')
        # Email settings
        self.send_mails = config.get('SendEmails')
        self.sender_email = config.get('SenderEmailAddress')
        self.email_password = config.get('SenderEmailPassword')
        self.receiver_email = config.get('ReceiverEmailAddress')
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

    def load_settings(self) -> dict:
        if not exists('settings.json'):
            return dict()
        return json.load(open('settings.json'))

    def save_settings(self) -> None:
        settings = dict()
        settings.update({'PriceTarget': self.price_target})
        settings.update({'UpdateFrequency': self.update_frequency})
        settings.update({'MoxfieldUsername': self.moxfield_username})
        settings.update({'MoxfieldPassword': self.moxfield_password})
        settings.update({'WebdriverPath': self.webdriver_path})
        settings.update({'SendEmails': self.send_mails})
        settings.update({'SenderEmailAddress': self.sender_email})
        settings.update({'SenderEmailPassword': self.email_password})
        settings.update({'ReceiverEmailAddress': self.receiver_email})
        settings.update({'TailLogs': self.tail_logs})
        with open('settings.json', 'w', encoding='utf-8') as settings_file:
            json.dump(settings, settings_file, ensure_ascii=False, indent=4)

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
