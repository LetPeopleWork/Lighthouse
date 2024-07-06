import React from 'react';
import { Button } from '@mui/material';
import { Link } from 'react-router-dom';
import logo from '../../../assets/LetPeopleWorkLogo.png'

const LetPeopleWorkLogo: React.FC = () => {
  return (
    
    <Button component={Link} to={`https://letpeople.work`} className="nav-link">
      <img src={logo} alt='Let People Work Logo' width={70} />
    </Button>
  );
}

export default LetPeopleWorkLogo;
