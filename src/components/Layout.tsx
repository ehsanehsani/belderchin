import type { ReactNode } from 'react'
import { AppBar, Toolbar, Typography, Container, Box, Button, useTheme } from '@mui/material'
import { Link, useLocation } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHeadphones } from '@fortawesome/free-solid-svg-icons'

interface LayoutProps {
  children: ReactNode
}

const Layout = ({ children }: LayoutProps) => {
  const location = useLocation()
  const theme = useTheme()

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static" elevation={1}>
        <Container maxWidth="lg">
          <Toolbar sx={{ px: { xs: 0 } }}>
            <FontAwesomeIcon 
              icon={faHeadphones} 
              style={{ 
                marginRight: theme.spacing(1), 
                fontSize: '1.5rem',
                color: theme.palette.common.white
              }} 
            />
            <Typography
              variant="h6"
              component={Link}
              to="/"
              sx={{
                flexGrow: 1,
                textDecoration: 'none',
                color: 'inherit',
                fontWeight: 'bold',
                fontSize: { xs: '1.1rem', md: '1.25rem' }
              }}
            >
              English Learning Podcast
            </Typography>
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button
                color="inherit"
                component={Link}
                to="/"
                sx={{
                  textDecoration: location.pathname === '/' ? 'underline' : 'none',
                  fontSize: { xs: '0.9rem', md: '1rem' }
                }}
              >
                Home
              </Button>
              <Button
                color="inherit"
                component={Link}
                to="/about"
                sx={{
                  textDecoration: location.pathname === '/about' ? 'underline' : 'none',
                  fontSize: { xs: '0.9rem', md: '1rem' }
                }}
              >
                About
              </Button>
            </Box>
          </Toolbar>
        </Container>
      </AppBar>
      
      <Box component="main" sx={{ flexGrow: 1, bgcolor: 'background.default' }}>
        {children}
      </Box>
      
      <Box
        component="footer"
        sx={{
          bgcolor: 'primary.dark',
          color: 'white',
          py: 2,
          mt: 'auto'
        }}
      >
        <Container maxWidth="lg">
          <Typography variant="body2" align="center">
            © 2024 English Learning Podcast. All rights reserved.
          </Typography>
        </Container>
      </Box>
    </Box>
  )
}

export default Layout
