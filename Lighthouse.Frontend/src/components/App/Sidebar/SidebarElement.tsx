import React from 'react';
import { ListItem, ListItemText } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';

interface SidebarElementProps {
    text: string;
    path: string;
}

const SidebarElement: React.FC<SidebarElementProps> = ({ text, path }) => {
    return (
        <ListItem component={RouterLink} to={path}>
          <ListItemText primary={text} />
        </ListItem>
      );
};

export default SidebarElement;