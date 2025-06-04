namespace RitsuPi.EmailOps.Infrastructure.Configuration;

public class PostmarkConfig
{
    public string ServerToken { get; set; }
    public string ReplyToPattern { get; set; }
    public string FromEmail { get; set; }
    public string[] AuthorizedEmails { get; set; }
}
