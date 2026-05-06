namespace DocumentService.Services;

public static class DocumentWorkflow
{
    private static readonly Dictionary<string, HashSet<string>> Transitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Submitted"] = new(StringComparer.OrdinalIgnoreCase) { "UnderReview" },
        ["UnderReview"] = new(StringComparer.OrdinalIgnoreCase) { "Approved", "Rejected" },
        ["Approved"] = new(StringComparer.OrdinalIgnoreCase),
        ["Rejected"] = new(StringComparer.OrdinalIgnoreCase)
    };

    public static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Submitted", "UnderReview", "Approved", "Rejected"
    };

    public static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status)) return status;

        return status.Trim() switch
        {
            "Pending" => "Submitted",
            "Processing" => "UnderReview",
            "Ready" => "Approved",
            "Collected" => "Approved",
            _ => status.Trim()
        };
    }

    public static bool CanTransition(string fromStatus, string toStatus)
    {
        var from = NormalizeStatus(fromStatus);
        var to = NormalizeStatus(toStatus);

        return Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static int GetProgressPercentage(string status)
    {
        return NormalizeStatus(status) switch
        {
            "Submitted" => 33,
            "UnderReview" => 66,
            "Approved" => 100,
            "Rejected" => 100,
            _ => 0
        };
    }

    public static string GetProgressColor(string status)
    {
        return NormalizeStatus(status) switch
        {
            "Approved" => "green",
            "Rejected" => "red",
            "UnderReview" => "yellow",
            _ => "blue"
        };
    }

    public static bool IsFinal(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is "Approved" or "Rejected";
    }
}
