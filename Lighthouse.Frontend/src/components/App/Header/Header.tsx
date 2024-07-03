import React from 'react';
import { AppBar, Toolbar, Box } from '@mui/material';
import HeaderItem from './HeaderItem';
import GitHubIcon from '@mui/icons-material/GitHub';
import YouTubeIcon from '@mui/icons-material/YouTube';
import BlogIcon from '@mui/icons-material/RssFeed';
import LighthouseLogo from '../LetPeopleWork/LighthouseLogo';
import { BugReport } from '@mui/icons-material';
import { Link } from 'react-router-dom';

const Header: React.FC = () => {
  return (
    <AppBar position="static" sx={{ backgroundColor: 'white' }}>
      <Toolbar>
        <Box sx={{ flexGrow: 1 }}>
          <Link to="/">
            <LighthouseLogo />
          </Link>
        </Box>
        <Box className="nav-links">
          <HeaderItem link="https://github.com/LetPeopleWork/Lighthouse/issues" icon={<BugReport sx={{ color: 'rgba(48, 87, 78, 1)' }} />} />
          <HeaderItem link="https://www.youtube.com/channel/UCipDDn2dpVE3rpoKNW2asZQ" icon={<YouTubeIcon sx={{ color: 'rgba(48, 87, 78, 1)' }} />} />
          <HeaderItem link="https://www.letpeople.work/blog" icon={<BlogIcon sx={{ color: 'rgba(48, 87, 78, 1)' }} />} />
          <HeaderItem link="https://github.com/LetPeopleWork/" icon={<GitHubIcon sx={{ color: 'rgba(48, 87, 78, 1)' }} />} />
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default Header;
