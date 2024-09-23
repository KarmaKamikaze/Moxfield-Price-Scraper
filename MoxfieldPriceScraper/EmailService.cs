using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Serilog;

namespace MoxfieldPriceScraper;

public static class EmailService
{
    private const string SmtpServer = "smtp.gmail.com";
    private const int SmtpPort = 587;

    /// <summary>
    /// Sends an email with an embedded image.
    /// </summary>
    /// <param name="fromEmail">The email that the message should be sent from.</param>
    /// <param name="fromPassword">The password to the email that the message should be sent from.</param>
    /// <param name="toEmail">The email that the message should be sent to.</param>
    /// <param name="subject">The subject of the email message.</param>
    /// <param name="body">The body is the email message.</param>
    /// <param name="imagePath">The path to the image that should be embedded in the email body.</param>
    /// <exception cref="Exception">The email could not be sent.</exception>
    public static async Task SendEmailWithEmbeddedImageAsync(string fromEmail, string fromPassword, string toEmail,
        string subject, string body, string imagePath)
    {
        try
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail);
            mailMessage.To.Add(new MailAddress(toEmail));
            mailMessage.Subject = subject;
            mailMessage.IsBodyHtml = true;

            // Embedding the image in the email body
            var htmlView = AlternateView.CreateAlternateViewFromString(
                body + "<br><br><img src=cid:EmbeddedImage>", null, "text/html");

            // Load the image and attach it as a linked resource
            var imageResource = new LinkedResource(imagePath, MediaTypeNames.Image.Png)
            {
                ContentId = "EmbeddedImage", // Must match the src attribute in the body
                TransferEncoding = TransferEncoding.Base64
            };

            htmlView.LinkedResources.Add(imageResource);
            mailMessage.AlternateViews.Add(htmlView);

            using var smtpClient = new SmtpClient(SmtpServer, SmtpPort);
            smtpClient.Credentials = new NetworkCredential(fromEmail, fromPassword);
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(mailMessage);
            Log.Information("Email with subject [{Subject}] sent successfully", subject);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to send email");
            throw new Exception("Failed to send email", e);
        }
    }
}
