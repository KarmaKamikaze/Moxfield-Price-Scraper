using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;
using System.Drawing;

namespace MoxfieldPriceScraper;

public class MoxfieldScraper : IMoxfieldScraper
{
    private readonly ISettings _settings;
    private readonly string _deckUrl;
    private string _deckAuthor = string.Empty;
    private string _deckTitle = string.Empty;
    private readonly TimeSpan _elementSeekTimeout = TimeSpan.FromSeconds(20);
    private readonly ChromeDriver _driver;
    private readonly string _chromeDriverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chromedriver.exe");
    private bool _disposed;

    public MoxfieldScraper(string deckUrl, ISettings settings)
    {
        _deckUrl = deckUrl;
        _settings = settings;
        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--headless");
        chromeOptions.AddArgument("--window-size=2560,1440");
        chromeOptions.AddArgument("--log-level=3");
        chromeOptions.AddExcludedArguments("enable-logging");
        var preferences = new Dictionary<string, object>
        {
            { "profile.managed_default_content_settings.images", 2 } // Disable image loading
        };
        chromeOptions.AddUserProfilePreference("prefs", preferences);
        if (File.Exists(_chromeDriverPath))
        {
            // Use local chromedriver.exe
            _driver = new ChromeDriver(_chromeDriverPath, chromeOptions);
            Log.Debug("Using local chromedriver.exe");
        }
        else
        {
            // Fallback to NuGet package version
            _driver = new ChromeDriver(chromeOptions);
            Log.Debug("Using NuGet package chromedriver.exe");
        }

        _driver.Manage().Timeouts().ImplicitWait = _elementSeekTimeout;
        Log.Debug("WebDriver initialized with ImplicitWait set to {Timeout}", _elementSeekTimeout);
    }

    /// <inheritdoc/>
    public async Task ScrapeAsync(CancellationToken cancellationToken)
    {
        await _driver.Navigate().GoToUrlAsync(_deckUrl);
        Log.Debug("Navigated to [{DeckUrl}]", _deckUrl);
        if (!string.IsNullOrEmpty(_settings.MoxfieldUsername) && !string.IsNullOrEmpty(_settings.MoxfieldPassword))
        {
            LoginToMoxfieldAsync(_settings.MoxfieldUsername, _settings.MoxfieldPassword);
        }
        else
        {
            Log.Warning("Moxfield credentials settings are not fully configured");
        }

        // Check for cancellation after login
        cancellationToken.ThrowIfCancellationRequested();

        CheckCurrency();
        await _driver.Navigate().GoToUrlAsync(_deckUrl);
        Log.Debug("Return to [{DeckUrl}] after login", _deckUrl);
        var finalPrice = await GetPrice(_settings.TargetPrice, _settings.UpdateFrequency, cancellationToken);

        // Create data directory if it doesn't exist
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        Directory.CreateDirectory(dataDirectory);
        SaveImageProof(Path.Combine(dataDirectory, $"{_deckTitle}_proof.png"));

        // Check for cancellation before sending email
        cancellationToken.ThrowIfCancellationRequested();
        if (_settings.SendEmailNotification)
        {
            if (!string.IsNullOrEmpty(_settings.SenderEmailAddress) &&
                !string.IsNullOrEmpty(_settings.SenderEmailPassword) &&
                !string.IsNullOrEmpty(_settings.ReceiverEmailAddress))
            {
                await EmailService.SendEmailWithEmbeddedImageAsync(_settings.SenderEmailAddress, _settings.SenderEmailPassword,
                    _settings.ReceiverEmailAddress, $"Moxfield Scraper Success on {_deckTitle}!",
                    $"Optimal price found for {_deckTitle}: €{finalPrice}! See attachment proof...",
                    Path.Combine(dataDirectory, $"{_deckTitle}_proof.png"));
            }
            else
            {
                Log.Warning("Email notification settings are not fully configured");
            }
        }
    }

    /// <summary>
    /// Disposes of the WebDriver instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the WebDriver instance.
    /// </summary>
    /// <param name="disposing">Determines if the function is called from Dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _driver.Quit();
                _driver.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Logs in to Moxfield with the provided credentials.
    /// </summary>
    /// <param name="username">Moxfield username for login.</param>
    /// <param name="password">Moxfield password for login.</param>
    /// <exception cref="ArgumentException">No credentials were given.</exception>
    private void LoginToMoxfieldAsync(string username, string password)
    {
        _deckTitle = _driver.FindElement(By.CssSelector("#menu-deckname > span")).Text;
        _deckAuthor = _driver.FindElement(By.CssSelector("#userhover-popup-2 > a")).Text;
        Log.Information("Deck [{DeckTitle}] by [{DeckAuthor}] loaded", _deckTitle, _deckAuthor);

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Log.Fatal("No Moxfield credentials provided");
            throw new ArgumentException("No Moxfield credentials provided");
        }

        Log.Debug("Initiating login to Moxfield");
        var loginBox =
            _driver.FindElement(
                By.CssSelector("#js-reactroot > header > nav > div > div > ul.navbar-nav.me-0 > li:nth-child(1) > a"));
        loginBox.Click();
        Log.Debug("Clicked on login box");

        var usernameField = _driver.FindElement(By.CssSelector("#username"));
        usernameField.SendKeys(username);
        Log.Debug("Entered username");

        var passwordField = _driver.FindElement(By.CssSelector("#password"));
        passwordField.SendKeys(password);
        Log.Debug("Entered password");

        var signInBox = _driver.FindElement(By.CssSelector(
            "#maincontent > div > div.col-lg-8 > div > div.card.border-0.col-sm-9.col-lg-7 > " +
            "div > form > div:nth-child(3) > button "));
        signInBox.Click();
        Log.Debug("Successfully logged in to Moxfield");
    }

    /// <summary>
    /// Sets the price of the deck to the lowest possible value.
    /// </summary>
    private void SetPriceToLowest()
    {
        Log.Debug("Setting price to lowest");
        var moreBox = _driver.FindElement(By.CssSelector("#subheader-more > span"));
        moreBox.Click();
        Log.Debug("Clicked on more box");

        var setLowestBox = _driver.FindElement(By.CssSelector(
            "body > div.dropdown-menu.show > div > div > div.d-inline-block.dropdown-column-divider > " +
            "a:nth-child(7) "));
        setLowestBox.Click();
        Log.Debug("Clicked on set lowest box");

        var confirmBox = _driver.FindElement(By.CssSelector(
            "body > div.modal.zoom.show.d-block.text-start > div > div > div.modal-footer > " +
            "button.btn.xfXbvFpydldcPS0H45tv.btn-primary > span.YdEWqn292WqT4MUY5cvf"));
        confirmBox.Click();
        Log.Debug("Clicked on confirm box");

        var waitTimeInSeconds = 3;
        Thread.Sleep(TimeSpan.FromSeconds(waitTimeInSeconds));
        Log.Debug("Allowing {TimeInSeconds} seconds for settings to save", waitTimeInSeconds);
    }

    /// <summary>
    /// Grabs the price of the deck from Moxfield.
    /// </summary>
    /// <returns>The Moxfield deck price.</returns>
    private string GetPriceField()
    {
        return _driver.FindElement(By.CssSelector("#shoppingcart")).Text;
    }

    /// <summary>
    /// Checks if the currency is set to Euro (€) on Moxfield. If it is not, it changes it.
    /// </summary>
    private void CheckCurrency()
    {
        var price = GetPriceField();
        Log.Debug("Checking currency settings");
        if (!price.Contains("€"))
        {
            Log.Debug("Currency is not set to Euro (€)");
            var changeCurrencySettingsBox = _driver.FindElement(By.CssSelector(
                "#maincontent > div.container.mt-3.mb-5 > div.deckview > div.d-none.d-md-block.pe-4 > " +
                "div > div.d-grid.gap-2.mt-4.mx-auto > div > a "));
            changeCurrencySettingsBox.Click();
            Log.Debug("Clicked on currency settings box");

            var euroBox = _driver.FindElement(By.CssSelector("#playStyle-paperEuros"));
            euroBox.Click();
            Log.Debug("Clicked on Euro (€) currency box");

            var cardmarketBox = _driver.FindElement(By.CssSelector("#affiliate-cardmarket"));
            cardmarketBox.Click();
            Log.Debug("Clicked on Cardmarket affiliate box");

            var saveSettingsBox = _driver.FindElement(By.CssSelector(
                "#maincontent > div.container.my-5 > div.row > div.col-lg-8.pe-lg-5.order-2.order-lg-1 > " +
                "form > div:nth-child(4) > button "));
            saveSettingsBox.Click();
            Log.Debug("Clicked on save settings box");

            var waitTimeInSeconds = 3;
            Thread.Sleep(TimeSpan.FromSeconds(waitTimeInSeconds));
            Log.Debug("Allowing {TimeInSeconds} seconds for settings to save", waitTimeInSeconds);
        }

        Log.Debug("Currency is set to Euro (€)");
    }

    /// <summary>
    /// Gets the price for the deck when it falls under the target price given.
    /// </summary>
    /// <param name="targetPrice">The target price which we aim to fall under.</param>
    /// <param name="updateFrequency">How often we check for a price update in seconds.</param>
    /// <param name="cancellationToken">A cancellation token that monitors if the scraper should end prematurely.</param>
    /// <returns>The price when it is found to be lower than the target price.</returns>
    private async Task<decimal> GetPrice(decimal targetPrice, int updateFrequency, CancellationToken cancellationToken)
    {
        Log.Debug("Beginning price-checking process");
        var price = decimal.Parse(GetPriceField().Replace("€", string.Empty).Trim().Split('(')[0],
            CultureInfo.InvariantCulture);

        while (price > targetPrice)
        {
            // Check for cancellation after before getting new price
            cancellationToken.ThrowIfCancellationRequested();

            await _driver.Navigate().RefreshAsync();
            SetPriceToLowest();
            var newPrice = decimal.Parse(GetPriceField().Replace("€", string.Empty).Trim().Split('(')[0],
                CultureInfo.InvariantCulture);
            Log.Information("{Time}\tPrice Before: [€{BeforePrice}]\tNow: [€{NewPrice}]",
                DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), price, newPrice);
            price = newPrice;
            if (price > targetPrice)
            {
                Thread.Sleep(TimeSpan.FromSeconds(updateFrequency));
            }
        }

        Log.Information("Optimal price found: €{Price}!", price);
        return price;
    }

    /// <summary>
    /// Saves a screenshot of the deck page as proof of the optimal price found.
    /// </summary>
    /// <param name="imagePath">The path to the screenshot image.</param>
    private void SaveImageProof(string imagePath = "proof.jpeg")
    {
        Log.Debug("Saving image proof");
        var originalSize = _driver.Manage().Window.Size;
        Log.Debug("Original window size is [{Width},{Height}]", originalSize.Width, originalSize.Height);
        var requiredWidth =
            Convert.ToInt32((long) _driver.ExecuteScript("return document.body.parentNode.scrollWidth"));
        var requiredHeight =
            Convert.ToInt32((long) _driver.ExecuteScript("return document.body.parentNode.scrollHeight"));
        _driver.Manage().Window.Size = new Size(requiredWidth, requiredHeight);
        Log.Debug("Window size set to full page screenshot size [{Width},{Height}]", requiredWidth,
            requiredHeight);

        //_driver.GetScreenshot().SaveAsFile(imagePath); // has scrollbar
        ((ITakesScreenshot) _driver.FindElement(By.TagName("body"))).GetScreenshot()
            .SaveAsFile(imagePath); // avoids scrollbar
        Log.Debug("Screenshot saved as [{ImagePath}]", imagePath);

        _driver.Manage().Window.Size = originalSize;
        Log.Debug("Restored original window size");
    }
}
