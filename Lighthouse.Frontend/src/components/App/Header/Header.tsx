import React from 'react';
import { AppBar, Toolbar, Box, Button, IconButton } from '@mui/material';
import LighthouseLogo from '../LetPeopleWork/LighthouseLogo';
import { Link } from 'react-router-dom';
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
          <Button component={Link} to="/" className="nav-link">Overview</Button>
          <Button component={Link} to="/teams" className="nav-link">Teams</Button>
          <Button component={Link} to="/projects" className="nav-link">Projects</Button>
          <Button component={Link} to="/settings" className="nav-link">Settings</Button>
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
