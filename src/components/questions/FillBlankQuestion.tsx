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
import type { FillBlankQuestion, QuestionResult } from '../../types/QuestionTypes'

interface FillBlankQuestionProps {
  question: FillBlankQuestion
  questionNumber: number
  result?: QuestionResult
  onAnswerSelect: (questionId: string, selectedAnswer: string, isCorrect: boolean) => void
}

const FillBlankQuestionComponent = ({ 
  question, 
  questionNumber, 
  result, 
  onAnswerSelect 
}: FillBlankQuestionProps) => {
  const [selectedValue, setSelectedValue] = useState(result?.userAnswer || '')

  const handleChange = (event: SelectChangeEvent) => {
    const selected = event.target.value
    setSelectedValue(selected)
    const isCorrect = selected === question.correctAnswer
    onAnswerSelect(question.id, selected, isCorrect)
  }

  const getStatusIcon = () => {
    if (!result) return null
    
    return (
      <Box sx={{ ml: 2, display: 'flex', alignItems: 'center' }}>
        <FontAwesomeIcon
          icon={result.isCorrect ? faCheck : faTimes}
          style={{
            color: result.isCorrect ? '#4caf50' : '#f44336',
            fontSize: '1.2rem'
          }}
        />
      </Box>
    )
  }

  const getSelectColor = () => {
    if (!result) return 'primary'
    return result.isCorrect ? 'success' : 'error'
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
          
          {/* English sentence with dropdown */}
          <Box sx={{ 
            display: 'flex', 
            alignItems: 'center', 
            flexWrap: 'wrap',
            gap: 1
          }}>
            <Typography 
              variant="body1" 
              sx={{ 
                fontSize: { xs: '1rem', md: '1.1rem' },
                fontWeight: 'medium'
              }}
            >
              {question.english.split('_____')[0]}
            </Typography>
            
            <FormControl 
              size="small" 
              sx={{ 
                minWidth: { xs: 140, md: 160 },
                mx: 1
              }}
              color={getSelectColor() as any}
            >
              <Select
                value={selectedValue}
                onChange={handleChange}
                displayEmpty
                sx={{
                  bgcolor: result ? (result.isCorrect ? '#e8f5e8' : '#ffebee') : 'background.paper',
                  '& .MuiSelect-select': {
                    py: 1,
                    fontSize: { xs: '0.9rem', md: '1rem' }
                  }
                }}
              >
                <MenuItem value="" disabled>
                  <em>Choose...</em>
                </MenuItem>
                {question.options.map((option) => (
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
            
            <Typography 
              variant="body1" 
              sx={{ 
                fontSize: { xs: '1rem', md: '1.1rem' },
                fontWeight: 'medium'
              }}
            >
              {question.english.split('_____')[1]}
            </Typography>
            
            {getStatusIcon()}
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

export default FillBlankQuestionComponent
