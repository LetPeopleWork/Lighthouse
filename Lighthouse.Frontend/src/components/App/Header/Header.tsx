// Header.tsx
import React from 'react';
import { faYoutube, faGithub, faBlogger } from '@fortawesome/free-brands-svg-icons';
import { faBug } from '@fortawesome/free-solid-svg-icons';
import LighthouseLogo from '../LetPeopleWork/LighthouseLogo';
import HeaderItem from './HeaderItem';

const Header: React.FC = () => {
  return (
    <header>
      <LighthouseLogo />      
      <HeaderItem link="https://github.com/LetPeopleWork/Lighthouse/issues" icon={faBug} />
      <HeaderItem link="https://www.youtube.com/channel/UCipDDn2dpVE3rpoKNW2asZQ" icon={faYoutube} />
      <HeaderItem link="https://www.letpeople.work/blog" icon={faBlogger} />
      <HeaderItem link="https://github.com/LetPeopleWork/" icon={faGithub} />      
      
    </header>
  );
};

export default Header;
