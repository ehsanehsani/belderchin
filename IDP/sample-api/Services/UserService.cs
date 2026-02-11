using sample_api.Models;

namespace sample_api.Services;

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private static readonly List<UserProfile> _users = new();
    private static readonly Dictionary<string, UserData> _userData = new();
    private static readonly Dictionary<string, UserPreferences> _userPreferences = new();

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Add some sample users for demonstration
        if (!_users.Any())
        {
            _users.AddRange(new[]
            {
                new UserProfile
                {
                    Id = "user-123",
                    Phone = "+989121234567",
                    Email = "user@example.com",
                    Role = "user",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    IsActive = true
                },
                new UserProfile
                {
                    Id = "admin-456",
                    Phone = "+989128765432",
                    Email = "admin@example.com",
                    Role = "admin",
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    IsActive = true
                },
                new UserProfile
                {
                    Id = "user-789",
                    Phone = "+989121112223",
                    Email = "user2@example.com",
                    Role = "user",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    IsActive = true
                }
            });

            // Initialize sample user data
            foreach (var user in _users)
            {
                _userData[user.Id] = new UserData
                {
                    UserId = user.Id,
                    Properties = new Dictionary<string, object>
                    {
                        ["loginCount"] = Random.Shared.Next(1, 100),
                        ["lastLogin"] = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                        ["preferences"] = new { theme = "dark", language = "en" }
                    },
                    LastAccess = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 7))
                };

                _userPreferences[user.Id] = new UserPreferences
                {
                    UserId = user.Id,
                    Language = "en",
                    Theme = user.Role == "admin" ? "dark" : "light",
                    EmailNotifications = true,
                    SmsNotifications = user.Role == "user",
                    CustomSettings = new Dictionary<string, object>
                    {
                        ["timezone"] = "UTC",
                        ["currency"] = "USD"
                    }
                };
            }
        }
    }

    public UserProfile GetUserProfile(string userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning($"User profile not found for ID: {userId}");
            return new UserProfile { Id = userId, Phone = "Unknown", Email = "Unknown" };
        }
        return user;
    }

    public UserData GetUserData(string userId)
    {
        if (_userData.TryGetValue(userId, out var data))
        {
            data.LastAccess = DateTime.UtcNow;
            return data;
        }

        return new UserData
        {
            UserId = userId,
            Properties = new Dictionary<string, object>(),
            LastAccess = DateTime.UtcNow
        };
    }

    public UserProfile UpdateUserProfile(string userId, UpdateProfileRequest request)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            // Create new user if not found
            user = new UserProfile
            {
                Id = userId,
                Phone = "Unknown",
                Email = request.Email ?? "unknown@example.com",
                Role = "user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _users.Add(user);
        }
        else
        {
            if (!string.IsNullOrEmpty(request.Email))
                user.Email = request.Email;
            user.UpdatedAt = DateTime.UtcNow;
        }

        _logger.LogInformation($"User profile updated for ID: {userId}");
        return user;
    }

    public UserPreferences GetUserPreferences(string userId)
    {
        if (_userPreferences.TryGetValue(userId, out var preferences))
        {
            return preferences;
        }

        return new UserPreferences
        {
            UserId = userId,
            Language = "en",
            Theme = "light",
            EmailNotifications = true,
            SmsNotifications = true
        };
    }

    public List<UserProfile> GetAllUsers()
    {
        return _users.Where(u => u.IsActive).ToList();
    }

    public SystemStatistics GetSystemStatistics()
    {
        var activeUsers = _users.Count(u => u.IsActive);
        var adminUsers = _users.Count(u => u.Role == "admin" && u.IsActive);
        var regularUsers = activeUsers - adminUsers;

        return new SystemStatistics
        {
            TotalUsers = _users.Count,
            ActiveUsers = activeUsers,
            AdminUsers = adminUsers,
            RegularUsers = regularUsers,
            LastUserRegistration = _users.OrderByDescending(u => u.CreatedAt).FirstOrDefault()?.CreatedAt ?? DateTime.UtcNow,
            UsersByRole = _users.Where(u => u.IsActive)
                .GroupBy(u => u.Role)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public OperationResult UpdateUserRole(string userId, string newRole)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            return new OperationResult
            {
                Success = false,
                Message = $"User with ID {userId} not found"
            };
        }

        var oldRole = user.Role;
        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation($"User {userId} role changed from {oldRole} to {newRole}");

        return new OperationResult
        {
            Success = true,
            Message = $"User role updated successfully from {oldRole} to {newRole}",
            Data = new { userId, oldRole, newRole }
        };
    }

    public OperationResult DeleteUser(string userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            return new OperationResult
            {
                Success = false,
                Message = $"User with ID {userId} not found"
            };
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation($"User {userId} marked as inactive (soft deleted)");

        return new OperationResult
        {
            Success = true,
            Message = $"User {userId} deleted successfully",
            Data = new { userId, deletedAt = DateTime.UtcNow }
        };
    }

    public AdminDashboard GetAdminDashboard()
    {
        var stats = GetSystemStatistics();
        var recentUsers = _users
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToList();

        var activeAdmins = _users
            .Where(u => u.IsActive && u.Role == "admin")
            .ToList();

        return new AdminDashboard
        {
            Statistics = stats,
            RecentUsers = recentUsers,
            ActiveAdmins = activeAdmins,
            SystemInfo = new Dictionary<string, object>
            {
                ["serverTime"] = DateTime.UtcNow,
                ["version"] = "1.0.0",
                ["environment"] = "development",
                ["totalApiEndpoints"] = 12
            }
        };
    }
}
