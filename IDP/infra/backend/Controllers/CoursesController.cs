using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AuthenticatedOnly")]
public class CoursesController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(IMemoryService memoryService, ILogger<CoursesController> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active courses
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActiveCourses()
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("User {UserId} requested active courses", userId);

        var courses = await _memoryService.GetActiveCoursesAsync();
        
        return Ok(new
        {
            message = "Active courses retrieved successfully",
            courses = courses,
            requestedBy = userId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get questions for a specific course
    /// </summary>
    [HttpGet("{courseId}/questions")]
    public async Task<IActionResult> GetCourseQuestions(string courseId)
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("User {UserId} requested questions for course {CourseId}", userId, courseId);

        var course = await _memoryService.GetCourseAsync(courseId);
        if (course == null || !course.IsActive)
        {
            return NotFound(new { message = "Course not found or inactive" });
        }
        
        var questions = await _memoryService.GetQuestionsByCourseIdAsync(courseId);
        
        return Ok(new
        {
            message = "Course questions retrieved successfully",
            courseId = courseId,
            courseTitle = course.Title,
            questions = questions,
            requestedBy = userId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create a new course (Backoffice only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "BackofficeOnly")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        var userId = User.FindFirst("userId")?.Value;
        var userType = User.FindFirst("userType")?.Value;
        _logger.LogInformation("Backoffice user {UserId} creating new course: {Title}", userId, request.Title);

        var course = new Course
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            IsActive = request.IsActive,
            Metadata = request.Metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // In a real implementation, this would save to database
        // await _memoryService.CreateCourseAsync(course);
        
        return Created($"/api/courses/{course.Id}", new
        {
            message = "Course created successfully",
            course = new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                IsActive = course.IsActive,
                Metadata = course.Metadata
            },
            createdBy = userId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Add question to course (Backoffice only)
    /// </summary>
    [HttpPost("{courseId}/questions")]
    [Authorize(Policy = "BackofficeOnly")]
    public async Task<IActionResult> AddQuestionToCourse(string courseId, [FromBody] CreateQuestionRequest request)
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("Backoffice user {UserId} adding question to course {CourseId}", userId, courseId);

        var course = await _memoryService.GetCourseAsync(courseId);
        if (course == null)
        {
            return NotFound(new { message = "Course not found" });
        }
        
        var question = new Question
        {
            Id = Guid.NewGuid().ToString(),
            CourseId = courseId,
            Type = request.Type,
            Farsi = request.Farsi,
            English = request.English,
            Options = request.Options,
            CorrectAnswer = request.CorrectAnswer,
            Difficulty = request.Difficulty,
            Tags = request.Tags,
            Explanation = request.Explanation,
            Blanks = request.Blanks,
            ShuffledWords = request.ShuffledWords,
            CorrectOrder = request.CorrectOrder
        };
        
        // In a real implementation, this would save to database
        // await _memoryService.CreateQuestionAsync(question);
        
        return Created($"/api/courses/{courseId}/questions/{question.Id}", new
        {
            message = "Question added to course successfully",
            question = new QuestionDto
            {
                Id = question.Id,
                Type = question.Type,
                Farsi = question.Farsi,
                English = question.English,
                Options = question.Options,
                CorrectAnswer = question.CorrectAnswer,
                Difficulty = question.Difficulty,
                Tags = question.Tags,
                Explanation = question.Explanation,
                Blanks = question.Blanks,
                ShuffledWords = question.ShuffledWords,
                CorrectOrder = question.CorrectOrder
            },
            courseId = courseId,
            createdBy = userId,
            timestamp = DateTime.UtcNow
        });
    }
}
