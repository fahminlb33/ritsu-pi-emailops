namespace RitsuPi.EmailOps.Domain.Entities;

public class SemanticKernelHistory
{
    public Guid Id { get; set; }
    
    public string ThreadHash { get; set; }
    public string MemoryJson { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ICollection<EmailHistory> EmailHistories { get; set; }
}
