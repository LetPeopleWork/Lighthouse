import React from 'react';

const Footer: React.FC = () => {
  const currentYear = new Date().getFullYear();
  const version = '1.0.0 DEV'; // Replace with your actual version

  return (
    <footer className="footer">
      <div className="left">© {currentYear} - Let People Work</div>
      <div className="right">Version: {version}</div>
    </footer>
  );
};

export default Footer;
