namespace MoxfieldPriceScraper;

public interface IMoxfieldScraper : IDisposable
{
    /// <summary>
    /// Scrapes the Moxfield deck page for the optimal price.
    /// A proof image is sent when the optimal price as been reached. Optionally, an email notification can be sent.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that monitors if the scraper should end prematurely.</param>
    Task ScrapeAsync(CancellationToken cancellationToken);
}
