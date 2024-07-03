import React from 'react';
import { IconButton } from '@mui/material';

interface HeaderItemProps {
  link: string;
  icon: React.ReactElement;
}

const HeaderItem: React.FC<HeaderItemProps> = ({ link, icon }) => {
  return (
    <IconButton href={link} target="_blank" rel="noopener noreferrer" color="inherit">
      {icon}
    </IconButton>
  );
};

export default HeaderItem;
