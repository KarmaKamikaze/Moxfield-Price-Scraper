import sys
import time
import logging
from os.path import exists
from os import makedirs
from datetime import datetime
from selenium import webdriver
from email_notification import send_mail
from config import Settings


def set_logging(name: str) -> logging.Logger:
    logger = logging.getLogger(name)
    logger.setLevel(logging.INFO)
    if not exists('./Data/Logs/'):
        try:
            makedirs('./Data/Logs/')
        except OSError:
            print('Log folder cannot be created. Insufficient privileges.')
            exit()
    file_handler = logging.FileHandler(f'./Data/Logs/{name}.log')
    file_handler.setLevel(logging.INFO)
    logger.addHandler(file_handler)
    return logger


class Scraper:
    def __init__(self, name: str, url: str, config: Settings):
        self.deck_author = None
        self.deck_title = None
        self.ui_name = name
        self.url = url
        self.logger = set_logging(name)
        self.__config = config
        self.__element_seek_timeout = 20

        # configure webdriver
        options = webdriver.ChromeOptions()
        options.headless = True  # hide GUI
        options.add_argument("--window-size=2560,1440")  # set window size to native GUI size
        options.add_argument("start-maximized")  # ensure window is full-screen
        options.add_argument("--log-level=3")
        # configure Chrome browser to not load images and javascript
        options.add_experimental_option(
            # this will disable image loading
            "prefs",
            {"profile.managed_default_content_settings.images": 2},
        )
        options.add_experimental_option(
            # this will disable DevTools logging
            "excludeSwitches",
            ["enable-logging"],
        )
        self.__driver = webdriver.Chrome(
            self.__config.webdriver_path,
            options=options,
        )
        # Set the implicit wait time
        self.__driver.implicitly_wait(self.__element_seek_timeout)

    def __log(self, message: str) -> None:
        self.logger.info(message)
        if self.__config.tail_logs:
            print(message)

    def quit(self) -> None:
        self.__driver.quit()

    def __login(self, username: str, password: str) -> None:
        self.deck_title = self.__driver.find_element_by_css_selector("#menu-deckname > span").text
        self.deck_author = self.__driver.find_element_by_css_selector("#userhover-popup-2 > a").text
        self.__log(f"Deck: {self.deck_title} by {self.deck_author}.")

        if username == "" or password == "":
            self.__log("Please add username and password information to the .env file. Exiting...")
            exit()

        self.__log("Logging in...")
        login_box = self.__driver.find_element_by_css_selector(
            "#js-reactroot > header > nav > div > div > ul.navbar-nav.me-0 > li:nth-child(1) > a"
        )
        login_box.click()

        username_box = self.__driver.find_element_by_css_selector("#username")
        username_box.send_keys(username)

        password_box = self.__driver.find_element_by_css_selector("#password")
        password_box.send_keys(password)

        sign_in_box = self.__driver.find_element_by_css_selector(
            "#maincontent > div > div.col-lg-8 > div > div.card.border-0.col-sm-9.col-lg-7 > div > form > "
            "div:nth-child(3) > button "
        )
        sign_in_box.click()

    def __set_price_to_lowest(self) -> None:
        more_box = self.__driver.find_element_by_css_selector("#subheader-more > span")
        more_box.click()

        set_to_lowest_box = self.__driver.find_element_by_css_selector(
            "body > div.dropdown-menu.show > div > div > div.d-inline-block.dropdown-column-divider > "
            "a:nth-child(7) "
        )
        set_to_lowest_box.click()

        confirm_box = self.__driver.find_element_by_css_selector(
            "body > div.modal.zoom.show.d-block.text-start > div > div > div.modal-footer > "
            "button.btn.xfXbvFpydldcPS0H45tv.btn-primary > span.YdEWqn292WqT4MUY5cvf"
        )
        confirm_box.click()
        time.sleep(2)  # wait for prompt to disappear

    def __get_price_field(self) -> str:
        return self.__driver.find_element_by_css_selector("#shoppingcart").text

    def __check_currency(self):
        price = self.__get_price_field()
        self.__log("Checking currency settings...")
        if not price.__contains__("€"):
            self.__log("Currency is not set to euros (€). Changing Currency...")

            change_currency_settings_box = self.__driver.find_element_by_css_selector(
                "#maincontent > div.container.mt-3.mb-5 > div.deckview > div.d-none.d-md-block.pe-4 > div > "
                "div.d-grid.gap-2.mt-4.mx-auto > div > a "
            )
            change_currency_settings_box.click()

            euros_box = self.__driver.find_element_by_css_selector("#playStyle-paperEuros")
            euros_box.click()

            cardmarket_box = self.__driver.find_element_by_css_selector("#affiliate-cardmarket")
            cardmarket_box.click()

            save_box = self.__driver.find_element_by_css_selector(
                "#maincontent > div.container.my-5 > div.row > div.col-lg-8.pe-lg-5.order-2.order-lg-1 > form > "
                "div:nth-child(4) > button "
            )
            save_box.click()
            time.sleep(2)  # wait for settings to save

        self.__log("Currency is set to euros (€).")

    def __check_price(self, price_target: float, update_frequency: int) -> float:
        self.__log("Beginning price-checking phase...")
        price = float(self.__get_price_field().replace("€", "").strip(" ").split("(", 1)[0])
        self.__log("Setting price to lowest...")
        while price > price_target:
            self.__driver.refresh()
            self.__set_price_to_lowest()
            new_price = float(self.__get_price_field().replace("€", "").strip(" ").split("(", 1)[0])
            self.__log(
                f"{datetime.now().strftime('%d-%m-%Y %H:%M:%S')}"
                + f"\tPrice Before: [€{price}]\tNow: [€{new_price}]"
            )
            price = new_price
            if price <= price_target:
                continue
            else:
                time.sleep(update_frequency)

        self.__log(f"Optimal price found: €{price}! Terminating program...")
        return price

    def __save_proof(self, path: str = "proof.png") -> None:
        original_size = self.__driver.get_window_size()
        required_width = self.__driver.execute_script(
            "return document.body.parentNode.scrollWidth"
        )
        required_height = self.__driver.execute_script(
            "return document.body.parentNode.scrollHeight"
        )
        self.__driver.set_window_size(required_width, required_height)
        # driver.save_screenshot(path)  # has scrollbar
        self.__driver.find_element_by_tag_name("body").screenshot(path)  # avoids scrollbar
        self.__driver.set_window_size(original_size["width"], original_size["height"])

    def scrape_price(self) -> None:
        try:
            self.__driver.get(self.url)
            self.__login(self.__config.moxfield_username, self.__config.moxfield_password)
            self.__check_currency()
            self.__driver.get(self.url)
            final_price = self.__check_price(self.__config.price_target, self.__config.update_frequency)
            self.__save_proof(f"{self.deck_title}_proof.png")
            if self.__config.send_mails:
                send_mail(self.__config.sender_email, self.__config.email_password, self.__config.receiver_email,
                          self.deck_title, final_price)
        finally:
            self.__driver.quit()


if __name__ == "__main__":
    Scraper(sys.argv[1], sys.argv[2], Settings()).scrape_price()
