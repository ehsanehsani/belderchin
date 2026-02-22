using System.Text.Json;
using Backend.Models;

namespace Backend.Data;

public class SampleDataSeeder
{
    public static List<Course> GetSampleCourses()
    {
        return new List<Course>
        {
            new Course
            {
                Id = "episode-15",
                Title = "Episode 15: Used To",
                Description = "Learn about 'used to' vs 'be used to' - expressing past habits and present adaptation",
                IsActive = true,
                Metadata = new CourseMetadata
                {
                    EpisodeNumber = 15,
                    Topic = "Used To",
                    Level = "intermediate",
                    Duration = "15 minutes",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    public static List<Question> GetSampleQuestions()
    {
        return new List<Question>
        {
            new Question
            {
                Id = "q1",
                CourseId = "episode-15",
                Type = "fill_blank",
                Farsi = "من به هوای سرد عادت دارم",
                English = "I _____ cold weather",
                Options = new List<string> { "am used to", "used to", "am getting used to" },
                CorrectAnswer = "am used to",
                Difficulty = "medium",
                Tags = new List<string> { "used to", "be used to", "weather", "present habits" },
                Explanation = "Use 'am used to' for present habits and adaptation"
            },
            new Question
            {
                Id = "q2",
                CourseId = "episode-15",
                Type = "fill_blank",
                Farsi = "او قبلاً اینجا زندگی می‌کرد",
                English = "He _____ live here",
                Options = new List<string> { "used to", "is used to", "was used to" },
                CorrectAnswer = "used to",
                Difficulty = "medium",
                Tags = new List<string> { "used to", "past habits", "living" },
                Explanation = "Use 'used to' for past habits that no longer exist"
            },
            new Question
            {
                Id = "q3",
                CourseId = "episode-15",
                Type = "fill_blank",
                Farsi = "آیا شما به کار کردن در شب عادت دارید؟",
                English = "Are you _____ working at night?",
                Options = new List<string> { "used to", "use to", "get used to" },
                CorrectAnswer = "used to",
                Difficulty = "medium",
                Tags = new List<string> { "used to", "be used to", "working", "night" },
                Explanation = "Use 'used to' after 'be' to ask about present adaptation"
            },
            new Question
            {
                Id = "q4",
                CourseId = "episode-15",
                Type = "multiple_blank",
                Farsi = "من قبلاً هر روز ورزش می‌کردم اما الان به کار کردن عادت دارم",
                English = "I _____ exercise every day, but now I _____ working",
                Blanks = new List<QuestionBlank>
                {
                    new QuestionBlank
                    {
                        Id = "blank1",
                        Options = new List<string> { "used to", "am used to", "get used to" },
                        CorrectAnswer = "used to"
                    },
                    new QuestionBlank
                    {
                        Id = "blank2",
                        Options = new List<string> { "used to", "am used to", "get used to" },
                        CorrectAnswer = "am used to"
                    }
                },
                Difficulty = "hard",
                Tags = new List<string> { "used to", "be used to", "exercise", "working", "comparison" },
                Explanation = "First blank: past habit (used to). Second blank: present adaptation (am used to)"
            },
            new Question
            {
                Id = "q5",
                CourseId = "episode-15",
                Type = "word_order",
                Farsi = "آیا شما قبلاً به اینجا می‌آمدید؟",
                English = "Did you used to come here?",
                ShuffledWords = new List<string> { "you", "Did", "here", "come", "used", "to", "?" },
                CorrectOrder = new List<string> { "Did", "you", "used", "to", "come", "here", "?" },
                Difficulty = "medium",
                Tags = new List<string> { "used to", "word order", "questions", "past habits" },
                Explanation = "In questions with 'used to', use 'Did + subject + used to + verb'"
            }
        };
    }

    public static List<User> GetSampleUsers()
    {
        return new List<User>
        {
            new User
            {
                Id = "admin-001",
                PhoneNumber = "+989123456789",
                UserType = UserType.Backoffice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = "user-001",
                PhoneNumber = "+989987654321",
                UserType = UserType.Regular,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }
}
