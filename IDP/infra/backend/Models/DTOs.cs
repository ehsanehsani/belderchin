namespace Backend.Models;

// Internal helper class for session user info
public class SessionUserInfo
{
    public string? Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
}

// Request DTOs
public class CreateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public CourseMetadata Metadata { get; set; } = new();
}

public class CreateQuestionRequest
{
    public string Type { get; set; } = string.Empty;
    public string Farsi { get; set; } = string.Empty;
    public string English { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
    public List<QuestionBlank> Blanks { get; set; } = new();
    public List<string> ShuffledWords { get; set; } = new();
    public List<string> CorrectOrder { get; set; } = new();
}

public class SubmitProgressRequest
{
    public Dictionary<string, object> Answers { get; set; } = new();
    public int Score { get; set; }
}

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
    public Dictionary<string, object>? Preferences { get; set; }
}

// Response DTOs
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CourseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public CourseMetadata Metadata { get; set; } = new();
}

public class QuestionDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Farsi { get; set; } = string.Empty;
    public string English { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
    public List<QuestionBlank> Blanks { get; set; } = new();
    public List<string> ShuffledWords { get; set; } = new();
    public List<string> CorrectOrder { get; set; } = new();
}

public class UserProgressDto
{
    public string Id { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Score { get; set; }
    public Dictionary<string, object> Answers { get; set; } = new();
}

public class UserProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public Dictionary<string, object> Preferences { get; set; } = new();
    public UserStatistics Statistics { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
