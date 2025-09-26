import { Box, Typography, Paper } from '@mui/material'

const AboutPage = () => {
  return (
    <Box sx={{ maxWidth: 800, mx: 'auto', px: { xs: 2, md: 0 } }}>
      <Typography 
        variant="h3" 
        component="h1" 
        gutterBottom 
        sx={{ 
          textAlign: 'center',
          fontSize: { xs: '2rem', md: '3rem' },
          mb: 4
        }}
      >
        About Me
      </Typography>
      
      <Paper 
        elevation={3} 
        sx={{ 
          p: { xs: 3, md: 4 },
          borderRadius: 2,
          bgcolor: 'background.paper'
        }}
      >
        <Typography 
          variant="body1" 
          sx={{ 
            fontSize: { xs: '1rem', md: '1.1rem' },
            lineHeight: 1.8,
            textAlign: 'center',
            color: 'text.secondary'
          }}
        >
          Welcome to my English learning platform! More information about me and this podcast will be available here soon.
        </Typography>
      </Paper>
    </Box>
  )
}

export default AboutPage
