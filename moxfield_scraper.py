import time
from datetime import datetime
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from email_notification import send_mail
from config import Settings


class Scraper:
    def __init__(self):
        self.deck_author = None
        self.deck_title = None
        self.__config = Settings()

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
            chrome_options=options,
        )

    def __login(self, username: str, password: str) -> None:
        WebDriverWait(driver=self.__driver, timeout=5).until(
            EC.presence_of_element_located(
                (
                    By.CSS_SELECTOR,
                    "#menu-deckname > span",
                )
            )
        )
        self.deck_title = self.__driver.find_element_by_css_selector("#menu-deckname > span").text
        self.deck_author = self.__driver.find_element_by_css_selector(
            "#maincontent > div.deckheader-wrapper > div.deckheader > div.deckheader-content > div > div.mb-3 > "
            "div > div.flex-grow-1 > div > a:nth-child(1) "
        ).text
        print(f"Deck: {self.deck_title} by {self.deck_author}.")

        if username == "" or password == "":
            print("Please add username and password information to the .env file. Exiting...")
            exit()
        print("Logging in...")
        # wait for page to load
        WebDriverWait(driver=self.__driver, timeout=5).until(
            EC.presence_of_element_located(
                (
                    By.CSS_SELECTOR,
                    "#js-reactroot > header > nav > div > div > ul.navbar-nav.me-0 > li:nth-child(1) > a",
                )
            )
        )
        login_box = self.__driver.find_element_by_css_selector(
            "#js-reactroot > header > nav > div > div > ul.navbar-nav.me-0 > li:nth-child(1) > a"
        )
        login_box.click()
        WebDriverWait(driver=self.__driver, timeout=5).until(
            EC.presence_of_element_located((By.CSS_SELECTOR, "#username"))
        )
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
        # wait for page to load
        WebDriverWait(driver=self.__driver, timeout=5).until(
            EC.presence_of_element_located((By.CSS_SELECTOR, "#subheader-more > span"))
        )
        more_box = self.__driver.find_element_by_css_selector("#subheader-more > span")
        more_box.click()
        WebDriverWait(driver=self.__driver, timeout=5).until(
            EC.presence_of_element_located(
                (
                    By.CSS_SELECTOR,
                    "body > div.dropdown-menu.show > div > div > div.d-inline-block.dropdown-column-divider > "
                    "a:nth-child(6)",
                )
            )
        )
        set_to_lowest_box = self.__driver.find_element_by_css_selector(
            "body > div.dropdown-menu.show > div > div > div.d-inline-block.dropdown-column-divider > "
            "a:nth-child(6) "
        )
        set_to_lowest_box.click()
        WebDriverWait(driver=self.__driver, timeout=5).until(
            EC.presence_of_element_located(
                (
                    By.CSS_SELECTOR,
                    "body > div.modal.zoom.show.d-block.text-start > div > div > div.modal-footer > "
                    "button.btn.btn-custom.btn-primary",
                )
            )
        )
        confirm_box = self.__driver.find_element_by_css_selector(
            "body > div.modal.zoom.show.d-block.text-start > div > div > div.modal-footer > "
            "button.btn.btn-custom.btn-primary "
        )
        confirm_box.click()
        time.sleep(2)  # wait for prompt to disappear

    def __get_price_field(self) -> str:
        WebDriverWait(driver=self.__driver, timeout=5).until(
            EC.presence_of_element_located(
                (
                    By.CSS_SELECTOR,
                    "#shoppingcart",
                )
            )
        )
        return self.__driver.find_element_by_css_selector("#shoppingcart").text

    def __check_currency(self):
        price = self.__get_price_field()
        print("Checking currency settings...")
        if not price.__contains__("€"):
            print("Currency is not set to euros (€). Changing Currency...")

            WebDriverWait(driver=self.__driver, timeout=5).until(
                EC.presence_of_element_located(
                    (
                        By.CSS_SELECTOR,
                        "#maincontent > div.container.mt-3.mb-5 > div.deckview > div.d-none.d-md-block.pe-4 > div "
                        "> div.d-grid.gap-2.mt-4.mx-auto > div > a",
                    )
                )
            )
            change_currency_settings_box = self.__driver.find_element_by_css_selector(
                "#maincontent > div.container.mt-3.mb-5 > div.deckview > div.d-none.d-md-block.pe-4 > div > "
                "div.d-grid.gap-2.mt-4.mx-auto > div > a "
            )
            change_currency_settings_box.click()
            WebDriverWait(driver=self.__driver, timeout=5).until(
                EC.presence_of_element_located(
                    (
                        By.CSS_SELECTOR,
                        "#playStyle-paperEuros",
                    )
                )
            )
            euros_box = self.__driver.find_element_by_css_selector("#playStyle-paperEuros")
            euros_box.click()
            WebDriverWait(driver=self.__driver, timeout=5).until(
                EC.presence_of_element_located(
                    (
                        By.CSS_SELECTOR,
                        "#affiliate-cardmarket",
                    )
                )
            )
            cardmarket_box = self.__driver.find_element_by_css_selector("#affiliate-cardmarket")
            cardmarket_box.click()
            WebDriverWait(driver=self.__driver, timeout=5).until(
                EC.presence_of_element_located(
                    (
                        By.CSS_SELECTOR,
                        "#maincontent > div.container.my-5 > div.row > div.col-lg-8.pe-lg-5.order-2.order-lg-1 > "
                        "form > div:nth-child(4) > button",
                    )
                )
            )
            save_box = self.__driver.find_element_by_css_selector(
                "#maincontent > div.container.my-5 > div.row > div.col-lg-8.pe-lg-5.order-2.order-lg-1 > form > "
                "div:nth-child(4) > button "
            )
            save_box.click()
            time.sleep(2)  # wait for settings to save

        print("Currency is set to euros (€).")

    def __check_price(self, price_target: float, update_frequency: int) -> float:
        print("Beginning price-checking phase...")
        price = float(self.__get_price_field().replace("€", "").strip(" ").split("(", 1)[0])
        print("Setting price to lowest...")
        while price > price_target:
            self.__driver.refresh()
            self.__set_price_to_lowest()
            new_price = float(self.__get_price_field().replace("€", "").strip(" ").split("(", 1)[0])
            print(
                f"{datetime.now().strftime('%d-%m-%Y %H:%M:%S')}"
                + f"\tPrice Before: [€{price}]\tNow: [€{new_price}]"
            )
            price = new_price
            if price <= price_target:
                continue
            else:
                time.sleep(update_frequency)

        print(f"Optimal price found: €{price}! Terminating program...")
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

    def scrape_price(self, url: str) -> None:
        try:
            self.__driver.get(url)
            self.__login(self.__config.moxfield_username, self.__config.moxfield_password)
            self.__check_currency()
            self.__driver.get(url)
            final_price = self.__check_price(self.__config.price_target, self.__config.update_frequency)
            self.__save_proof(f"{self.deck_title}_proof.png")
            if self.__config.send_mails:
                send_mail(self.__config.sender_email, self.__config.email_password, self.__config.receiver_email,
                          self.deck_title, final_price)
        finally:
            self.__driver.quit()
