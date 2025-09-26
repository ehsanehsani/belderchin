// Base question interface
export interface BaseQuestion {
  id: string
  type: QuestionType
  farsi: string
  difficulty: 'easy' | 'medium' | 'hard'
  tags: string[]
  explanation?: string
}

// Question types
export const QuestionType = {
  FILL_BLANK: 'fill_blank',
  MULTIPLE_BLANK: 'multiple_blank',
  WORD_ORDER: 'word_order',
  MULTIPLE_CHOICE: 'multiple_choice',
  TRUE_FALSE: 'true_false',
  MATCHING: 'matching'
} as const

export type QuestionType = typeof QuestionType[keyof typeof QuestionType]

// Fill in the blank (single blank)
export interface FillBlankQuestion extends BaseQuestion {
  type: typeof QuestionType.FILL_BLANK
  english: string // Contains _____ for blank
  options: string[]
  correctAnswer: string
}

// Multiple blanks
export interface MultipleBlankQuestion extends BaseQuestion {
  type: typeof QuestionType.MULTIPLE_BLANK
  english: string // Contains multiple _____
  blanks: {
    id: string
    options: string[]
    correctAnswer: string
  }[]
}

// Word ordering
export interface WordOrderQuestion extends BaseQuestion {
  type: typeof QuestionType.WORD_ORDER
  english: string // The correct sentence
  shuffledWords: string[]
  correctOrder: string[]
}

// Multiple choice
export interface MultipleChoiceQuestion extends BaseQuestion {
  type: typeof QuestionType.MULTIPLE_CHOICE
  question: string
  options: string[]
  correctAnswer: string
}

// True/False
export interface TrueFalseQuestion extends BaseQuestion {
  type: typeof QuestionType.TRUE_FALSE
  statement: string
  correctAnswer: boolean
}

// Matching pairs
export interface MatchingQuestion extends BaseQuestion {
  type: typeof QuestionType.MATCHING
  pairs: {
    id: string
    left: string
    right: string
  }[]
  shuffledRight: string[]
}

// Union type for all question types
export type Question = 
  | FillBlankQuestion 
  | MultipleBlankQuestion 
  | WordOrderQuestion 
  | MultipleChoiceQuestion 
  | TrueFalseQuestion 
  | MatchingQuestion

// Course structure
export interface CourseData {
  id: string
  title: string
  description?: string
  isActive: boolean
  questions: Question[]
  metadata: {
    episodeNumber?: number
    topic: string
    duration?: string
    level: 'beginner' | 'intermediate' | 'advanced'
    createdAt: string
    updatedAt: string
  }
}

// Question result for tracking answers
export interface QuestionResult {
  questionId: string
  type: QuestionType
  userAnswer: any // Can be string, string[], boolean, etc.
  isCorrect: boolean
  timeSpent?: number
  attempts?: number
}

// AI-friendly question template
export interface QuestionTemplate {
  type: QuestionType
  farsi: string
  english: string
  topic: string
  difficulty: 'easy' | 'medium' | 'hard'
  tags: string[]
  instructions?: string
}

// Database-ready interfaces (for future use)
export interface DatabaseCourse {
  id: string
  title: string
  description?: string
  isActive: boolean
  episodeNumber?: number
  topic: string
  duration?: string
  level: 'beginner' | 'intermediate' | 'advanced'
  createdAt: Date
  updatedAt: Date
}

export interface DatabaseQuestion {
  id: string
  courseId: string
  type: QuestionType
  farsi: string
  english: string
  options: string
  correctAnswer: string
  difficulty: 'easy' | 'medium' | 'hard'
  tags: string
  explanation?: string
  createdAt: Date
  updatedAt: Date
}
