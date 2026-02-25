using Couchbase;
using Couchbase.KeyValue;
using System.Text.Json;
using Backend.Models;

namespace Backend.Services;

public interface ICouchbaseService
{
    Task<bool> IsHealthyAsync();
    Task<User?> GetUserAsync(string userId);
    Task<User?> GetUserByPhoneNumberAsync(string phoneNumber);
    Task CreateUserAsync(User user);
    Task<Course?> GetCourseAsync(string courseId);
    Task<List<CourseDto>> GetActiveCoursesAsync();
    Task CreateCourseAsync(Course course);
    Task UpdateCourseAsync(Course course);
    Task<List<QuestionDto>> GetQuestionsByCourseIdAsync(string courseId);
    Task CreateQuestionAsync(Question question);
    Task<List<UserProgressDto>> GetUserProgressAsync(string userId);
    Task<UserProgressDto> UpdateUserProgressAsync(string userId, string courseId, Dictionary<string, object> answers, int score);
    Task<UserProfile?> GetUserProfileAsync(string userId);
    Task<UserProfile> UpdateUserProfileAsync(string userId, UpdateProfileRequest request);
}

public class CouchbaseService : ICouchbaseService
{
    private readonly ICluster _cluster;
    private readonly IBucket _bucket;
    private readonly ILogger<CouchbaseService> _logger;

    public CouchbaseService(ICluster cluster, IBucket bucket, ILogger<CouchbaseService> logger)
    {
        _cluster = cluster;
        _bucket = bucket;
        _logger = logger;
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await _bucket.DefaultCollection().ExistsAsync("health-check");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couchbase health check failed");
            return false;
        }
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        try
        {
            var result = await _bucket.DefaultCollection().GetAsync($"user::{userId}");
            return JsonSerializer.Deserialize<User>(result.ContentAs<string>());
        }
        catch (Exception ex) when (ex.Message.Contains("document not found"))
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<User?> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        try
        {
            // In production, you'd create a proper index for phone number lookup
            // For now, we'll use a simple approach with a phone index document
            var result = await _bucket.DefaultCollection().GetAsync($"phone::{phoneNumber}");
            var userId = JsonSerializer.Deserialize<string>(result.ContentAs<string>());
            return await GetUserAsync(userId);
        }
        catch (Exception ex) when (ex.Message.Contains("document not found"))
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by phone {PhoneNumber}", phoneNumber);
            return null;
        }
    }

    public async Task CreateUserAsync(User user)
    {
        try
        {
            // Store user
            await _bucket.DefaultCollection().InsertAsync($"user::{user.Id}", JsonSerializer.Serialize(user));
            
            // Create phone index for lookup
            await _bucket.DefaultCollection().InsertAsync($"phone::{user.PhoneNumber}", JsonSerializer.Serialize(user.Id));
            
            // Create user profile
            var profile = new UserProfile
            {
                UserId = user.Id,
                DisplayName = user.PhoneNumber, // Default display name
                Avatar = null,
                Preferences = new Dictionary<string, object>(),
                Statistics = new UserStatistics(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _bucket.DefaultCollection().InsertAsync($"profile::{user.Id}", JsonSerializer.Serialize(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<Course?> GetCourseAsync(string courseId)
    {
        try
        {
            var result = await _bucket.DefaultCollection().GetAsync($"course::{courseId}");
            return JsonSerializer.Deserialize<Course>(result.ContentAs<string>());
        }
        catch (Exception ex) when (ex.Message.Contains("document not found"))
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course {CourseId}", courseId);
            return null;
        }
    }

    public async Task<List<CourseDto>> GetActiveCoursesAsync()
    {
        try
        {
            // In production, you'd use N1QL query with proper indexes
            // For now, we'll use a simple approach with course list document
            var result = await _bucket.DefaultCollection().GetAsync("course_list::active");
            var courseIds = JsonSerializer.Deserialize<List<string>>(result.ContentAs<string>());
            
            var courses = new List<CourseDto>();
            foreach (var courseId in courseIds ?? new List<string>())
            {
                var course = await GetCourseAsync(courseId);
                if (course != null && course.IsActive)
                {
                    courses.Add(new CourseDto
                    {
                        Id = course.Id,
                        Title = course.Title,
                        Description = course.Description,
                        IsActive = course.IsActive,
                        Metadata = course.Metadata
                    });
                }
            }
            
            return courses.OrderBy(c => c.Metadata.EpisodeNumber).ToList();
        }
        catch (Exception ex) when (ex.Message.Contains("document not found"))
        {
            return new List<CourseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active courses");
            return new List<CourseDto>();
        }
    }

    public async Task CreateCourseAsync(Course course)
    {
        try
        {
            await _bucket.DefaultCollection().InsertAsync($"course::{course.Id}", JsonSerializer.Serialize(course));
            
            // Update active courses list
            var activeCourses = await GetActiveCourseIds();
            activeCourses.Add(course.Id);
            await _bucket.DefaultCollection().UpsertAsync("course_list::active", JsonSerializer.Serialize(activeCourses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course {CourseId}", course.Id);
            throw;
        }
    }

    public async Task UpdateCourseAsync(Course course)
    {
        try
        {
            await _bucket.DefaultCollection().UpsertAsync($"course::{course.Id}", JsonSerializer.Serialize(course));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course {CourseId}", course.Id);
            throw;
        }
    }

    public async Task<List<QuestionDto>> GetQuestionsByCourseIdAsync(string courseId)
    {
        try
        {
            var result = await _bucket.DefaultCollection().GetAsync($"questions::{courseId}");
            var questions = JsonSerializer.Deserialize<List<Question>>(result.ContentAs<string>());
            
            return questions?.Select(q => new QuestionDto
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
            }).ToList() ?? new List<QuestionDto>();
        }
        catch (Exception ex) when (ex.Message.Contains("document not found"))
        {
            return new List<QuestionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions for course {CourseId}", courseId);
            return new List<QuestionDto>();
        }
    }

    public async Task CreateQuestionAsync(Question question)
    {
        try
        {
            // Get existing questions for the course
            var questions = new List<Question>();
            try
            {
                var result = await _bucket.DefaultCollection().GetAsync($"questions::{question.CourseId}");
                questions = JsonSerializer.Deserialize<List<Question>>(result.ContentAs<string>()) ?? new List<Question>();
            }
            catch (Exception ex) when (ex.Message.Contains("document not found"))
            {
                // First question for this course
            }
            
            questions.Add(question);
            await _bucket.DefaultCollection().UpsertAsync($"questions::{question.CourseId}", JsonSerializer.Serialize(questions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question {QuestionId}", question.Id);
            throw;
        }
    }

    public async Task<List<UserProgressDto>> GetUserProgressAsync(string userId)
    {
        try
        {
            var result = await _bucket.DefaultCollection().GetAsync($"progress::{userId}");
            var progress = JsonSerializer.Deserialize<List<UserProgress>>(result.ContentAs<string>());
            
            return progress?.Select(p => new UserProgressDto
            {
                Id = p.Id,
                CourseId = p.CourseId,
                IsCompleted = p.IsCompleted,
                StartedAt = p.StartedAt,
                CompletedAt = p.CompletedAt,
                Score = p.Score,
                Answers = p.Answers
            }).ToList() ?? new List<UserProgressDto>();
        }
        catch (Exception ex) when (ex.Message.Contains("document not found"))
        {
            return new List<UserProgressDto>();
        }
    }

    public async Task<UserProgressDto> UpdateUserProgressAsync(string userId, string courseId, Dictionary<string, object> answers, int score)
    {
        try
        {
            var progressList = new List<UserProgress>();
            try
            {
                var result = await _bucket.DefaultCollection().GetAsync($"progress::{userId}");
                progressList = JsonSerializer.Deserialize<List<UserProgress>>(result.ContentAs<string>()) ?? new List<UserProgress>();
            }
            catch (Exception ex) when (ex.Message.Contains("document not found"))
            {
                // First progress entry
            }
            
            // Remove existing progress for this course if any
            progressList.RemoveAll(p => p.CourseId == courseId);
            
            // Add new progress
            var progress = new UserProgress
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CourseId = courseId,
                IsCompleted = score >= 80, // Consider 80% as passing
                StartedAt = DateTime.UtcNow,
                CompletedAt = score >= 80 ? DateTime.UtcNow : null,
                Score = score,
                Answers = answers
            };
            
            progressList.Add(progress);
            await _bucket.DefaultCollection().UpsertAsync($"progress::{userId}", JsonSerializer.Serialize(progressList));
            
            return new UserProgressDto
            {
                Id = progress.Id,
                CourseId = progress.CourseId,
                IsCompleted = progress.IsCompleted,
                StartedAt = progress.StartedAt,
                CompletedAt = progress.CompletedAt,
                Score = progress.Score,
                Answers = progress.Answers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user progress for user {UserId}, course {CourseId}", userId, courseId);
            throw;
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        try
        {
            var result = await _bucket.DefaultCollection().GetAsync($"profile::{userId}");
            return JsonSerializer.Deserialize<UserProfile>(result.ContentAs<string>());
        }
        catch (Exception ex) when (ex.Message.Contains("document not found"))
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile {UserId}", userId);
            return null;
        }
    }

    public async Task<UserProfile> UpdateUserProfileAsync(string userId, UpdateProfileRequest request)
    {
        try
        {
            var profile = await GetUserProfileAsync(userId) ?? new UserProfile
            {
                UserId = userId,
                DisplayName = request.DisplayName ?? userId,
                Avatar = null,
                Preferences = new Dictionary<string, object>(),
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
            
            await _bucket.DefaultCollection().UpsertAsync($"profile::{userId}", JsonSerializer.Serialize(profile));
            
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile {UserId}", userId);
            throw;
        }
    }

    private async Task<List<string>> GetActiveCourseIds()
    {
        try
        {
            var result = await _bucket.DefaultCollection().GetAsync("course_list::active");
            return JsonSerializer.Deserialize<List<string>>(result.ContentAs<string>()) ?? new List<string>();
        }
        catch (Exception ex) when (ex.Message.Contains("document not found"))
        {
            return new List<string>();
        }
    }
}
