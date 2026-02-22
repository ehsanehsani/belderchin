using System.Text.Json.Serialization;

namespace Backend.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PhoneNumber { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum UserType
{
    Regular = 1,
    Backoffice = 2
}

public class Course
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public CourseMetadata Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CourseMetadata
{
    public int EpisodeNumber { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Question
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CourseId { get; set; } = string.Empty;
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

public class QuestionBlank
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
}

public class UserProgress
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Score { get; set; }
    public Dictionary<string, object> Answers { get; set; } = new();
}

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public Dictionary<string, object> Preferences { get; set; } = new();
    public UserStatistics Statistics { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserStatistics
{
    public int TotalCoursesCompleted { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int CorrectAnswers { get; set; }
    public double AverageScore { get; set; }
    public TimeSpan TotalStudyTime { get; set; }
    public DateTime LastActiveAt { get; set; }
    public List<string> CompletedCourses { get; set; } = new();
    public Dictionary<string, int> CourseScores { get; set; } = new();
}
