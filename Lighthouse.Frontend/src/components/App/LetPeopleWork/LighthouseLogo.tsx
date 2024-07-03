import React from 'react';
import { Box } from '@mui/material';
import { Link } from 'react-router-dom';

const LighthouseLogo: React.FC = () => {
  return (
    <Box
      component={Link}
      to="/"
      sx={{
        display: 'flex',
        alignItems: 'center',
        textDecoration: 'none',
      }}
    >
      <span style={{ fontFamily: 'Quicksand, sans-serif', color: 'rgba(48, 87, 78, 1)', fontWeight: 'bold' }}>
        Light
      </span>
      <span style={{ fontFamily: 'Quicksand, sans-serif', color: 'black', fontWeight: 'bold' }}>
        house
      </span>
    </Box>
  );
};

export default LighthouseLogo;
