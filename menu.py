import os
from time import sleep
from config import Settings
from moxfield_scraper import Scraper


class Menu:
    def __init__(self, config: Settings):
        self.__config = config
        self.__logo = ' █▄ ▄█ ▄▀▄ ▀▄▀ █▀ █ ██▀ █   █▀▄   █▀▄ █▀▄ █ ▄▀▀ ██▀   ▄▀▀ ▄▀▀ █▀▄ ▄▀▄ █▀▄ ██▀ █▀▄\n' \
                      ' █ ▀ █ ▀▄▀ █ █ █▀ █ █▄▄ █▄▄ █▄▀   █▀  █▀▄ █ ▀▄▄ █▄▄   ▄██ ▀▄▄ █▀▄ █▀█ █▀  █▄▄ █▀▄'
        self.__options = [
            'Start scraping',
            'Tail logs',
            'Add deck',
            'Remove deck',
            'Settings',
            'Quit'
        ]

    def __clear(self) -> None:
        os.system('cls' if os.name == 'nt' else 'clear')

    def display_menu(self) -> None:
        while 1:
            self.__clear()
            has_decks = len(self.__config.decklist) > 0

            print(self.__logo + '\n\n')
            if has_decks:
                self.__display_decks()
            self.__print_options(has_decks)
            self.__redirect(has_decks)

    def __print_options(self, has_decks: bool) -> None:
        print('\nOptions:\n')
        # also check if there are any logs when logs have been implemented
        print(
            (('1. ' + self.__options[0] + '\n') if has_decks else '') +
            (('2. ' + self.__options[1] + '\n') if has_decks else ('1. ' + self.__options[1] + '\n')) +
            (('3. ' + self.__options[2] + '\n') if has_decks else ('2. ' + self.__options[2] + '\n')) +
            (('4. ' + self.__options[3] + '\n') if has_decks else '') +
            (('5. ' + self.__options[4] + '\n') if has_decks else ('3. ' + self.__options[4] + '\n')) +
            (('6. ' + self.__options[5] + '\n') if has_decks else ('4. ' + self.__options[5] + '\n'))
        )

    def __redirect(self, has_decks: bool) -> None:
        valid_input = False
        while not valid_input:
            choice = input('\n> ')
            match choice:
                case '1':
                    valid_input = True
                    if has_decks: # Start scraping
                        for name, url in self.__config.decklist.items():
                            Scraper(self.__config).scrape_price(url)
                    else: # Tail logs
                        pass  # display tailing logs
                case '2':
                    valid_input = True
                    if has_decks: # Tail logs
                        pass  # display tailing logs
                    else: # Add deck
                        self.__new_deck()
                case '3':
                    valid_input = True
                    if has_decks: # Add deck
                        self.__new_deck()
                    else: # Settings
                        pass # display and offer to change settings
                case '4':
                    valid_input = True
                    if has_decks: # Remove deck
                        self.__remove_deck()
                    else:  # Quit
                        exit()
                case '5':
                    if has_decks: # Settings
                        valid_input = True
                        pass # display and offer to change settings
                    else:
                        valid_input = False
                case '6':
                    if has_decks: # Quit
                        valid_input = True
                        exit()
                    else:
                        valid_input = False
                case _:
                    print('Error in input. Please try again.')
                    self.__redirect(has_decks)

    def __new_deck(self) -> None:
        self.__clear()
        name = input('Deck name: ')
        url = input('Deck URL: ')
        if self.__config.add_deck(name, url):
            print(name + ' added to decklist.')
            sleep(5)
        else:
            print('Decklist already contain a deck with the name: ' + name)
            sleep(5)

    def __remove_deck(self) -> None:
        self.__clear()
        deck = input('Name the deck which should be removed: ')
        if self.__config.delete_deck(deck):
            print(deck + ' was removed from the desklist.')
            sleep(5)
        else:
            print(f'Error: A deck with the name [{deck}] was not found!')

    def __display_decks(self) -> None:
        print('Loaded decks:\n')
        for name, url in self.__config.decklist.items():
            print(name)
