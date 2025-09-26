import { useState } from 'react'
import {
  Card,
  CardContent,
  Typography,
  Collapse,
  IconButton,
  Box,
  Chip
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faMinus, faPlay } from '@fortawesome/free-solid-svg-icons'
import type { CourseData, QuestionResult } from '../types/QuestionTypes'
import { QuestionType } from '../types/QuestionTypes'
import QuestionRenderer from './questions/QuestionRenderer'

interface CourseBoxProps {
  course: CourseData
}

const CourseBox = ({ course }: CourseBoxProps) => {
  const [expanded, setExpanded] = useState(false)
  const [questionResults, setQuestionResults] = useState<QuestionResult[]>([])

  const handleToggle = (event: React.MouseEvent) => {
    // Don't toggle if clicking on interactive elements
    const target = event.target as HTMLElement
    if (target.closest('.MuiSelect-root') || 
        target.closest('.MuiMenuItem-root') || 
        target.closest('.MuiChip-root') ||
        target.closest('.MuiButton-root') ||
        target.closest('.MuiIconButton-root')) {
      return
    }
    
    if (course.isActive) {
      setExpanded(!expanded)
    }
  }

  const handleAnswerSelect = (questionId: string, userAnswer: any, isCorrect: boolean) => {
    setQuestionResults(prev => {
      const existing = prev.findIndex(result => result.questionId === questionId)
      const newResult: QuestionResult = { 
        questionId, 
        type: course.questions.find(q => q.id === questionId)?.type || QuestionType.FILL_BLANK,
        userAnswer, 
        isCorrect 
      }
      
      if (existing >= 0) {
        const updated = [...prev]
        updated[existing] = newResult
        return updated
      } else {
        return [...prev, newResult]
      }
    })
  }

  const getQuestionResult = (questionId: string) => {
    return questionResults.find(result => result.questionId === questionId)
  }

  return (
    <Card 
      elevation={3}
      sx={{ 
        borderRadius: 2,
        opacity: course.isActive ? 1 : 0.7,
        cursor: course.isActive ? 'pointer' : 'default',
        transition: 'all 0.3s ease',
        '&:hover': course.isActive ? {
          elevation: 6,
          transform: 'translateY(-2px)'
        } : {},
        border: expanded ? '2px solid' : '1px solid',
        borderColor: expanded ? 'primary.main' : 'divider'
      }}
    >
      <CardContent 
        onClick={handleToggle}
        sx={{ 
          p: { xs: 2, md: 3 },
          '&:last-child': { pb: { xs: 2, md: 3 } }
        }}
      >
        <Box sx={{ 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'space-between',
          mb: course.isActive && expanded ? 2 : 0
        }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <FontAwesomeIcon 
              icon={faPlay} 
              style={{ 
                color: course.isActive ? '#1976d2' : '#999',
                fontSize: '1.2rem'
              }} 
            />
            <Typography 
              variant="h6" 
              sx={{ 
                fontWeight: 'bold',
                fontSize: { xs: '1.1rem', md: '1.25rem' },
                color: course.isActive ? 'text.primary' : 'text.secondary'
              }}
            >
              {course.title}
            </Typography>
            {!course.isActive && (
              <Chip 
                label="Coming Soon" 
                size="small" 
                variant="outlined"
                sx={{ 
                  fontSize: '0.75rem',
                  height: '24px'
                }}
              />
            )}
          </Box>
          
          {course.isActive && (
            <IconButton 
              size="small"
              sx={{ 
                color: 'primary.main',
                '&:hover': { bgcolor: 'primary.light', color: 'white' }
              }}
            >
              <FontAwesomeIcon 
                icon={expanded ? faMinus : faPlus}
                style={{ fontSize: '1rem' }}
              />
            </IconButton>
          )}
        </Box>

        <Collapse in={expanded && course.isActive}>
          <Box sx={{ mt: 2 }}>
            {course.questions.map((question, index) => (
              <QuestionRenderer
                key={question.id}
                question={question}
                questionNumber={index + 1}
                result={getQuestionResult(question.id)}
                onAnswerSelect={handleAnswerSelect}
              />
            ))}
          </Box>
        </Collapse>
      </CardContent>
    </Card>
  )
}

export default CourseBox
