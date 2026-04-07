namespace ServiceRequestService.Services;

public static class ServiceRequestWorkflow
{
    private static readonly Dictionary<string, HashSet<string>> PermitTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Submitted"] = new(StringComparer.OrdinalIgnoreCase) { "OfficerAssigned" },
        ["OfficerAssigned"] = new(StringComparer.OrdinalIgnoreCase) { "AwaitingDocuments", "UnderReview" },
        ["AwaitingDocuments"] = new(StringComparer.OrdinalIgnoreCase) { "UnderReview" },
        ["DocumentsRejected"] = new(StringComparer.OrdinalIgnoreCase) { "UnderReview" },
        ["UnderReview"] = new(StringComparer.OrdinalIgnoreCase) { "Approved", "DocumentsRejected", "Rejected" },
        ["Approved"] = new(StringComparer.OrdinalIgnoreCase),
        ["Rejected"] = new(StringComparer.OrdinalIgnoreCase)
    };

    private static readonly Dictionary<string, HashSet<string>> ComplaintTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Submitted"] = new(StringComparer.OrdinalIgnoreCase) { "OfficerAssigned" },
        ["OfficerAssigned"] = new(StringComparer.OrdinalIgnoreCase) { "UnderReview" },
        ["UnderReview"] = new(StringComparer.OrdinalIgnoreCase) { "Approved", "Rejected" },
        ["Approved"] = new(StringComparer.OrdinalIgnoreCase),
        ["Rejected"] = new(StringComparer.OrdinalIgnoreCase)
    };

    public static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Permit", "Complaint"
    };

    public static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Submitted", "OfficerAssigned", "AwaitingDocuments", "UnderReview", "Approved", "DocumentsRejected", "Rejected"
    };

    public static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status)) return status;

        return status.Trim() switch
        {
            "Pending" => "Submitted",
            "InProgress" => "UnderReview",
            "Resolved" => "Approved",
            _ => status.Trim()
        };
    }

    public static bool CanTransition(string type, string fromStatus, string toStatus)
    {
        var normalizedFrom = NormalizeStatus(fromStatus);
        var normalizedTo = NormalizeStatus(toStatus);

        var transitions = type.Equals("Permit", StringComparison.OrdinalIgnoreCase)
            ? PermitTransitions
            : ComplaintTransitions;

        return transitions.TryGetValue(normalizedFrom, out var allowed) && allowed.Contains(normalizedTo);
    }

    public static int GetProgressPercentage(string type, string status)
    {
        var normalizedStatus = NormalizeStatus(status);

        if (type.Equals("Permit", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedStatus switch
            {
                "Submitted" => 20,
                "OfficerAssigned" => 40,
                "AwaitingDocuments" => 60,
                "DocumentsRejected" => 60,
                "UnderReview" => 80,
                "Approved" => 100,
                "Rejected" => 100,
                _ => 0
            };
        }

        return normalizedStatus switch
        {
            "Submitted" => 25,
            "OfficerAssigned" => 50,
            "UnderReview" => 75,
            "Approved" => 100,
            "Rejected" => 100,
            _ => 0
        };
    }

    public static string GetProgressColor(string status)
    {
        var normalizedStatus = NormalizeStatus(status);

        return normalizedStatus switch
        {
            "Approved" => "green",
            "Rejected" => "red",
            "AwaitingDocuments" or "DocumentsRejected" => "yellow",
            _ => "blue"
        };
    }

    public static bool IsResubmittable(string type, string status)
    {
        return type.Equals("Permit", StringComparison.OrdinalIgnoreCase)
               && NormalizeStatus(status).Equals("DocumentsRejected", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsClosed(string status)
    {
        var normalizedStatus = NormalizeStatus(status);
        return normalizedStatus is "Approved" or "Rejected";
    }
}
