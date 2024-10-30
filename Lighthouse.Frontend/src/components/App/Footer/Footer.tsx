import React from 'react';
import { Box, Container, Typography } from '@mui/material';
import LetPeopleWorkLogo from '../LetPeopleWork/LetPeopleWorkLogo';
import LighthouseVersion from '../LetPeopleWork/LighthouseVersion';

import GitHubIcon from '@mui/icons-material/GitHub';
import EmailIcon from '@mui/icons-material/Email';
import LinkedInIcon from '@mui/icons-material/LinkedIn';
import ExternalLinkButton from '../Header/ExternalLinkButton';
import CallIcon from '@mui/icons-material/Call';

const Footer: React.FC = () => {
  return (
    <Box component="footer" className="footer" sx={{ backgroundColor: 'white', py: 2 }}>
      <Container>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box>
            <LetPeopleWorkLogo />
          </Box>

          <Box textAlign="center">
            <Typography variant="body2">Contact us:</Typography>
            <Box>
              <ExternalLinkButton
                link="mailto:contact@letpeople.work"
                icon={EmailIcon}
                tooltip='Send an Email'
              />
              <ExternalLinkButton
                link="https://calendly.com/letpeoplework/"
                icon={CallIcon}
                tooltip='Schedule a Call'
              />
              <ExternalLinkButton
                link="https://www.linkedin.com/company/let-people-work/?viewAsMember=true"
                icon={LinkedInIcon}
                tooltip='View our LinkedIn Page'
              />
              <ExternalLinkButton
                link="https://github.com/LetPeopleWork/Lighthouse/issues"
                icon={GitHubIcon}
                tooltip='Raise an Issue on GitHub'
              />
            </Box>
          </Box>

          <Box textAlign="right">
            <LighthouseVersion />
          </Box>
        </Box>
      </Container>
    </Box>
  );
};

export default Footer;
