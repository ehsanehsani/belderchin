using Backend.Models;

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
    private static readonly List<Course> _courses = new();
    private static readonly Dictionary<string, List<Question>> _questions = new();
    private static readonly Dictionary<string, User> _users = new();
    private static readonly Dictionary<string, UserProfile> _userProfiles = new();
    private static readonly Dictionary<string, List<UserProgress>> _userProgress = new();

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

    public Task<User?> GetUserAsync(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        var user = _users.Values.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
        return Task.FromResult(user);
    }

    public Task CreateUserAsync(User user)
    {
        _users[user.Id] = user;
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

    public Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        _userProfiles.TryGetValue(userId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<UserProfile> UpdateUserProfileAsync(string userId, UpdateProfileRequest request)
    {
        var profile = _userProfiles.TryGetValue(userId, out var existingProfile) ? existingProfile : new UserProfile
        {
            UserId = userId,
            DisplayName = request.DisplayName ?? userId,
            Avatar = request.Avatar,
            Preferences = request.Preferences ?? new Dictionary<string, object>(),
            Statistics = new UserStatistics(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        if (request.DisplayName != null)
            profile.DisplayName = request.DisplayName;
        
        if (request.Avatar != null)
            profile.Avatar = request.Avatar;
        
        if (request.Preferences != null)
            profile.Preferences = request.Preferences;
        
        profile.UpdatedAt = DateTime.UtcNow;
        _userProfiles[userId] = profile;
        
        return Task.FromResult(profile);
    }

    public Task<bool> IsHealthyAsync()
    {
        return Task.FromResult(true);
    }
}
