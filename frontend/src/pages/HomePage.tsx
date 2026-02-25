import { useState, useEffect } from 'react'
import { Box, Typography, CircularProgress, Alert } from '@mui/material'
import CourseBox from '../components/CourseBox'
import { QuestionService } from '../services/QuestionService'
import type { CourseData } from '../types/QuestionTypes'

const HomePage = () => {
  const [courses, setCourses] = useState<CourseData[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadCourses = async () => {
      try {
        setLoading(true)
        const loadedCourses = await QuestionService.loadQuestions()
        setCourses(loadedCourses)
      } catch (err) {
        setError('Failed to load courses. Please try again later.')
        console.error('Error loading courses:', err)
      } finally {
        setLoading(false)
      }
    }

    loadCourses()
  }, [])

  if (loading) {
    return (
      <Box sx={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        minHeight: '50vh' 
      }}>
        <CircularProgress size={60} />
      </Box>
    )
  }

  if (error) {
    return (
      <Box sx={{ maxWidth: 900, mx: 'auto', px: { xs: 2, md: 0 } }}>
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      </Box>
    )
  }

  return (
    <Box sx={{ maxWidth: 900, mx: 'auto', px: { xs: 2, md: 0 } }}>
      <Typography 
        variant="h3" 
        component="h1" 
        gutterBottom 
        sx={{ 
          textAlign: 'center',
          fontSize: { xs: '2rem', md: '3rem' },
          mb: { xs: 3, md: 4 },
          fontWeight: 'bold',
          color: 'primary.main'
        }}
      >
        English Learning Courses
      </Typography>
      
      <Typography 
        variant="h6" 
        sx={{ 
          textAlign: 'center',
          mb: { xs: 4, md: 6 },
          color: 'text.secondary',
          fontSize: { xs: '1rem', md: '1.25rem' }
        }}
      >
        Practice English grammar with exercises based on podcast episodes
      </Typography>
      
      <Box sx={{ 
        display: 'flex', 
        flexDirection: 'column', 
        gap: { xs: 2, md: 3 }
      }}>
        {courses.map((course) => (
          <CourseBox 
            key={course.id} 
            course={course} 
          />
        ))}
      </Box>
    </Box>
  )
}

export default HomePage
