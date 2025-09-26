import type { Question, QuestionResult } from '../../types/QuestionTypes'
import FillBlankQuestionComponent from './FillBlankQuestion'
import MultipleBlankQuestionComponent from './MultipleBlankQuestion'
import WordOrderQuestionComponent from './WordOrderQuestion'

interface QuestionRendererProps {
  question: Question
  questionNumber: number
  result?: QuestionResult
  onAnswerSelect: (questionId: string, userAnswer: any, isCorrect: boolean) => void
}

const QuestionRenderer = ({ 
  question, 
  questionNumber, 
  result, 
  onAnswerSelect 
}: QuestionRendererProps) => {
  const handleAnswerSelect = (questionId: string, userAnswer: any, isCorrect: boolean) => {
    onAnswerSelect(questionId, userAnswer, isCorrect)
  }

  switch (question.type) {
    case 'fill_blank':
      return (
        <FillBlankQuestionComponent
          question={question}
          questionNumber={questionNumber}
          result={result}
          onAnswerSelect={handleAnswerSelect}
        />
      )

    case 'multiple_blank':
      return (
        <MultipleBlankQuestionComponent
          question={question}
          questionNumber={questionNumber}
          result={result}
          onAnswerSelect={handleAnswerSelect}
        />
      )

    case 'word_order':
      return (
        <WordOrderQuestionComponent
          question={question}
          questionNumber={questionNumber}
          result={result}
          onAnswerSelect={handleAnswerSelect}
        />
      )

    // TODO: Implement other question types
    case 'multiple_choice':
    case 'true_false':
    case 'matching':
      return (
        <div style={{ 
          padding: '16px', 
          border: '1px solid #e0e0e0', 
          borderRadius: '8px',
          backgroundColor: '#f5f5f5',
          textAlign: 'center'
        }}>
          <p>Question type "{question.type}" is coming soon!</p>
          <p>Question: {question.farsi}</p>
        </div>
      )

    default:
      return (
        <div style={{ 
          padding: '16px', 
          border: '1px solid #ff0000', 
          borderRadius: '8px',
          backgroundColor: '#ffebee',
          textAlign: 'center'
        }}>
          <p>Unknown question type: {(question as any).type}</p>
        </div>
      )
  }
}

export default QuestionRenderer
