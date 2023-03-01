import os
from os.path import exists
import subprocess
from config import Settings


def clear() -> None:
    os.system('cls' if os.name == 'nt' else 'clear')


def spin(seconds: int) -> None:
    for i in range(seconds):
        print('.', end='')


class Menu:
    def __init__(self, config: Settings):
        self.__config = config
        self.__running = False
        self.__clear_screen = True
        self.__logo = ' █▄ ▄█ ▄▀▄ ▀▄▀ █▀ █ ██▀ █   █▀▄   █▀▄ █▀▄ █ ▄▀▀ ██▀   ▄▀▀ ▄▀▀ █▀▄ ▄▀▄ █▀▄ ██▀ █▀▄\n' \
                      ' █ ▀ █ ▀▄▀ █ █ █▀ █ █▄▄ █▄▄ █▄▀   █▀  █▀▄ █ ▀▄▄ █▄▄   ▄██ ▀▄▄ █▀▄ █▀█ █▀  █▄▄ █▀▄'
        self.__options = [
            'Start scraping',
            'Tail logs',
            'Add deck',
            'Remove deck',
            'Quit'
        ]
        self.__scrapers = list()

    def display_menu(self) -> None:
        while 1:
            if self.__clear_screen:
                clear()
            self.__clear_screen = True
            has_decks = len(self.__config.decklist) > 0

            print(self.__logo + '\n\n')
            if self.__running:
                print('SCRAPER IS RUNNING\n')
            if has_decks:
                self.__display_decks()
            self.__print_options(has_decks)
            self.__redirect(has_decks)

    def __print_options(self, has_decks: bool) -> None:
        print('\nOptions:\n')
        print(
            (('1. ' + self.__options[0] + '\n') if has_decks and not self.__running else '') +

            (('2. ' + self.__options[1] + '\n') if has_decks and not self.__running else
             ('1. ' + self.__options[1] + '\n')) +

            (('3. ' + self.__options[2] + '\n') if has_decks and not self.__running else
             ('2. ' + self.__options[2] + '\n')) +

            (('4. ' + self.__options[3] + '\n') if has_decks and not self.__running else
             ('3. ' + self.__options[3] + '\n' if self.__running else '')) +

            (('5. ' + self.__options[4] + '\n') if has_decks and not self.__running else
             ('4. ' + self.__options[4] + '\n' if self.__running else '3. ' + self.__options[4] + '\n'))
        )

    def __redirect(self, has_decks: bool) -> None:
        valid_input = False
        while not valid_input:
            choice = input('\n> ')
            match choice:
                case '1':
                    valid_input = True
                    if has_decks and not self.__running:  # Start scraping
                        self.__running = True
                        self.__start_scraper_threads()
                    else:  # Tail logs
                        self.__config.tail_logs = True
                        self.__clear_screen = False
                        self.__read_logs()
                case '2':
                    valid_input = True
                    if has_decks and not self.__running:  # Tail logs
                        self.__config.tail_logs = True
                        self.__clear_screen = False
                        self.__read_logs()
                    else:  # Add deck
                        self.__new_deck()
                case '3':
                    valid_input = True
                    if has_decks and not self.__running:  # Add deck
                        self.__new_deck()
                    elif has_decks and self.__running:  # Remove deck
                        self.__remove_deck()
                    else:  # Quit
                        for proc in self.__scrapers:
                            proc.terminate()
                        exit()
                case '4':
                    valid_input = True
                    if has_decks and not self.__running:  # Remove deck
                        self.__remove_deck()
                    else:  # Quit
                        for proc in self.__scrapers:
                            proc.terminate()
                        exit()
                case '5' | 'q':
                    if has_decks and not self.__running:  # Quit
                        for proc in self.__scrapers:
                            proc.terminate()
                        exit()
                    else:
                        valid_input = False
                case _:
                    print('Error in input. Please try again.')
                    self.__redirect(has_decks)

    def __read_logs(self):
        if exists('./Data/Logs/'):
            for file in os.listdir('./Data/Logs/'):
                with open(os.path.join('./Data/Logs/', file), 'r') as log:
                    print(log.read())

    def __start_scraper_threads(self) -> None:
        for name, url in self.__config.decklist.items():
            self.__scrapers.append(subprocess.Popen(
                ['py', '-3.10', os.path.realpath('./moxfield_scraper.py'), name, url], stdout=subprocess.PIPE))

    def __new_deck(self) -> None:
        clear()
        name = input('Deck name: ')
        url = input('Deck URL: ')
        if self.__config.add_deck(name, url):
            print(name + ' added to decklist.')
            spin(3)
        else:
            print('Decklist already contain a deck with the name: ' + name)
            spin(3)

    def __remove_deck(self) -> None:
        clear()
        deck = input('Name the deck which should be removed: ')
        if self.__config.delete_deck(deck):
            print(deck + ' was removed from the desklist.')
            spin(3)
        else:
            print(f'Error: A deck with the name [{deck}] was not found!')
            spin(3)

    def __display_decks(self) -> None:
        print('Loaded decks:\n')
        for name, url in self.__config.decklist.items():
            print(name)
