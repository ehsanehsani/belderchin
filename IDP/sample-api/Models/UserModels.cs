namespace sample_api.Models;

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UserData
{
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public DateTime LastAccess { get; set; }
}

public class UserPreferences
{
    public string UserId { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "light";
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = true;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

public class UpdateProfileRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Dictionary<string, object>? CustomProperties { get; set; }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class SystemStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int AdminUsers { get; set; }
    public int RegularUsers { get; set; }
    public DateTime LastUserRegistration { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
}

public class AdminDashboard
{
    public SystemStatistics Statistics { get; set; } = new();
    public List<UserProfile> RecentUsers { get; set; } = new();
    public List<UserProfile> ActiveAdmins { get; set; } = new();
    public Dictionary<string, object> SystemInfo { get; set; } = new();
}
