// Header.tsx
import React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faYoutube, faGithub } from '@fortawesome/free-brands-svg-icons';
import { faBug } from '@fortawesome/free-solid-svg-icons';

const Header: React.FC = () => {
  return (
    <header className="header">
      <div className="logo">Lighthouse</div>
      <div className="links">
        <FontAwesomeIcon icon={faBug} />
        <FontAwesomeIcon icon={faYoutube} />
        <FontAwesomeIcon icon={faGithub} />
        {/* Add links and appropriate styles */}
      </div>
    </header>
  );
};

export default Header;
