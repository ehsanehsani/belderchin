import { useState } from 'react'
import {
  Box,
  Typography,
  FormControl,
  Select,
  MenuItem,
  Paper
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faTimes } from '@fortawesome/free-solid-svg-icons'
import type { MultipleBlankQuestion, QuestionResult } from '../../types/QuestionTypes'

interface MultipleBlankQuestionProps {
  question: MultipleBlankQuestion
  questionNumber: number
  result?: QuestionResult
  onAnswerSelect: (questionId: string, selectedAnswer: Record<string, string>, isCorrect: boolean) => void
}

const MultipleBlankQuestionComponent = ({ 
  question, 
  questionNumber, 
  result, 
  onAnswerSelect 
}: MultipleBlankQuestionProps) => {
  const [selectedValues, setSelectedValues] = useState<Record<string, string>>(
    result?.userAnswer || {}
  )

  const handleChange = (blankId: string, event: SelectChangeEvent) => {
    const selected = event.target.value
    const newValues = { ...selectedValues, [blankId]: selected }
    setSelectedValues(newValues)
    
    // Check if all blanks are filled and if they're correct
    const allFilled = question.blanks.every(blank => newValues[blank.id])
    if (allFilled) {
      const isCorrect = question.blanks.every(blank => 
        newValues[blank.id] === blank.correctAnswer
      )
      // Store as object with blank IDs as keys, not as array
      onAnswerSelect(question.id, newValues, isCorrect)
    }
  }

  const getBlankResult = (blankId: string) => {
    if (!result || !result.userAnswer) return null
    const blank = question.blanks.find(b => b.id === blankId)
    if (!blank) return null
    
    // Handle both object and array formats for backward compatibility
    const userAnswer = typeof result.userAnswer === 'object' && !Array.isArray(result.userAnswer) 
      ? result.userAnswer[blankId] 
      : null
    
    if (userAnswer === null) return null
    
    return {
      isCorrect: userAnswer === blank.correctAnswer,
      userAnswer
    }
  }

  const getStatusIcon = (blankId: string) => {
    const blankResult = getBlankResult(blankId)
    if (!blankResult) return null
    
    return (
      <Box sx={{ ml: 1, display: 'flex', alignItems: 'center' }}>
        <FontAwesomeIcon
          icon={blankResult.isCorrect ? faCheck : faTimes}
          style={{
            color: blankResult.isCorrect ? '#4caf50' : '#f44336',
            fontSize: '1rem'
          }}
        />
      </Box>
    )
  }

  const getSelectColor = (blankId: string) => {
    const blankResult = getBlankResult(blankId)
    if (!blankResult) return 'primary'
    return blankResult.isCorrect ? 'success' : 'error'
  }

  // Split the English sentence by blanks
  const renderEnglishWithBlanks = () => {
    const parts = question.english.split('_____')
    const elements: React.ReactNode[] = []
    
    parts.forEach((part, index) => {
      // Add the text part
      if (part) {
        elements.push(
          <Typography 
            key={`text-${index}`}
            variant="body1" 
            sx={{ 
              fontSize: { xs: '1rem', md: '1.1rem' },
              fontWeight: 'medium'
            }}
          >
            {part}
          </Typography>
        )
      }
      
      // Add the blank if it's not the last part
      if (index < parts.length - 1) {
        const blank = question.blanks[index]
        if (blank) {
          elements.push(
            <Box key={`blank-${index}`} sx={{ display: 'inline-flex', alignItems: 'center', mx: 1 }}>
              <FormControl 
                size="small" 
                sx={{ 
                  minWidth: { xs: 120, md: 140 }
                }}
                color={getSelectColor(blank.id) as any}
              >
                <Select
                  value={selectedValues[blank.id] || ''}
                  onChange={(e) => handleChange(blank.id, e)}
                  displayEmpty
                  sx={{
                    bgcolor: getBlankResult(blank.id) ? 
                      (getBlankResult(blank.id)?.isCorrect ? '#e8f5e8' : '#ffebee') : 
                      'background.paper',
                    '& .MuiSelect-select': {
                      py: 1,
                      fontSize: { xs: '0.9rem', md: '1rem' }
                    }
                  }}
                >
                  <MenuItem value="" disabled>
                    <em>Choose...</em>
                  </MenuItem>
                  {blank.options.map((option) => (
                    <MenuItem 
                      key={option} 
                      value={option}
                      sx={{ fontSize: { xs: '0.9rem', md: '1rem' } }}
                    >
                      {option}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              {getStatusIcon(blank.id)}
            </Box>
          )
        }
      }
    })
    
    return elements
  }

  return (
    <Paper 
      elevation={1}
      onClick={(e) => e.stopPropagation()}
      sx={{ 
        p: { xs: 2, md: 3 }, 
        mb: 2, 
        borderRadius: 2,
        bgcolor: 'background.default',
        border: result ? `2px solid ${result.isCorrect ? '#4caf50' : '#f44336'}` : '1px solid #e0e0e0'
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
        <Typography 
          variant="body2" 
          sx={{ 
            fontWeight: 'bold',
            color: 'primary.main',
            minWidth: '24px',
            fontSize: { xs: '0.9rem', md: '1rem' }
          }}
        >
          {questionNumber}.
        </Typography>
        
        <Box sx={{ flexGrow: 1 }}>
          {/* Farsi sentence */}
          <Typography 
            variant="body1" 
            sx={{ 
              mb: 1,
              fontStyle: 'italic',
              color: 'text.secondary',
              fontSize: { xs: '0.9rem', md: '1rem' },
              direction: 'rtl',
              textAlign: 'right'
            }}
          >
            {question.farsi}
          </Typography>
          
          {/* English sentence with multiple blanks */}
          <Box sx={{ 
            display: 'flex', 
            alignItems: 'center', 
            flexWrap: 'wrap',
            gap: 1
          }}>
            {renderEnglishWithBlanks()}
          </Box>
          
          {/* Explanation */}
          {result && question.explanation && (
            <Box sx={{ mt: 2, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
              <Typography 
                variant="body2" 
                sx={{ 
                  color: 'text.secondary',
                  fontStyle: 'italic'
                }}
              >
                💡 {question.explanation}
              </Typography>
            </Box>
          )}
        </Box>
      </Box>
    </Paper>
  )
}

export default MultipleBlankQuestionComponent
