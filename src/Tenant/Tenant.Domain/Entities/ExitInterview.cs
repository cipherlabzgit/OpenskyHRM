namespace Tenant.Domain.Entities;

public class ExitInterview
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime InterviewDate { get; set; }
    public Guid? InterviewerId { get; set; }
    public string? SeparationReason { get; set; }
    public string? PrimaryReasonForLeaving { get; set; }
    public int? OverallSatisfactionRating { get; set; }
    public int? ManagementRating { get; set; }
    public int? WorkEnvironmentRating { get; set; }
    public int? CompensationRating { get; set; }
    public int? GrowthOpportunitiesRating { get; set; }
    public string? WhatLikedMost { get; set; }
    public string? WhatLikedLeast { get; set; }
    public string? Suggestions { get; set; }
    public bool? WouldRecommend { get; set; }
    public bool? WouldRejoin { get; set; }
    public string? AdditionalComments { get; set; }
    public bool IsConfidential { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
