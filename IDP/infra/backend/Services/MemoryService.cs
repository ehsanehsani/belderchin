using Backend.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Backend.Services;

public interface IMemoryService
{
    Task<List<CourseDto>> GetActiveCoursesAsync();
    Task<Course?> GetCourseAsync(string courseId);
    Task<List<QuestionDto>> GetQuestionsByCourseIdAsync(string courseId);
    Task<User?> GetUserAsync(string userId);
    Task<User?> GetUserByPhoneNumberAsync(string phoneNumber);
    Task CreateUserAsync(User user);
    Task<List<UserProgressDto>> GetUserProgressAsync(string userId);
    Task<UserProgressDto> UpdateUserProgressAsync(string userId, string courseId, Dictionary<string, object> answers, int score);
    Task<UserProfile?> GetUserProfileAsync(string userId);
    Task<UserProfile> UpdateUserProfileAsync(string userId, UpdateProfileRequest request);
    Task<bool> IsHealthyAsync();
}

public class MemoryService : IMemoryService
{
    private readonly IDatabase _redisDb;
    private static readonly List<Course> _courses = new();
    private static readonly Dictionary<string, List<Question>> _questions = new();
    private static readonly Dictionary<string, List<UserProgress>> _userProgress = new();

    public MemoryService(IDatabase redisDb)
    {
        _redisDb = redisDb;
    }

    public Task<List<CourseDto>> GetActiveCoursesAsync()
    {
        var courseDtos = _courses
            .Where(c => c.IsActive)
            .OrderBy(c => c.Metadata.EpisodeNumber)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                IsActive = c.IsActive,
                Metadata = c.Metadata
            })
            .ToList();
        
        return Task.FromResult(courseDtos);
    }

    public Task<Course?> GetCourseAsync(string courseId)
    {
        var course = _courses.FirstOrDefault(c => c.Id == courseId);
        return Task.FromResult(course);
    }

    public Task<List<QuestionDto>> GetQuestionsByCourseIdAsync(string courseId)
    {
        if (_questions.TryGetValue(courseId, out var questions))
        {
            var questionDtos = questions
                .Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Type = q.Type,
                    Farsi = q.Farsi,
                    English = q.English,
                    Options = q.Options,
                    CorrectAnswer = q.CorrectAnswer,
                    Difficulty = q.Difficulty,
                    Tags = q.Tags,
                    Explanation = q.Explanation,
                    Blanks = q.Blanks,
                    ShuffledWords = q.ShuffledWords,
                    CorrectOrder = q.CorrectOrder
                })
                .ToList();
            
            return Task.FromResult(questionDtos);
        }
        
        return Task.FromResult(new List<QuestionDto>());
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        try
        {
            var userDataJson = await _redisDb.StringGetAsync($"user:{userId}");
            if (!userDataJson.HasValue)
                return null;

            var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
            
            return new User
            {
                Id = userData.GetProperty("id").GetString() ?? userId,
                PhoneNumber = userData.GetProperty("phone").GetString() ?? "",
                UserType = userData.GetProperty("userType").GetString() == "Backoffice" ? UserType.Backoffice : UserType.Regular,
                CreatedAt = userData.GetProperty("createdAt").GetDateTimeOffset().DateTime,
                UpdatedAt = userData.GetProperty("updatedAt").GetDateTimeOffset().DateTime
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<User?> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        try
        {
            // This is inefficient but works for now
            // In a real implementation, you'd maintain a phone->userId index
            var server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(_redisDb.Database, "user:*");
            
            foreach (var key in keys)
            {
                var userDataJson = await _redisDb.StringGetAsync(key);
                if (userDataJson.HasValue)
                {
                    var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
                    var phone = userData.GetProperty("phone").GetString();
                    if (phone == phoneNumber)
                    {
                        return new User
                        {
                            Id = userData.GetProperty("id").GetString() ?? key.ToString().Replace("user:", ""),
                            PhoneNumber = phone,
                            UserType = userData.GetProperty("userType").GetString() == "Backoffice" ? UserType.Backoffice : UserType.Regular,
                            CreatedAt = userData.GetProperty("createdAt").GetDateTimeOffset().DateTime,
                            UpdatedAt = userData.GetProperty("updatedAt").GetDateTimeOffset().DateTime
                        };
                    }
                }
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task CreateUserAsync(User user)
    {
        // Users are created in Auth-Bridge, so this method is deprecated
        // Keeping for backward compatibility but should not be used
        return Task.CompletedTask;
    }

    public Task<List<UserProgressDto>> GetUserProgressAsync(string userId)
    {
        if (_userProgress.TryGetValue(userId, out var progress))
        {
            var progressDtos = progress
                .Select(p => new UserProgressDto
                {
                    Id = p.Id,
                    CourseId = p.CourseId,
                    IsCompleted = p.IsCompleted,
                    StartedAt = p.StartedAt,
                    CompletedAt = p.CompletedAt,
                    Score = p.Score,
                    Answers = p.Answers
                })
                .ToList();
            
            return Task.FromResult(progressDtos);
        }
        
        return Task.FromResult(new List<UserProgressDto>());
    }

    public Task<UserProgressDto> UpdateUserProgressAsync(string userId, string courseId, Dictionary<string, object> answers, int score)
    {
        if (!_userProgress.TryGetValue(userId, out var progress))
        {
            progress = new List<UserProgress>();
        }
        
        // Remove existing progress for this course
        progress.RemoveAll(p => p.CourseId == courseId);
        
        // Add new progress
        var newProgress = new UserProgress
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            CourseId = courseId,
            IsCompleted = score >= 80,
            StartedAt = DateTime.UtcNow,
            CompletedAt = score >= 80 ? DateTime.UtcNow : null,
            Score = score,
            Answers = answers
        };
        
        progress.Add(newProgress);
        _userProgress[userId] = progress;
        
        var progressDto = new UserProgressDto
        {
            Id = newProgress.Id,
            CourseId = newProgress.CourseId,
            IsCompleted = newProgress.IsCompleted,
            StartedAt = newProgress.StartedAt,
            CompletedAt = newProgress.CompletedAt,
            Score = newProgress.Score,
            Answers = newProgress.Answers
        };
        
        return Task.FromResult(progressDto);
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        try
        {
            var userDataJson = await _redisDb.StringGetAsync($"user:{userId}");
            if (!userDataJson.HasValue)
                return null;

            var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
            
            return new UserProfile
            {
                UserId = userId,
                DisplayName = userData.GetProperty("displayName").GetString() ?? userId,
                Avatar = userData.GetProperty("avatar").GetString() ?? "",
                Preferences = JsonSerializer.Deserialize<Dictionary<string, object>>(userData.GetProperty("preferences").GetRawText()) ?? new Dictionary<string, object>(),
                Statistics = new UserStatistics(),
                CreatedAt = userData.GetProperty("createdAt").GetDateTimeOffset().DateTime,
                UpdatedAt = userData.GetProperty("updatedAt").GetDateTimeOffset().DateTime
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<UserProfile> UpdateUserProfileAsync(string userId, UpdateProfileRequest request)
    {
        try
        {
            // Get existing user data from Redis
            var userDataJson = await _redisDb.StringGetAsync($"user:{userId}");
            if (!userDataJson.HasValue)
            {
                throw new KeyNotFoundException($"User {userId} not found");
            }

            var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(userDataJson.ToString()) ?? new Dictionary<string, object>();
            
            // Update profile fields
            if (!string.IsNullOrEmpty(request.DisplayName))
                userData["displayName"] = request.DisplayName;
            if (!string.IsNullOrEmpty(request.Avatar))
                userData["avatar"] = request.Avatar;
            if (request.Preferences != null)
                userData["preferences"] = request.Preferences;
            
            userData["updatedAt"] = DateTimeOffset.UtcNow;

            // Save back to Redis
            await _redisDb.StringSetAsync($"user:{userId}", JsonSerializer.Serialize(userData), TimeSpan.FromDays(365));

            return new UserProfile
            {
                UserId = userId,
                DisplayName = request.DisplayName ?? userData["displayName"].ToString() ?? userId,
                Avatar = request.Avatar ?? userData["avatar"].ToString() ?? "",
                Preferences = request.Preferences ?? JsonSerializer.Deserialize<Dictionary<string, object>>(userData["preferences"].ToString()) ?? new Dictionary<string, object>(),
                Statistics = new UserStatistics(),
                CreatedAt = userData.ContainsKey("createdAt") ? DateTimeOffset.Parse(userData["createdAt"].ToString()).DateTime : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception)
        {
            // Return default profile on error
            return new UserProfile
            {
                UserId = userId,
                DisplayName = request.DisplayName ?? userId,
                Avatar = request.Avatar ?? "",
                Preferences = request.Preferences ?? new Dictionary<string, object>(),
                Statistics = new UserStatistics(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    public Task<bool> IsHealthyAsync()
    {
        return Task.FromResult(true);
    }
}
