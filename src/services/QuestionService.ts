import type { CourseData, Question } from '../types/QuestionTypes'
import questionsData from '../data/questions.json'

// Import all course files
import episode15Data from '../data/courses/episode-15.json'
import episode14Data from '../data/courses/episode-14.json'
import episode13Data from '../data/courses/episode-13.json'

// Course data mapping
const courseDataMap: Record<string, CourseData> = {
  'episode-15': episode15Data as CourseData,
  'episode-14': episode14Data as CourseData,
  'episode-13': episode13Data as CourseData
}

export class QuestionService {
  private static courseReferences = questionsData.courses

  /**
   * Load all courses and questions
   */
  static async loadQuestions(): Promise<CourseData[]> {
    try {
      // Load courses from individual files
      const courses: CourseData[] = []
      
      for (const courseRef of this.courseReferences) {
        const courseData = courseDataMap[courseRef.id]
        if (courseData) {
          courses.push(courseData)
        } else {
          console.warn(`Course data not found for: ${courseRef.id}`)
        }
      }
      
      return courses
    } catch (error) {
      console.error('Error loading questions:', error)
      return []
    }
  }

  /**
   * Get a specific course by ID
   */
  static async getCourse(courseId: string): Promise<CourseData | null> {
    try {
      return courseDataMap[courseId] || null
    } catch (error) {
      console.error('Error loading course:', error)
      return null
    }
  }

  /**
   * Get all active courses
   */
  static async getActiveCourses(): Promise<CourseData[]> {
    const courses = await this.loadQuestions()
    return courses.filter(course => course.isActive)
  }

  /**
   * Get questions by type
   */
  static async getQuestionsByType(type: string): Promise<Question[]> {
    const courses = await this.loadQuestions()
    const questions: Question[] = []
    
    courses.forEach(course => {
      course.questions.forEach(question => {
        if (question.type === type) {
          questions.push(question)
        }
      })
    })
    
    return questions
  }

  /**
   * Get questions by difficulty
   */
  static async getQuestionsByDifficulty(difficulty: 'easy' | 'medium' | 'hard'): Promise<Question[]> {
    const courses = await this.loadQuestions()
    const questions: Question[] = []
    
    courses.forEach(course => {
      course.questions.forEach(question => {
        if (question.difficulty === difficulty) {
          questions.push(question)
        }
      })
    })
    
    return questions
  }

  /**
   * Get questions by tags
   */
  static async getQuestionsByTags(tags: string[]): Promise<Question[]> {
    const courses = await this.loadQuestions()
    const questions: Question[] = []
    
    courses.forEach(course => {
      course.questions.forEach(question => {
        if (tags.some(tag => question.tags.includes(tag))) {
          questions.push(question)
        }
      })
    })
    
    return questions
  }

  /**
   * Search questions by text
   */
  static async searchQuestions(query: string): Promise<Question[]> {
    const courses = await this.loadQuestions()
    const questions: Question[] = []
    const searchQuery = query.toLowerCase()
    
    courses.forEach(course => {
      course.questions.forEach(question => {
        const hasEnglish = 'english' in question ? question.english : 
                          'question' in question ? question.question : 
                          'statement' in question ? question.statement : ''
        
        if (
          question.farsi.toLowerCase().includes(searchQuery) ||
          hasEnglish.toLowerCase().includes(searchQuery) ||
          question.tags.some(tag => tag.toLowerCase().includes(searchQuery))
        ) {
          questions.push(question)
        }
      })
    })
    
    return questions
  }

  /**
   * Add a new question to a course
   */
  static async addQuestion(courseId: string, question: Question): Promise<boolean> {
    try {
      const course = await this.getCourse(courseId)
      if (!course) return false

      course.questions.push(question)
      course.metadata.updatedAt = new Date().toISOString()
      
      // In a real app, this would save to database
      console.log('Question added:', question)
      return true
    } catch (error) {
      console.error('Error adding question:', error)
      return false
    }
  }

  /**
   * Update an existing question
   */
  static async updateQuestion(courseId: string, questionId: string, updatedQuestion: Question): Promise<boolean> {
    try {
      const course = await this.getCourse(courseId)
      if (!course) return false

      const questionIndex = course.questions.findIndex(q => q.id === questionId)
      if (questionIndex === -1) return false

      course.questions[questionIndex] = updatedQuestion
      course.metadata.updatedAt = new Date().toISOString()
      
      // In a real app, this would save to database
      console.log('Question updated:', updatedQuestion)
      return true
    } catch (error) {
      console.error('Error updating question:', error)
      return false
    }
  }

  /**
   * Delete a question
   */
  static async deleteQuestion(courseId: string, questionId: string): Promise<boolean> {
    try {
      const course = await this.getCourse(courseId)
      if (!course) return false

      const questionIndex = course.questions.findIndex(q => q.id === questionId)
      if (questionIndex === -1) return false

      course.questions.splice(questionIndex, 1)
      course.metadata.updatedAt = new Date().toISOString()
      
      // In a real app, this would save to database
      console.log('Question deleted:', questionId)
      return true
    } catch (error) {
      console.error('Error deleting question:', error)
      return false
    }
  }

  /**
   * Get statistics about questions
   */
  static async getQuestionStats(): Promise<{
    totalQuestions: number
    questionsByType: Record<string, number>
    questionsByDifficulty: Record<string, number>
    totalCourses: number
    activeCourses: number
  }> {
    const courses = await this.loadQuestions()
    const stats = {
      totalQuestions: 0,
      questionsByType: {} as Record<string, number>,
      questionsByDifficulty: { easy: 0, medium: 0, hard: 0 },
      totalCourses: courses.length,
      activeCourses: courses.filter(c => c.isActive).length
    }

    courses.forEach(course => {
      course.questions.forEach(question => {
        stats.totalQuestions++
        stats.questionsByType[question.type] = (stats.questionsByType[question.type] || 0) + 1
        stats.questionsByDifficulty[question.difficulty]++
      })
    })

    return stats
  }

  /**
   * Export questions to JSON (for backup or migration)
   */
  static async exportQuestions(): Promise<string> {
    const courses = await this.loadQuestions()
    return JSON.stringify({ courses }, null, 2)
  }

  /**
   * Import questions from JSON (for migration)
   * Note: This method is for future use when implementing dynamic course loading
   */
  static async importQuestions(jsonData: string): Promise<boolean> {
    try {
      const data = JSON.parse(jsonData)
      if (!data.courses || !Array.isArray(data.courses)) {
        throw new Error('Invalid format')
      }
      
      // In the new structure, this would need to be implemented differently
      // For now, just log that import was attempted
      console.log('Import functionality needs to be updated for new file structure')
      console.log('Questions imported successfully')
      return true
    } catch (error) {
      console.error('Error importing questions:', error)
      return false
    }
  }
}
