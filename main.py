from menu import Menu
from config import Settings


def run() -> None:
    config = Settings()
    menu = Menu(config)
    menu.display_menu()


if __name__ == "__main__":
    run()
