import { useState } from 'react'
import {
  Box,
  Typography,
  Paper,
  Chip,
  Button
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faTimes, faUndo } from '@fortawesome/free-solid-svg-icons'
import type { WordOrderQuestion, QuestionResult } from '../../types/QuestionTypes'

interface WordOrderQuestionProps {
  question: WordOrderQuestion
  questionNumber: number
  result?: QuestionResult
  onAnswerSelect: (questionId: string, selectedOrder: string[], isCorrect: boolean) => void
}

const WordOrderQuestionComponent = ({ 
  question, 
  questionNumber, 
  result, 
  onAnswerSelect 
}: WordOrderQuestionProps) => {
  const [selectedWords, setSelectedWords] = useState<string[]>(
    result?.userAnswer || []
  )
  const [availableWords, setAvailableWords] = useState<string[]>(
    result ? [] : [...question.shuffledWords]
  )

  const handleWordClick = (word: string, fromAvailable: boolean) => {
    if (fromAvailable) {
      // Move word from available to selected
      setAvailableWords(prev => prev.filter(w => w !== word))
      setSelectedWords(prev => [...prev, word])
    } else {
      // Move word from selected back to available
      setSelectedWords(prev => prev.filter(w => w !== word))
      setAvailableWords(prev => [...prev, word])
    }
  }

  const handleReset = () => {
    setSelectedWords([])
    setAvailableWords([...question.shuffledWords])
  }

  const handleSubmit = () => {
    const isCorrect = JSON.stringify(selectedWords) === JSON.stringify(question.correctOrder)
    onAnswerSelect(question.id, selectedWords, isCorrect)
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

  const isSubmitted = result !== undefined

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
          
          {/* Instructions */}
          <Typography 
            variant="body2" 
            sx={{ 
              mb: 2,
              color: 'text.secondary',
              fontSize: { xs: '0.9rem', md: '1rem' }
            }}
          >
            Arrange the words to form a correct English sentence:
          </Typography>
          
          {/* Selected words (sentence being built) */}
          <Box sx={{ 
            mb: 2, 
            p: 2, 
            bgcolor: 'grey.50', 
            borderRadius: 1,
            minHeight: '60px',
            display: 'flex',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: 1
          }}>
            {selectedWords.length === 0 ? (
              <Typography 
                variant="body2" 
                sx={{ 
                  color: 'text.secondary',
                  fontStyle: 'italic'
                }}
              >
                Click words below to build your sentence...
              </Typography>
            ) : (
              selectedWords.map((word, index) => (
                <Chip
                  key={`selected-${index}`}
                  label={word}
                  onClick={() => !isSubmitted && handleWordClick(word, false)}
                  sx={{
                    cursor: isSubmitted ? 'default' : 'pointer',
                    bgcolor: 'primary.main',
                    color: 'white',
                    '&:hover': isSubmitted ? {} : { bgcolor: 'primary.dark' }
                  }}
                />
              ))
            )}
          </Box>
          
          {/* Available words */}
          <Box sx={{ 
            mb: 2,
            display: 'flex',
            flexWrap: 'wrap',
            gap: 1
          }}>
            {availableWords.map((word, index) => (
              <Chip
                key={`available-${index}`}
                label={word}
                onClick={() => !isSubmitted && handleWordClick(word, true)}
                variant="outlined"
                sx={{
                  cursor: isSubmitted ? 'default' : 'pointer',
                  '&:hover': isSubmitted ? {} : { bgcolor: 'primary.light', color: 'white' }
                }}
              />
            ))}
          </Box>
          
          {/* Action buttons */}
          {!isSubmitted && (
            <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
              <Button
                variant="contained"
                onClick={handleSubmit}
                disabled={selectedWords.length === 0}
                sx={{ fontSize: { xs: '0.9rem', md: '1rem' } }}
              >
                Check Answer
              </Button>
              <Button
                variant="outlined"
                onClick={handleReset}
                startIcon={<FontAwesomeIcon icon={faUndo} />}
                sx={{ fontSize: { xs: '0.9rem', md: '1rem' } }}
              >
                Reset
              </Button>
            </Box>
          )}
          
          {/* Status icon */}
          {getStatusIcon()}
          
          {/* Correct answer (shown after submission) */}
          {result && !result.isCorrect && (
            <Box sx={{ 
              mt: 2, 
              p: 2, 
              bgcolor: '#fff3e0', 
              borderRadius: 1,
              border: '1px solid #ffb74d'
            }}>
              <Typography 
                variant="body2" 
                sx={{ 
                  color: 'text.secondary',
                  mb: 1,
                  fontWeight: 'bold'
                }}
              >
                Correct answer:
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {question.correctOrder.map((word, index) => (
                  <Chip
                    key={`correct-${index}`}
                    label={word}
                    sx={{
                      bgcolor: '#4caf50',
                      color: 'white'
                    }}
                  />
                ))}
              </Box>
            </Box>
          )}
          
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

export default WordOrderQuestionComponent
