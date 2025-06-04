using Markdig;
using Microsoft.Extensions.Options;
using PostmarkDotNet;
using RitsuPi.EmailOps.Infrastructure.Configuration;

namespace RitsuPi.EmailOps.Infrastructure.Services;

public interface IEmailSenderService
{
    Task SendEmailAsync(string threadHash, string toEmail, string subject, string markdownBody,
        List<string> attachmentPaths, CancellationToken ct = default);
}

public class EmailSenderService : IEmailSenderService
{
    private readonly ILogger<EmailSenderService> _logger;
    private readonly PostmarkConfig _config;
    private readonly PostmarkClient _postmark;

    public EmailSenderService(ILogger<EmailSenderService> logger, IOptions<PostmarkConfig> postmarkConfig)
    {
        _logger = logger;
        _config = postmarkConfig.Value;
        _postmark = new PostmarkClient(_config.ServerToken);
    }

    public async Task SendEmailAsync(string threadHash, string toEmail, string subject, string markdownBody,
        List<string> attachmentPaths, CancellationToken ct = default)
    {
        try
        {
            var message = new PostmarkMessage
            {
                To = toEmail,
                From = _config.FromEmail,
                ReplyTo = string.Format(_config.ReplyToPattern, threadHash),
                TrackOpens = false,
                Subject = subject,
                TextBody = Markdown.ToPlainText(markdownBody),
                HtmlBody = Markdown.ToHtml(markdownBody),
                Tag = "Ristu-Pi EmailOps",
                MessageStream = "outbound",
            };

            if (attachmentPaths.Count > 0)
            {
                foreach (var attachment in attachmentPaths)
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(attachment);
                    var attachmentContents = await File.ReadAllBytesAsync(attachment, ct);
                    message.AddAttachment(attachmentContents, fileNameWithoutExtension + ".png", "image/png", $"cid:{fileNameWithoutExtension}");
                }
            }

            var result = await _postmark.SendMessageAsync(message);
            _logger.LogInformation("Email sent! ID: {0}", result.MessageID);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send email");
        }
    }
}
