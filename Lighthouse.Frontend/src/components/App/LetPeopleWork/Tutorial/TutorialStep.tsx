import React from 'react';
import { Box, Typography, Grid, Paper } from '@mui/material';

interface TutorialStepProps {
  title: string;
  description: string;
  imageSrc?: string;
  children: React.ReactNode;
}

const TutorialStep: React.FC<TutorialStepProps> = ({ title, description, imageSrc, children }) => {
  return (
    <Paper elevation={3} sx={{ padding: 2, margin: 2 }}>
      <Typography variant="h5" gutterBottom>
        {title}
      </Typography>
      <Typography variant="subtitle1" gutterBottom>
        {description}
      </Typography>
      <Grid container spacing={2}>
        <Grid item xs={12} md={imageSrc ? 6 : 12}>
          {children}
        </Grid>
        {imageSrc && (
          <Grid item xs={12} md={6}>
            <Box
              component="img"
              sx={{
                width: '100%',
                height: 'auto',
                maxHeight: 300,
                borderRadius: 1,
                boxShadow: 1,
              }}
              src={imageSrc}
              alt="Tutorial step image"
            />
          </Grid>
        )}
      </Grid>
    </Paper>
  );
};

export default TutorialStep;