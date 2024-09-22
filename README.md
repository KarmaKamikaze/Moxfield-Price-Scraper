# Moxfield Price Scraper
The **Moxfield Price Scraper** is an application designed to continuously monitor the price of specified Moxfield decks until a defined threshold is reached. It automatically adjusts the prices of all cards within the deck list to reflect the lowest market values, ensuring users can acquire their desired decks at the best possible prices. Once the threshold is met, the application captures a screenshot of the deck and saves it. If email notifications are enabled, a notification email will also be sent to the user with the attached screenshot. This tool is particularly useful for playgroups that host events requiring decks to remain within a specific price range. My playgroup utilizes it for a custom format known as _Shitlander_, a €25 budget Commander (EDH) format, where all participants must provide a screenshot of their deck within this price limit.

## Prerequisites
To run the application, ensure you have the following installed:
- [.Net 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Selenium](https://www.selenium.dev/documentation/) (installed via NuGet package)
- [Google Chrome](https://www.google.com/chrome/index.html)
- [ChromeDriver](https://chromedriver.chromium.org/) (ensure the version matches your Chrome installation)

**Note:** The ChromeDriver executable must be manually placed in the root directory of the project.

## Setting up
Before running the application in development mode, user-specific settings must be configured. This can be done by creating and editing a `.env` file in the root directory. The file should contain the following options:

```dockerfile
TARGET_PRICE=25.00
UPDATE_FREQUENCY=300
MOXFIELD_USERNAME='moxfieldUsername'
MOXFIELD_PASSWORD='moxfieldPassword'
SEND_EMAIL_NOTIFICATION=true
SENDER_EMAIL_ADDRESS='senderemail@example.com'
SENDER_EMAIL_PASSWORD='senderemailpassword'
RECEIVER_EMAIL_ADDRESS='receiveremail@example.com'
DECK_LIST='{"Deck1":"https://moxfield.com/deck1", "Deck2":"https://moxfield.com/deck2"}'
```
### Configuration options
- **TargetPrice**: The price threshold that the deck must reach before the scraping stops.
- **UpdateFrequency**: The frequency at which the price is checked (in seconds).
- **MoxfieldUsername**: Your Moxfield account username.
- **MoxfieldPassword**: Your Moxfield account password.
- **SendEmailNotification**: Whether to send an e-mail notification when the threshold is reached.
- **SenderEmailAddress**: Email address used to send notifications.
- **SenderEmailPassword**: Password for the sender email account.
- **ReceiverEmailAddress**: Email address to receive notifications.
- **DeckList**:  A dictionary of deck names and their corresponding Moxfield URLs. There should be at least one deck in the list, however, multiple decks can be added.

## Running the Application
The application can be executed using the following command:
```shell
dotnet run
```
Alternatively, you can publish the application and run the resulting executable.

## Contributing
Contributions are welcome! If you would like to contribute to this project, please fork the repository and submit a pull request.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
