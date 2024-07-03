import React from 'react';
import { Drawer, List, ListItem, ListItemIcon, ListItemText, Divider } from '@mui/material';
import { Link } from 'react-router-dom';
import HomeIcon from '@mui/icons-material/Home';
import GroupIcon from '@mui/icons-material/Group';
import WorkIcon from '@mui/icons-material/Work';
import SettingsIcon from '@mui/icons-material/Settings';

const Sidebar: React.FC = () => {
  return (
    <Drawer variant="permanent" className="sidebar">
      <List>
        <ListItem component={Link} to="/" className="sidebar-item">
          <ListItemIcon><HomeIcon style={{ color: 'rgba(48, 87, 78, 1)' }} /></ListItemIcon>
          <ListItemText primary="Overview" />
        </ListItem>
        <ListItem component={Link} to="/teams" className="sidebar-item">
          <ListItemIcon><GroupIcon style={{ color: 'rgba(48, 87, 78, 1)' }} /></ListItemIcon>
          <ListItemText primary="Teams" />
        </ListItem>
        <ListItem component={Link} to="/projects" className="sidebar-item">
          <ListItemIcon><WorkIcon style={{ color: 'rgba(48, 87, 78, 1)' }} /></ListItemIcon>
          <ListItemText primary="Projects" />
        </ListItem>
        <ListItem component={Link} to="/settings" className="sidebar-item">
          <ListItemIcon><SettingsIcon style={{ color: 'rgba(48, 87, 78, 1)' }} /></ListItemIcon>
          <ListItemText primary="Settings" />
        </ListItem>
      </List>
      <Divider />
    </Drawer>
  );
};

export default Sidebar;
