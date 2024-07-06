import React from 'react';
import { Box, Container, Grid } from '@mui/material';
import LetPeopleWorkLogo from '../LetPeopleWork/LetPeopleWorkLogo';
import LighthouseVersion from '../LetPeopleWork/LighthouseVersion';

const Footer: React.FC = () => {
  return (
    <Box component="footer" className="footer" sx={{ backgroundColor: 'white' }}>
      <Container>
        <Grid container justifyContent="space-between" alignItems="center" className="footer-content">
          <Grid item xs={6} md={10}>
            <LetPeopleWorkLogo/>
          </Grid>
          <Grid item xs={6} md={2}>
            <LighthouseVersion />
          </Grid>
        </Grid>
      </Container>
    </Box>
  );
};

export default Footer;