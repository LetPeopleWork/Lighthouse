import React from 'react';
import CellTowerIcon from '@mui/icons-material/CellTower';
import { Typography } from '@mui/material';

const LighthouseLogo: React.FC = () => {
  return (
    <>
      <CellTowerIcon sx={{ display: { xs: 'none', md: 'flex' }, mr: 1, color: 'black' }} />
      <Typography style={{ fontFamily: 'Quicksand, sans-serif', color: 'rgba(48, 87, 78, 1)', fontWeight: 'bold' }}>
        Light
      </Typography>
      <Typography style={{ fontFamily: 'Quicksand, sans-serif', color: 'black', fontWeight: 'bold' }}>
        house
      </Typography>
      </>
  );
};

export default LighthouseLogo;
