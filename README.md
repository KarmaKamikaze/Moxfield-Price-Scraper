# Moxfield Price Scraper

This script will continuously scrape the price of a given Moxfield deck until it reaches a certain specified threshold. It will set the price of all cards within the deck list to the cheapest market value to ensure that prices are as low as possible. The script is great if your playgroup likes to play events where a deck must be within a certain price range. My playgroup uses it for a custom format that we call _Shitlander_, a €20 budget Commander (EDH) format, where all participants must provide a screenshot of their deck within this price range.

## Prerequisites
The script uses [Python 3.9.13](https://www.python.org/downloads/release/python-3913/) and `Selenium 3.141.0`; the latter can be installed with pip using the following command:
```
python pip -m install selenium==3.141.0
```

Additionally, [ChromeDriver](https://chromedriver.chromium.org/) and [Google Chrome](https://www.google.com/chrome/index.html) must be installed and the absolute path to the ChromeDriver executable must be manually inserted into the script under `# User specific settings`. See below.

## Setting up
User-specific settings within the script must be set before running the program.
The following options are available:
```
# User specific settings
price_target = 20.00
update_frequency = 300  # In seconds
username = "[USERNAME]"
password = '[PASSWORD]'
webdriver_path = "[ABSOLUTE PATH TO CHROMEDRIVER EXECUTABLE]"
# Email settings
send_mails = True
sender_email = "[E-MAIL #1]"
receiver_email = "[E-MAIL #2]"
email_password = "[SENDER E-MAIL PASSWORD]"
```

## Running the script
The script can be run with the following command:
```
python ./moxfield_scraper [DECK URL]
```
The `[DECK URL]` is the Moxfield link to the deck that will be price-monitored.
