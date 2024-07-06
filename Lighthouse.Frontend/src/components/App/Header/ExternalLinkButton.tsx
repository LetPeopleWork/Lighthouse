import React from 'react';
import IconButton from '@mui/material/IconButton';
import { SvgIconComponent } from '@mui/icons-material';

interface ExternalLinkButtonProps {
  link: string;
  icon: SvgIconComponent;
}

const ExternalLinkButton: React.FC<ExternalLinkButtonProps> = ({ link, icon: Icon }) => {
  return (
    <IconButton
      size="large"
      color="inherit"
      component="a"
      href={link}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={link}
      data-testid={link}
    >
      <Icon style={{ color: 'rgba(48, 87, 78, 1)' }} />
    </IconButton>
  );
};

export default ExternalLinkButton;
