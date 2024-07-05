import React from 'react';
import { Box, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

const Copyright: React.FC = () => {
  return (
    <Box
      component={Link}
      to="https://letpeople.work"
    >
      <Typography variant="body2" color="textPrimary">
        &copy; {new Date().getFullYear()} - <span style={{ color: 'rgba(48, 87, 78, 1)' }}>Let People</span> Work
      </Typography>
    </Box>
  );
}

export default Copyright;
