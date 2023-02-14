"""
    File name: moxfield_scraper.py
    Description: Moxfield Deck Price Scraper
    Author: Nicolai Hejlesen Jørgensen
    Date created: 03.12.2022
    Date last modified: 14.02.2023
    Python version: 3.9.13
    Requirements: Selenium 3.141.0
    Install requirements with: python pip -m install selenium==3.141.0
"""
import sys
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import imghdr, smtplib, ssl
from email.message import EmailMessage
from datetime import datetime
import time

# User specific settings
price_target = 20.00
update_frequency = 300  # In seconds
username = ""
password = ''
webdriver_path = "C:/ChromeDriver/chromedriver.exe"
# Email settings
send_mails = True
sender_email = ""
receiver_email = ""
email_password = ""

# configure webdriver
options = webdriver.ChromeOptions()
options.headless = True  # hide GUI
options.add_argument("--window-size=2560,1440")  # set window size to native GUI size
options.add_argument("start-maximized")  # ensure window is full-screen
options.add_argument("--log-level=3")
# configure chrome browser to not load images and javascript
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
driver = webdriver.Chrome(
    webdriver_path,
    chrome_options=options,
)

# global information
deck_title = ""
deck_author = ""


def check_url(args):
    if len(args) < 2:
        print("Usage: ./moxfield_scraper [URL]")
        exit()
    else:
        print(f"Moxfield URL received: {args[1]}")
        return str(args[1])


def login():
    global deck_title
    global deck_author
    WebDriverWait(driver=driver, timeout=5).until(
        EC.presence_of_element_located(
            (
                By.CSS_SELECTOR,
                "#menu-deckname > span",
            )
        )
    )
    deck_title = driver.find_element_by_css_selector("#menu-deckname > span").text
    deck_author = driver.find_element_by_css_selector(
        "#maincontent > div.deckheader-wrapper > div.deckheader > div.deckheader-content > div > div.mb-3 > div > div.flex-grow-1 > div > a:nth-child(1)"
    ).text
    print(f"Deck: {deck_title} by {deck_author}.")

    if username == "" or password == "":
        print("Please add username and password information to the script. Exiting...")
        exit()
    print("Logging in...")
    # wait for page to load
    WebDriverWait(driver=driver, timeout=5).until(
        EC.presence_of_element_located(
            (
                By.CSS_SELECTOR,
                "#js-reactroot > header > nav > div > div > ul.navbar-nav.me-0 > li:nth-child(1) > a",
            )
        )
    )
    login_box = driver.find_element_by_css_selector(
        "#js-reactroot > header > nav > div > div > ul.navbar-nav.me-0 > li:nth-child(1) > a"
    )
    login_box.click()
    WebDriverWait(driver=driver, timeout=5).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, "#username"))
    )
    username_box = driver.find_element_by_css_selector("#username")
    username_box.send_keys(username)
    password_box = driver.find_element_by_css_selector("#password")
    password_box.send_keys(password)
    sign_in_box = driver.find_element_by_css_selector(
        "#maincontent > div > div.col-lg-8 > div > div.card.border-0.col-sm-9.col-lg-7 > div > form > div:nth-child(3) > button"
    )
    sign_in_box.click()


def set_price_to_lowest():
    # wait for page to load
    WebDriverWait(driver=driver, timeout=5).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, "#subheader-more > span"))
    )
    more_box = driver.find_element_by_css_selector("#subheader-more > span")
    more_box.click()
    WebDriverWait(driver=driver, timeout=5).until(
        EC.presence_of_element_located(
            (
                By.CSS_SELECTOR,
                "body > div.dropdown-menu.show > div > div > div.d-inline-block.dropdown-column-divider > a:nth-child(6)",
            )
        )
    )
    set_to_lowest_box = driver.find_element_by_css_selector(
        "body > div.dropdown-menu.show > div > div > div.d-inline-block.dropdown-column-divider > a:nth-child(6)"
    )
    set_to_lowest_box.click()
    WebDriverWait(driver=driver, timeout=5).until(
        EC.presence_of_element_located(
            (
                By.CSS_SELECTOR,
                "body > div.modal.zoom.show.d-block.text-start > div > div > div.modal-footer > button.btn.btn-custom.btn-primary",
            )
        )
    )
    confirm_box = driver.find_element_by_css_selector(
        "body > div.modal.zoom.show.d-block.text-start > div > div > div.modal-footer > button.btn.btn-custom.btn-primary"
    )
    confirm_box.click()
    time.sleep(2)  # wait for prompt to disappear


def get_price():
    WebDriverWait(driver=driver, timeout=5).until(
        EC.presence_of_element_located(
            (
                By.CSS_SELECTOR,
                "#shoppingcart",
            )
        )
    )
    return driver.find_element_by_css_selector("#shoppingcart").text


def check_currency():
    price = get_price()
    print("Checking currency settings...")
    if not price.__contains__("€"):
        print("Currency is not set to euros (€). Changing Currency...")

        WebDriverWait(driver=driver, timeout=5).until(
            EC.presence_of_element_located(
                (
                    By.CSS_SELECTOR,
                    "#maincontent > div.container.mt-3.mb-5 > div.deckview > div.d-none.d-md-block.pe-4 > div > div.d-grid.gap-2.mt-4.mx-auto > div > a",
                )
            )
        )
        change_currency_settings_box = driver.find_element_by_css_selector(
            "#maincontent > div.container.mt-3.mb-5 > div.deckview > div.d-none.d-md-block.pe-4 > div > div.d-grid.gap-2.mt-4.mx-auto > div > a"
        )
        change_currency_settings_box.click()
        WebDriverWait(driver=driver, timeout=5).until(
            EC.presence_of_element_located(
                (
                    By.CSS_SELECTOR,
                    "#playStyle-paperEuros",
                )
            )
        )
        euros_box = driver.find_element_by_css_selector("#playStyle-paperEuros")
        euros_box.click()
        WebDriverWait(driver=driver, timeout=5).until(
            EC.presence_of_element_located(
                (
                    By.CSS_SELECTOR,
                    "#affiliate-cardmarket",
                )
            )
        )
        cardmarket_box = driver.find_element_by_css_selector("#affiliate-cardmarket")
        cardmarket_box.click()
        WebDriverWait(driver=driver, timeout=5).until(
            EC.presence_of_element_located(
                (
                    By.CSS_SELECTOR,
                    "#maincontent > div.container.my-5 > div.row > div.col-lg-8.pe-lg-5.order-2.order-lg-1 > form > div:nth-child(4) > button",
                )
            )
        )
        save_box = driver.find_element_by_css_selector(
            "#maincontent > div.container.my-5 > div.row > div.col-lg-8.pe-lg-5.order-2.order-lg-1 > form > div:nth-child(4) > button"
        )
        save_box.click()
        time.sleep(2)  # wait for settings to save

    print("Currency is set to euros (€).")


def save_proof(path: str = "proof.png"):
    original_size = driver.get_window_size()
    required_width = driver.execute_script(
        "return document.body.parentNode.scrollWidth"
    )
    required_height = driver.execute_script(
        "return document.body.parentNode.scrollHeight"
    )
    driver.set_window_size(required_width, required_height)
    # driver.save_screenshot(path)  # has scrollbar
    driver.find_element_by_tag_name("body").screenshot(path)  # avoids scrollbar
    driver.set_window_size(original_size["width"], original_size["height"])


def check_price() -> float:
    print("Beginning price-checking phase...")
    price = float(get_price().replace("€", "").strip(" ").split("(", 1)[0])
    print("Setting price to lowest...")
    while price > price_target:
        driver.refresh()
        set_price_to_lowest()
        new_price = float(get_price().replace("€", "").strip(" ").split("(", 1)[0])
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


def scrape_price(args):
    try:
        url = check_url(args)
        driver.get(url)
        login()
        check_currency()
        driver.get(url)
        final_price = check_price()
        save_proof(f"{deck_title}_proof.png")
        if send_mails:
            send_mail(final_price)
    finally:
        driver.quit()


def send_mail(price):
    subject = f"Moxfield Scraper Success on {deck_title}!"
    body = f"Optimal price found for {deck_title}: €{price}! See attachment proof..."
    # Create an email message and set headers
    message = EmailMessage()
    message["From"] = sender_email
    message["To"] = receiver_email
    message["Subject"] = subject
    message["Bcc"] = receiver_email  # Recommended for mass emails

    # Add body to email
    message.set_content(body)

    attachment = f"{deck_title}_proof.png"  # In same directory as script

    # Open image file in binary mode
    with open(attachment, "rb") as a:
        image_data = a.read()
        image_type = imghdr.what(a.name)
        image_name = a.name

    message.add_attachment(
        image_data, maintype="image", subtype=image_type, filename=image_name
    )

    # Log in to server using secure context and send email
    context = ssl.create_default_context()
    with smtplib.SMTP_SSL("smtp.gmail.com", 465, context=context) as server:
        server.login(sender_email, email_password)
        server.send_message(message)


if __name__ == "__main__":
    scrape_price(sys.argv)
