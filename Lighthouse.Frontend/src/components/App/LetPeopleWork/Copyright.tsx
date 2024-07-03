import React from 'react';
import { Box, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

const Copyright: React.FC = () => {
  const currentYear = new Date().getFullYear();

  return (
    <Box
      component={Link}
      to="/"
      sx={{
        display: 'flex',
        alignItems: 'center',
        textDecoration: 'none',
        fontFamily: 'Quicksand, sans-serif',
        color: 'black',
        fontWeight: 'bold',
      }}
    >
      <Typography variant="body2" color="textPrimary">
        &copy; {new Date().getFullYear()} - <span style={{ color: 'rgba(48, 87, 78, 1)' }}>Let People</span> Work
      </Typography>
    </Box>
  );
}

export default Copyright;
