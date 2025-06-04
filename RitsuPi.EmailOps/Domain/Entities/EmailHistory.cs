namespace RitsuPi.EmailOps.Domain.Entities;

public class EmailHistory
{
    public Guid Id { get; set; }
    
    public string Email { get; set; }
    public EmailDirection Direction { get; set; }
    public string Content { get; set; }
    public Guid SemanticKernelHistoryId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public SemanticKernelHistory SemanticKernelHistory { get; set; }
}
