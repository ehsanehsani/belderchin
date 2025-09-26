export interface Question {
  id: string
  farsi: string
  english: string
  options: string[]
  correctAnswer: string
}

export interface CourseData {
  id: string
  title: string
  isActive: boolean
  questions: Question[]
}

export interface QuestionResult {
  questionId: string
  selectedAnswer: string
  isCorrect: boolean
}
