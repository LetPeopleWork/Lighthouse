import React, { useState, useEffect } from 'react';
import { Box, Typography, Paper } from '@mui/material';

interface TutorialStepProps {
  title: string;
  description: string;
  imageSrc?: string;
  children: React.ReactNode;
}

const TutorialStep: React.FC<TutorialStepProps> = ({ title, description, imageSrc, children }) => {
  const [imageDimensions, setImageDimensions] = useState<{ width: number, height: number }>({ width: 0, height: 0 });

  useEffect(() => {
    if (imageSrc) {
      const img = new Image();
      img.src = imageSrc;
      img.onload = () => {
        setImageDimensions({ width: img.width, height: img.height });
      };
    }
  }, [imageSrc]);

  const aspectRatio = imageDimensions.width / imageDimensions.height;

  return (
    <Paper elevation={3} sx={{ padding: 2, margin: 2 }}>
      <Typography variant="h5" gutterBottom align="center">
        {title}
      </Typography>
      <Typography variant="subtitle1" fontWeight={'bold'} gutterBottom align="center" sx={{ whiteSpace: 'pre-line' }}>
        {description}
      </Typography>
      {imageSrc && imageDimensions.width > 0 && (
        <Box
          component="img"
          sx={{
            width: '100%',
            height: 'auto',
            maxWidth: '100%',
            objectFit: 'contain',
            aspectRatio: `${aspectRatio}`,
            maxHeight: `${imageDimensions.height}px`,
            borderRadius: 1,
            boxShadow: 1,
            marginTop: 2,
          }}
          src={imageSrc}
          alt="Tutorial step image"
        />
      )}
      <Box sx={{ marginTop: 2 }}>
        {children}
      </Box>
    </Paper>
  );
};

export default TutorialStep;
