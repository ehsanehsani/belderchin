import type { Question, QuestionTemplate } from '../types/QuestionTypes'
import { QuestionType } from '../types/QuestionTypes'
import templatesData from '../data/templates.json'

export class AIQuestionGenerator {
  private static templates = templatesData.questionTemplates
  private static prompts = templatesData.aiPrompts

  /**
   * Generate questions using AI based on a template
   */
  static async generateQuestions(
    template: QuestionTemplate,
    count: number = 5
  ): Promise<Question[]> {
    try {
      const prompt = this.buildPrompt(template, count)
      
      // In a real implementation, this would call an AI API
      // For now, we'll simulate the response
      console.log('AI Prompt:', prompt)
      
      // Simulate AI response - in real app, replace with actual AI API call
      const generatedQuestions = await this.simulateAIResponse(template, count)
      
      return generatedQuestions
    } catch (error) {
      console.error('Error generating questions:', error)
      return []
    }
  }

  /**
   * Build AI prompt from template
   */
  private static buildPrompt(template: QuestionTemplate, count: number): string {
    const basePrompt = this.prompts.generateQuestions
    const templateInfo = this.templates.find(t => t.type === template.type)
    
    if (!templateInfo) {
      throw new Error(`Template not found for type: ${template.type}`)
    }

    return basePrompt
      .replace('{topic}', template.topic)
      .replace('{count}', count.toString())
      .replace('{type}', template.type)
      .replace('{difficulty}', template.difficulty)
      + `\n\nTemplate: ${templateInfo.aiPrompt}`
      + `\n\nContext: ${JSON.stringify(template, null, 2)}`
  }

  /**
   * Simulate AI response (replace with actual AI API call)
   */
  private static async simulateAIResponse(
    template: QuestionTemplate,
    count: number
  ): Promise<Question[]> {
    // This is a simulation - replace with actual AI API call
    const questions: Question[] = []
    
    for (let i = 0; i < count; i++) {
      const question = this.createSampleQuestion(template, i + 1)
      questions.push(question)
    }
    
    return questions
  }

  /**
   * Create a sample question based on template (for simulation)
   */
  private static createSampleQuestion(
    template: QuestionTemplate,
    index: number
  ): Question {
    const baseQuestion = {
      id: `ai_generated_${Date.now()}_${index}`,
      type: template.type,
      farsi: template.farsi,
      difficulty: template.difficulty,
      tags: template.tags,
      explanation: `AI-generated question about ${template.topic}`
    }

    switch (template.type) {
      case QuestionType.FILL_BLANK:
        return {
          ...baseQuestion,
          type: QuestionType.FILL_BLANK,
          english: template.english,
          options: ['Option 1', 'Option 2', 'Option 3'],
          correctAnswer: 'Option 1'
        }

      case QuestionType.MULTIPLE_BLANK:
        return {
          ...baseQuestion,
          type: QuestionType.MULTIPLE_BLANK,
          english: template.english,
          blanks: [
            {
              id: 'blank1',
              options: ['Option 1', 'Option 2', 'Option 3'],
              correctAnswer: 'Option 1'
            }
          ]
        }

      case QuestionType.WORD_ORDER:
        return {
          ...baseQuestion,
          type: QuestionType.WORD_ORDER,
          english: template.english,
          shuffledWords: template.english.split(' ').sort(() => Math.random() - 0.5),
          correctOrder: template.english.split(' ')
        }

      case QuestionType.MULTIPLE_CHOICE:
        return {
          ...baseQuestion,
          type: QuestionType.MULTIPLE_CHOICE,
          question: template.english,
          options: ['Option 1', 'Option 2', 'Option 3', 'Option 4'],
          correctAnswer: 'Option 1'
        }

      case QuestionType.TRUE_FALSE:
        return {
          ...baseQuestion,
          type: QuestionType.TRUE_FALSE,
          statement: template.english,
          correctAnswer: true
        }

      case QuestionType.MATCHING:
        return {
          ...baseQuestion,
          type: QuestionType.MATCHING,
          pairs: [
            {
              id: 'pair1',
              left: template.farsi,
              right: template.english
            }
          ],
          shuffledRight: [template.english]
        }

      default:
        throw new Error(`Unsupported question type: ${template.type}`)
    }
  }

  /**
   * Validate questions using AI
   */
  static async validateQuestions(questions: Question[]): Promise<{
    isValid: boolean
    feedback: string
    suggestions: string[]
  }> {
    try {
      const prompt = this.prompts.validateQuestions
      console.log('Validation Prompt:', prompt)
      
      // In a real implementation, this would call an AI API
      // For now, we'll simulate the response
      const validation = await this.simulateValidation(questions)
      
      return validation
    } catch (error) {
      console.error('Error validating questions:', error)
      return {
        isValid: false,
        feedback: 'Validation failed',
        suggestions: []
      }
    }
  }

  /**
   * Simulate validation response
   */
  private static async simulateValidation(questions: Question[]): Promise<{
    isValid: boolean
    feedback: string
    suggestions: string[]
  }> {
    // Simple validation logic
    const isValid = questions.every(q => {
      const hasEnglish = 'english' in q ? q.english : 
                        'question' in q ? q.question : 
                        'statement' in q ? q.statement : false
      return q.farsi && hasEnglish && q.difficulty && q.tags.length > 0
    })
    
    return {
      isValid,
      feedback: isValid ? 'Questions are valid' : 'Some questions have issues',
      suggestions: isValid ? [] : ['Check Farsi text', 'Verify English text', 'Add tags']
    }
  }

  /**
   * Adapt questions to different difficulty levels
   */
  static async adaptDifficulty(
    questions: Question[],
    targetLevel: 'easy' | 'medium' | 'hard'
  ): Promise<Question[]> {
    try {
      const prompt = this.prompts.adaptDifficulty
      console.log('Adaptation Prompt:', prompt)
      
      // In a real implementation, this would call an AI API
      // For now, we'll simulate the response
      const adaptedQuestions = await this.simulateAdaptation(questions, targetLevel)
      
      return adaptedQuestions
    } catch (error) {
      console.error('Error adapting questions:', error)
      return questions
    }
  }

  /**
   * Simulate adaptation response
   */
  private static async simulateAdaptation(
    questions: Question[],
    targetLevel: 'easy' | 'medium' | 'hard'
  ): Promise<Question[]> {
    return questions.map(question => ({
      ...question,
      difficulty: targetLevel,
      explanation: `Adapted to ${targetLevel} level`
    }))
  }

  /**
   * Get available templates
   */
  static getTemplates() {
    return this.templates
  }

  /**
   * Get template by type
   */
  static getTemplate(type: QuestionType) {
    return this.templates.find(t => t.type === type)
  }

  /**
   * Create a question template from existing data
   */
  static createTemplate(
    type: QuestionType,
    farsi: string,
    english: string,
    topic: string,
    difficulty: 'easy' | 'medium' | 'hard' = 'medium',
    tags: string[] = [],
    instructions?: string
  ): QuestionTemplate {
    return {
      type,
      farsi,
      english,
      topic,
      difficulty,
      tags,
      instructions
    }
  }

  /**
   * Generate questions from podcast transcript
   */
  static async generateFromTranscript(
    transcript: string,
    topic: string,
    questionTypes: string[] = [QuestionType.FILL_BLANK],
    count: number = 5
  ): Promise<Question[]> {
    try {
      const prompt = `Based on this podcast transcript about "${topic}", generate ${count} questions for English learners:

Transcript: ${transcript}

Generate questions of types: ${questionTypes.join(', ')}
Each question should test understanding of the key concepts from the transcript.
Include Farsi translations for context.
Return in JSON format.`

      console.log('Transcript Prompt:', prompt)
      
      // In a real implementation, this would call an AI API
      // For now, we'll simulate the response
      const questions = await this.simulateTranscriptQuestions(transcript, topic, questionTypes, count)
      
      return questions
    } catch (error) {
      console.error('Error generating from transcript:', error)
      return []
    }
  }

  /**
   * Simulate transcript-based question generation
   */
  private static async simulateTranscriptQuestions(
    _transcript: string,
    topic: string,
    questionTypes: string[],
    count: number
  ): Promise<Question[]> {
    const questions: Question[] = []
    
    for (let i = 0; i < count; i++) {
      const type = questionTypes[i % questionTypes.length]
      const template = this.createTemplate(
        type as any,
        `Sample Farsi text about ${topic}`,
        `Sample English text about ${topic}`,
        topic
      )
      
      const question = this.createSampleQuestion(template, i + 1)
      questions.push(question)
    }
    
    return questions
  }
}
