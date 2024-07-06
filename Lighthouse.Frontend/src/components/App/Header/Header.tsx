import React from 'react';
import { AppBar, Toolbar, Box, IconButton } from '@mui/material';
import LighthouseLogo from '../LetPeopleWork/LighthouseLogo';
import { Link, NavLink } from 'react-router-dom';
import { BugReport } from '@mui/icons-material';
import GitHubIcon from '@mui/icons-material/GitHub';
import YouTubeIcon from '@mui/icons-material/YouTube';
import BlogIcon from '@mui/icons-material/RssFeed';

const Header: React.FC = () => {
  return (
    <AppBar position="static" className="header" style={{ backgroundColor: 'white' }}>
      <Toolbar className="toolbar">
        <Box className="logo">
          <LighthouseLogo />
        </Box>
        <Box className="nav-links">
          <NavLink to="/" className={({isActive}) => (isActive ? 'nav-link active': 'nav-link')}>Overview</NavLink>
          <NavLink to="/teams" className={({isActive}) => (isActive ? 'nav-link active': 'nav-link')}>Teams</NavLink>
          <NavLink to="/projects" className={({isActive}) => (isActive ? 'nav-link active': 'nav-link')}>Projects</NavLink>
          <NavLink to="/settings" className={({isActive}) => (isActive ? 'nav-link active': 'nav-link')}>Settings</NavLink>
        </Box>
        <Box sx={{ display: { xs: 'none', md: 'flex' } }}>
          <IconButton
            size="large"
            color="inherit"
            component={Link}
            to={"https://github.com/LetPeopleWork/Lighthouse/issues"} >
            <BugReport style={{ color: 'rgba(48, 87, 78, 1)' }} />
          </IconButton>
          <IconButton
            size="large"
            color="inherit"
            component={Link}
            to={"https://www.youtube.com/channel/UCipDDn2dpVE3rpoKNW2asZQ"} >
            <YouTubeIcon style={{ color: 'rgba(48, 87, 78, 1)' }} />
          </IconButton>
          <IconButton
            size="large"
            color="inherit"
            component={Link}
            to={"https://www.letpeople.work/blog/"} >
            <BlogIcon style={{ color: 'rgba(48, 87, 78, 1)' }} />
          </IconButton>
          <IconButton
            size="large"
            color="inherit"
            component={Link}
            to={"https://github.com/LetPeopleWork/"} >
            <GitHubIcon style={{ color: 'rgba(48, 87, 78, 1)' }} />
          </IconButton>
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default Header;
