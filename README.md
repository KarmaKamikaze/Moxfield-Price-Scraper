# Moxfield Price Scraper

This script will continuously scrape the price of a given Moxfield deck until it reaches a certain specified threshold. It will set the price of all cards within the deck list to the cheapest market value to ensure that prices are as low as possible. Once the threshold is reached, the scraping will cease and a screenshot will be taken and saved. If E-mail notifications have been enabled, an e-mail will also be sent to the user, containing the screenshot. The script is great if your playgroup likes to play events where a deck must be within a certain price range. My playgroup uses it for a custom format that we call _Shitlander_, a €20 budget Commander (EDH) format, where all participants must provide a screenshot of their deck within this price range.

## Prerequisites
The script uses [Python 3.10.10](https://www.python.org/downloads/release/python-31010/) and `Selenium 3.141.0`; the latter can be installed with pip using the following command:
```
python pip -m install selenium==3.141.0
```
or simply by running the following command `(Recommended)`: 
```
pip install -r requirements.txt
```


Additionally, [ChromeDriver](https://chromedriver.chromium.org/) and [Google Chrome](https://www.google.com/chrome/index.html) must be installed and the absolute path to the ChromeDriver executable must be manually inserted into the `.env` file. See below.

## Setting up
User-specific settings must be set before running the program.
For this purpose, create a new `settings.json` file in the root directory and fill in the details.

The following options are available and must be filled out:
```
{
	"PriceTarget": 20.00,
	"UpdateFrequency": 300,
	"MoxfieldUsername": "[USERNAME]",
	"MoxfieldPassword": "[PASSWORD]",
	"WebdriverPath": "[ABSOLUTE PATH TO CHROMEDRIVER EXECUTABLE]",
	"SendEmails": true,
	"SenderEmailAddress": "[E-MAIL #1]",
	"SenderEmailPassword": "[E-MAIL #1 PASSWORD]",
	"ReceiverEmailAddress": "[E-MAIL #2]",
	"TailLogs": false
}
```

## Running the script
The script can be run with the following command:
```
py -3.10 ./main
```
The `[DECK URL]` is the Moxfield link to the deck that will be price-monitored.
