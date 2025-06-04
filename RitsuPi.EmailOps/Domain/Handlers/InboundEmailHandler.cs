using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using PostmarkDotNet.Webhooks;
using RitsuPi.EmailOps.Infrastructure.Configuration;
using RitsuPi.EmailOps.Infrastructure.Database;
using RitsuPi.EmailOps.Infrastructure.Services;

namespace RitsuPi.EmailOps.Domain.Handlers;

public interface IInboundEmailHandler
{
    Task Handle(PostmarkInboundWebhookMessage message, CancellationToken ct = default);
}

public partial class InboundEmailHandler : IInboundEmailHandler
{
    [GeneratedRegex(@"\[.+\]\((.+)\)")]
    private partial Regex MatchImagesPattern();

    [GeneratedRegex(@"<subject>(.+)<\/subject>", RegexOptions.Multiline | RegexOptions.Singleline)]
    private partial Regex MatchSubjectPattern();

    [GeneratedRegex(@"<markdown_body>(.+)<\/markdown_body>", RegexOptions.Multiline | RegexOptions.Singleline)]
    private partial Regex MatchBodyPattern();

    private readonly ILogger<InboundEmailHandler> _logger;
    private readonly GeminiConfig _geminiConfig;
    private readonly Kernel _kernel;
    private readonly RitsuOpsContext _context;
    private readonly IEmailSenderService _emailSender;

    public InboundEmailHandler(ILogger<InboundEmailHandler> logger, IOptions<GeminiConfig> geminiConfig, Kernel kernel,
        RitsuOpsContext context, IEmailSenderService emailSender)
    {
        _logger = logger;
        _geminiConfig = geminiConfig.Value;
        _kernel = kernel;
        _context = context;
        _emailSender = emailSender;
    }

    public async Task Handle(PostmarkInboundWebhookMessage message, CancellationToken ct = default)
    {
        // build history and append current request
        _kernel.Data["origin_email"] = message.From;
        var (semanticKernelHistory, chatHistory) = await GetOrCreateChatHistory(message, ct);
        chatHistory.AddUserMessage($"<subject>{message.Subject}</subject>\n<body>{message.TextBody}</body>");

        // generate response
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        var chatSettings = new GeminiPromptExecutionSettings
        {
            // https://github.com/microsoft/semantic-kernel/issues/10223#issuecomment-2600940589
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
        };

        var response =
            (GeminiChatMessageContent)await chatCompletion.GetChatMessageContentAsync(chatHistory, chatSettings,
                _kernel, cancellationToken: ct);

        chatHistory.AddAssistantMessage(response.Content);

        // send email response
        var (subject, bodyMarkdown, attachments) = PostProcessChatResponse(response.Content);
        await _emailSender.SendEmailAsync(semanticKernelHistory.ThreadHash, message.From, subject, bodyMarkdown, attachments, ct);

        // persist histories
        semanticKernelHistory.MemoryJson = JsonSerializer.Serialize(chatHistory);
        await _context.EmailHistories.AddRangeAsync([
            new EmailHistory
            {
                Id = Guid.CreateVersion7(),
                Direction = EmailDirection.Inbound,
                Email = message.From,
                Content = message.TextBody,
                CreatedAt = DateTime.UtcNow,
                SemanticKernelHistory = semanticKernelHistory,
            },
            new EmailHistory
            {
                Id = Guid.CreateVersion7(),
                Direction = EmailDirection.Outbound,
                Email = message.From,
                Content = response.Content,
                CreatedAt = DateTime.UtcNow,
                SemanticKernelHistory = semanticKernelHistory,
            }
        ], ct);

        await _context.SaveChangesAsync(ct);
    }

    private string GetThreadHash(string? mailboxHash)
    {
        if (string.IsNullOrWhiteSpace(mailboxHash))
        {
            var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(Guid.CreateVersion7().ToString()));
            return hashed.Aggregate(string.Empty, (current, b) => current + b.ToString("x2"))[..10];
        }

        return mailboxHash;
    }

    private async Task<(SemanticKernelHistory entity, ChatHistory chatHistory)> GetOrCreateChatHistory(
        PostmarkInboundWebhookMessage message, CancellationToken ct = default)
    {
        var hash = GetThreadHash(message.MailboxHash);
        var semanticHistory = await _context.MessageThreads.SingleOrDefaultAsync(x => x.ThreadHash == hash, ct);

        var chatHistory = new ChatHistory();
        if (semanticHistory is null)
        {
            chatHistory.AddSystemMessage(_geminiConfig.SystemPrompt);

            semanticHistory = new SemanticKernelHistory
            {
                Id = Guid.CreateVersion7(),
                ThreadHash = hash,
                CreatedAt = DateTime.UtcNow,
            };

            await _context.MessageThreads.AddAsync(semanticHistory, ct);
            return (semanticHistory, chatHistory);
        }

        chatHistory = JsonSerializer.Deserialize<ChatHistory>(semanticHistory.MemoryJson);
        return (semanticHistory, chatHistory);
    }

    private (string subject, string bodyMarkdown, List<string> attachments) PostProcessChatResponse(string content)
    {
        var subject = MatchSubjectPattern().Match(content).Groups[1].Value;
        var bodyMarkdown = MatchBodyPattern().Match(content).Groups[1].Value;

        var attachments = MatchImagesPattern().Matches(content).Select(match => match.Groups[1].Value).ToList();
        foreach (var attachment in attachments)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(attachment);
            bodyMarkdown = bodyMarkdown.Replace(attachment, $"cid:{fileNameWithoutExtension}");
        }

        var tempPath = Path.GetTempPath();
        attachments = attachments.Select(fileName => Path.Combine(tempPath, fileName)).ToList();

        return (subject, bodyMarkdown, attachments);
    }
}
