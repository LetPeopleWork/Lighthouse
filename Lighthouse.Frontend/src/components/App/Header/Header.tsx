import React from 'react';
import { AppBar, Toolbar, Box } from '@mui/material';
import LighthouseLogo from '../LetPeopleWork/LighthouseLogo';
import NavigationItem from './NavigationItem';
import ExternalLinkButton from './ExternalLinkButton';
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
          <NavigationItem path='/' text='Overview' />
          <NavigationItem path='/teams' text='Teams' />
          <NavigationItem path='/projects' text='Projects' />
          <NavigationItem path='/settings' text='Settings' />
          <NavigationItem path='/tutorial' text='Tutorial' />
        </Box>
        <Box sx={{ display: { xs: 'none', md: 'flex' } }}>
          <ExternalLinkButton
            link="https://github.com/LetPeopleWork/Lighthouse/issues"
            icon={BugReport}
          />
          <ExternalLinkButton
            link="https://www.youtube.com/channel/UCipDDn2dpVE3rpoKNW2asZQ"
            icon={YouTubeIcon}
          />
          <ExternalLinkButton
            link="https://www.letpeople.work/blog/"
            icon={BlogIcon}
          />
          <ExternalLinkButton
            link="https://github.com/LetPeopleWork/"
            icon={GitHubIcon}
          />
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default Header;
