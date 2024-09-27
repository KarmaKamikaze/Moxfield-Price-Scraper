using MoxfieldPriceScraper;
using MoxfieldPriceScraper.Healthcheck;
using Serilog;

if (args.Length > 0 && args[0] == "healthcheck")
{
    if (Healthcheck.AreTasksRunning())
    {
        Environment.Exit(0); // Healthy, tasks are running
    }

    Environment.Exit(1); // Unhealthy, no tasks running
}

var debugEnabled = false;
#if DEBUG
debugEnabled = true;
#endif

Log.Logger = LoggerFactory.CreateLogger(debugEnabled);

try
{
    Log.Information("Moxfield Price Scraper initiated");
    ISettings settings = new Settings();

    if (settings.DeckList == null || settings.DeckList.Count == 0)
    {
        Log.Error("No decks found in settings file");
        await Log.CloseAndFlushAsync();
        Environment.Exit(1);
    }

    Healthcheck.InitializeStatusFile();

    // Create a CancellationTokenSource to cancel all threads on error
    var cts = new CancellationTokenSource();
    var cancellationToken = cts.Token;

    var tasks = new List<Task>();

    foreach (var deck in settings.DeckList)
    {
        var deckCopy = deck; // To avoid closure issues with async/await

        tasks.Add(Task.Run(async () =>
        {
            try
            {
                Healthcheck.UpdateTaskStatus(deckCopy.Key, "running");
                IMoxfieldScraper scraper = new MoxfieldScraper(deckCopy.Value, settings);
                await scraper.ScrapeAsync(cancellationToken);
                Healthcheck.UpdateTaskStatus(deckCopy.Key, "completed");
            }
            catch (Exception e)
            {
                Log.Error("Scraping thread for deck {Deck} failed: {Message}", deckCopy.Key, e.Message);
                Healthcheck.UpdateTaskStatus(deckCopy.Key, "failed");
                cts.Cancel(); // Cancel all threads on error
                throw;
            }
        }, cancellationToken));
    }

    // Wait for all tasks to complete, or any task to fail
    try
    {
        await Task.WhenAll(tasks);
    }
    catch (OperationCanceledException e)
    {
        Log.Warning("Operation was canceled: {Message}", e.Message);
    }
    catch (Exception e)
    {
        Log.Fatal("Unexpected error occurred: {Message}", e.Message);
    }
}
finally
{
    Log.Information("Moxfield Price Scraper terminated");
    await Log.CloseAndFlushAsync();
}
