using System.Drawing;
using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace MoxfieldPriceScraper;

public class MoxfieldScraper : IMoxfieldScraper
{
    private readonly string _deckUrl;
    private readonly TimeSpan _elementSeekTimeout = TimeSpan.FromMinutes(2);
    private readonly ISettings _settings;
    private string _deckAuthor = string.Empty;
    private string _deckTitle = string.Empty;
    private bool _disposed;
    private ChromeDriver? _driver;
    private DefaultWait<IWebDriver>? _fluentWait;

    public MoxfieldScraper(string deckUrl, ISettings settings)
    {
        _deckUrl = deckUrl;
        _settings = settings;
        InitializeWebDriver();
    }

    /// <inheritdoc />
    public async Task ScrapeAsync(CancellationToken cancellationToken)
    {
        await _driver!.Navigate().GoToUrlAsync(_deckUrl);
        Log.Debug("Navigated to {DeckUrl}", _deckUrl);
        if (!string.IsNullOrEmpty(_settings.MoxfieldUsername) && !string.IsNullOrEmpty(_settings.MoxfieldPassword))
        {
            LoginToMoxfieldAsync(_settings.MoxfieldUsername, _settings.MoxfieldPassword);
        }
        else
        {
            Log.Fatal("Moxfield credentials settings are not fully configured");
            throw new ArgumentException("Moxfield credentials settings are not fully configured");
        }

        // Check for cancellation after login
        cancellationToken.ThrowIfCancellationRequested();

        await CheckCurrency();
        await _driver.Navigate().GoToUrlAsync(_deckUrl);
        Log.Debug("Return to {DeckUrl} after login", _deckUrl);
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
                await EmailService.SendEmailWithEmbeddedImageAsync(_settings.SenderEmailAddress,
                    _settings.SenderEmailPassword,
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
    ///     Disposes of the WebDriver instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Disposes of the WebDriver instance.
    /// </summary>
    /// <param name="disposing">Determines if the function is called from Dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _driver!.Quit();
                _driver.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    ///     Initializes the WebDriver with the required settings.
    /// </summary>
    private void InitializeWebDriver()
    {
        Log.Debug("Initializing WebDriver");
        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--no-sandbox"); // Bypass OS security model
        chromeOptions.AddArgument("--headless=new"); // Run in headless mode, without a GUI
        chromeOptions.AddArgument("--disable-gpu"); // Disables GPU hardware acceleration
        chromeOptions.AddArgument("--disable-software-rasterizer"); // Disable software rasterizer
        chromeOptions.AddArgument("--disable-setuid-sandbox"); // Disable setuid sandbox
        chromeOptions.AddArgument("--disable-crash-reporter"); // Disable crash reporting
        chromeOptions.AddArgument("--disable-web-security"); // Disable web security
        chromeOptions.AddArgument("--disable-extensions"); // Disable extensions
        chromeOptions.AddArgument("--disable-dev-shm-usage"); // Disables the /dev/shm memory usage
        chromeOptions.AddArgument("--window-size=2560,1440"); // Set window size
        chromeOptions.AddArgument("--log-level=3"); // Disable logging
        chromeOptions.AddArgument("--disable-webgl"); // Disable WebGL
        chromeOptions.AddArgument("--disable-webrtc"); // Disable WebRTC
        chromeOptions.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                                  "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
        chromeOptions.AddExcludedArguments("enable-logging"); // Disable logging
        chromeOptions.AddExcludedArguments("enable-automation"); // Disable automation
        chromeOptions.AddAdditionalOption("useAutomationExtension", false); // Disable automation
        chromeOptions.AddArgument("--disable-blink-features=AutomationControlled"); // Disable automation
        var preferences = new Dictionary<string, object>
        {
            { "profile.managed_default_content_settings.images", 2 } // Disable image loading
        };
        chromeOptions.AddUserProfilePreference("prefs", preferences);

#if DOCKER
        chromeOptions.BinaryLocation = "/usr/bin/chromium";
        _driver = new ChromeDriver("/usr/bin/chromedriver", chromeOptions, TimeSpan.FromMinutes(5));
#else
        new DriverManager().SetUpDriver(new ChromeConfig());
        _driver = new ChromeDriver(chromeOptions);
#endif

        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(5);
        _fluentWait = new DefaultWait<IWebDriver>(_driver)
        {
            Timeout = _elementSeekTimeout,
            PollingInterval = TimeSpan.FromSeconds(2)
        };
        _fluentWait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(ElementNotVisibleException));
        Log.Debug("WebDriver initialized with waits set to {Timeout}", _elementSeekTimeout);
    }

    /// <summary>
    ///     Logs in to Moxfield with the provided credentials.
    /// </summary>
    /// <param name="username">Moxfield username for login.</param>
    /// <param name="password">Moxfield password for login.</param>
    /// <exception cref="ArgumentException">No credentials were given.</exception>
    /// <exception cref="InvalidOperationException">Moxfield login failed.</exception>
    private void LoginToMoxfieldAsync(string username, string password)
    {
        _deckTitle = _fluentWait!.Until(drv => drv.FindElement(By.CssSelector("#menu-deckname > span"))).Text;
        _deckAuthor = _fluentWait.Until(drv => drv.FindElement(By.CssSelector("#userhover-popup-2 > a"))).Text;
        Log.Information("Deck [{DeckTitle}] by [{DeckAuthor}] loaded", _deckTitle, _deckAuthor);

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Log.Fatal("No Moxfield credentials provided");
            throw new ArgumentException("No Moxfield credentials provided");
        }

        Log.Debug("Initiating login to Moxfield");
        var loginBox = _fluentWait.Until(drv =>
        {
            var element =
                drv.FindElement(By.CssSelector(
                    "#js-reactroot > header > nav > div > div > ul.navbar-nav.me-0 > li:nth-child(1) > a"));
            return element.Displayed && element.Enabled ? element : null;
        });
        loginBox?.Click();
        Log.Debug("Clicked on login box");

        AcceptCookies();

        var usernameField = _fluentWait.Until(drv =>
        {
            var element = drv.FindElement(By.CssSelector("#username"));
            return element.Displayed && element.Enabled ? element : null;
        });
        usernameField?.SendKeys(username);
        _fluentWait.Until(drv =>
            drv.FindElement(By.CssSelector("#username")).GetAttribute("value").Length == username.Length);
        Log.Debug("Entered username");

        var passwordField = _fluentWait.Until(drv =>
        {
            var element = drv.FindElement(By.CssSelector("#password"));
            return element.Displayed && element.Enabled ? element : null;
        });
        passwordField?.SendKeys(password);
        _fluentWait.Until(drv =>
            drv.FindElement(By.CssSelector("#password")).GetAttribute("value").Length == password.Length);
        Log.Debug("Entered password");

        var signInBox = _fluentWait.Until(drv =>
        {
            var element = drv.FindElement(By.CssSelector(
                "#maincontent > div > div.flex-grow-1 > div > div.card.border-0 > div > form > " +
                "div:nth-child(3) > button"));
            return element.Displayed && element.Enabled ? element : null;
        });
        signInBox?.Click();
        Log.Debug("Clicked on sign in box");

        var loginConfirmation = _fluentWait.Until(drv =>
        {
            var element = drv.FindElement(By.CssSelector("#mainmenu-user"));
            return element.Displayed && element.Enabled ? element : null;
        });
        if (loginConfirmation != null)
        {
            Log.Debug("Successfully logged in to Moxfield");
        }
        else
        {
            Log.Error("Failed to log in to Moxfield");
            throw new InvalidOperationException("Failed to log in to Moxfield");
        }
    }

    /// <summary>
    ///     Accepts the cookies on the Moxfield website.
    /// </summary>
    private void AcceptCookies()
    {
        try
        {
            var acceptCookiesButton = _fluentWait!.Until(driver =>
            {
                var button = driver.FindElement(By.CssSelector(
                    "#ncmp__tool > div > div > div.ncmp__banner-actions > div.ncmp__banner-btns > button:nth-child(2)"));
                return button.Displayed && button.Enabled ? button : null;
            });

            acceptCookiesButton?.Click();
            Log.Debug("Clicked on 'Accept Cookies' button");
        }
        catch (WebDriverTimeoutException)
        {
            Log.Debug("No 'Accept Cookies' button found, proceeding without clicking");
        }
    }

    /// <summary>
    ///     Sets the price of the deck to the lowest possible value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Failed to set price to cheapest.</exception>
    private void SetPriceToLowest()
    {
        Log.Debug("Setting price to lowest");
        var moreBox = _fluentWait!.Until(drv =>
        {
            var element = drv.FindElement(By.CssSelector("#subheader-more > span"));
            return element.Displayed && element.Enabled ? element : null;
        });
        moreBox?.Click();
        Log.Debug("Clicked on more box");

        var setLowestBox = _fluentWait.Until(drv =>
        {
            var element = drv.FindElement(By.CssSelector(
                "body > div.dropdown-menu.show > div > div > div.d-inline-block.dropdown-column-divider > " +
                "a:nth-child(7) "));
            return element.Displayed && element.Enabled ? element : null;
        });
        setLowestBox?.Click();
        Log.Debug("Clicked on set lowest box");

        var confirmBox = _fluentWait.Until(drv =>
        {
            var element = drv.FindElement(By.CssSelector(
                "body > div.modal.zoom.show.d-block.text-start > div > div > div.modal-footer > " +
                "button.btn.xfXbvFpydldcPS0H45tv.btn-primary > span.YdEWqn292WqT4MUY5cvf"));
            return element.Displayed && element.Enabled ? element : null;
        });
        confirmBox?.Click();
        Log.Debug("Clicked on confirm box");

        var setConfirmation = _fluentWait.Until(drv =>
        {
            var element = drv.FindElement(By.CssSelector("#subheader-more > span"));
            return element.Displayed && element.Enabled ? element : null;
        });
        if (setConfirmation != null)
        {
            Log.Debug("Price set to lowest");
        }
        else
        {
            Log.Error("Failed to set price to lowest");
            throw new InvalidOperationException("Failed to set price to lowest");
        }
    }

    /// <summary>
    ///     Grabs the price of the deck from Moxfield.
    /// </summary>
    /// <returns>The Moxfield deck price.</returns>
    /// <exception cref="InvalidOperationException">Failed to set price field.</exception>
    private string GetPriceField()
    {
        var priceField = _fluentWait!.Until(drv =>
        {
            var element = drv.FindElement(By.CssSelector("#shoppingcart"));
            return !string.IsNullOrEmpty(element.Text) ? element : null;
        });

        if (priceField != null) return priceField.Text;
        Log.Error("Failed to get price field");
        throw new InvalidOperationException("Failed to get price field");
    }

    /// <summary>
    ///     Checks if the currency is set to Euro (€) on Moxfield. If it is not, it changes it.
    /// </summary>
    private async Task CheckCurrency()
    {
        var price = GetPriceField();
        Log.Debug("Checking currency settings");
        if (!price.Contains('€'))
        {
            Log.Debug("Currency is not set to Euro (€)");
            await _driver!.Navigate().GoToUrlAsync("https://www.moxfield.com/account/settings/affiliates");
            Log.Debug("Navigated to affiliate settings");

            // Try to find the "up" arrow to change currency to Euro using Cardmarket
            while (true)
                try
                {
                    var upButton = _fluentWait!.Until(drv =>
                    {
                        var element = drv.FindElement(By.CssSelector("#affiliate-control-cardmarket-up"));
                        return element.Displayed && element.Enabled ? element : null;
                    });
                    if (upButton != null)
                    {
                        // Click the "up" button to move the item up
                        upButton.Click();
                        Log.Debug("Clicked on up button to change currency to Cardmarket's Euro (€)");
                        var uiUpdateDelay = TimeSpan.FromSeconds(1);
                        await Task.Delay(uiUpdateDelay);
                    }
                }
                catch (NoSuchElementException)
                {
                    // If the "up" arrow is not found, break out of the loop
                    Log.Debug("Cardmarket affiliate is listed at the top");
                    break;
                }

            var saveSettingsBox = _fluentWait!.Until(drv =>
            {
                var element = drv.FindElement(By.CssSelector(
                    "#maincontent > div > div.row > div.col-lg-8.pe-lg-5.order-2.order-lg-1 > form > " +
                    "div:nth-child(3) > button"));
                return element.Displayed && element.Enabled ? element : null;
            });
            saveSettingsBox?.Click();
            Log.Debug("Clicked on save settings box");

            var waitTimeInSeconds = 3;
            Thread.Sleep(TimeSpan.FromSeconds(waitTimeInSeconds));
            Log.Debug("Allowing {TimeInSeconds} seconds for settings to save", waitTimeInSeconds);
        }

        Log.Debug("Currency is set to Euro (€)");
    }

    /// <summary>
    ///     Gets the price for the deck when it falls under the target price given.
    /// </summary>
    /// <param name="targetPrice">The target price which we aim to fall under.</param>
    /// <param name="updateFrequency">How often we check for a price update in seconds.</param>
    /// <param name="cancellationToken">A cancellation token that monitors if the scraper should end prematurely.</param>
    /// <returns>The price when it is found to be lower than the target price.</returns>
    private async Task<decimal> GetPrice(decimal targetPrice, int updateFrequency, CancellationToken cancellationToken)
    {
        Log.Debug("Beginning price-checking process");
        var price = decimal.Parse(GetPriceField().Replace("Cardmarket€", string.Empty).Trim().Split('(')[0],
            CultureInfo.InvariantCulture);

        while (price > targetPrice)
        {
            // Check for cancellation after before getting new price
            cancellationToken.ThrowIfCancellationRequested();

            await _driver!.Navigate().RefreshAsync();

            SetPriceToLowest();

            var newPrice = decimal.Parse(GetPriceField().Replace("Cardmarket€", string.Empty).Trim().Split('(')[0],
                CultureInfo.InvariantCulture);
            Log.Information("Deck: {DeckName}\tPrice Before: [€{BeforePrice}]\tNow: [€{NewPrice}]",
                _deckTitle, price, newPrice);

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
    ///     Saves a screenshot of the deck page as proof of the optimal price found.
    /// </summary>
    /// <param name="imagePath">The path to the screenshot image.</param>
    private void SaveImageProof(string imagePath = "proof.jpeg")
    {
        Log.Debug("Saving image proof");

        var originalSize = _driver!.Manage().Window.Size;
        Log.Debug("Original window size is [{Width},{Height}]", originalSize.Width, originalSize.Height);

        var requiredWidth =
            Convert.ToInt32((long)_driver.ExecuteScript("return document.body.parentNode.scrollWidth"));
        var requiredHeight =
            Convert.ToInt32((long)_driver.ExecuteScript("return document.body.parentNode.scrollHeight"));
        _driver.Manage().Window.Size = new Size(requiredWidth, requiredHeight);
        Log.Debug("Window size set to full page screenshot size [{Width},{Height}]", requiredWidth,
            requiredHeight);

        //_driver.GetScreenshot().SaveAsFile(imagePath); // has scrollbar
        var screenshot = _fluentWait!.Until(drv =>
        {
            var bodyElement = drv.FindElement(By.TagName("body"));
            return bodyElement is ITakesScreenshot takesScreenshot ? takesScreenshot.GetScreenshot() : null;
        }) ?? throw new Exception("Failed to capture screenshot");
        screenshot.SaveAsFile(imagePath); // avoids scrollbar
        Log.Debug("Screenshot saved as [{ImagePath}]", imagePath);

        _driver.Manage().Window.Size = originalSize;
        Log.Debug("Restored original window size");
    }
}
