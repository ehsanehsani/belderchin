using sample_api.Models;

namespace sample_api.Services;

public interface IUserService
{
    UserProfile GetUserProfile(string userId);
    UserData GetUserData(string userId);
    UserProfile UpdateUserProfile(string userId, UpdateProfileRequest request);
    UserPreferences GetUserPreferences(string userId);
    List<UserProfile> GetAllUsers();
    SystemStatistics GetSystemStatistics();
    OperationResult UpdateUserRole(string userId, string newRole);
    OperationResult DeleteUser(string userId);
    AdminDashboard GetAdminDashboard();
}
