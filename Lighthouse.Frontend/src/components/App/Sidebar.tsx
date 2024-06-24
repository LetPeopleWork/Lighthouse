import React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faChartPie, faUsers, faProjectDiagram, faCog } from '@fortawesome/free-solid-svg-icons';

const Sidebar: React.FC = () => {
  return (
    <aside className="sidebar">
      <div className="nav-item">
        <FontAwesomeIcon icon={faChartPie} />
        <span className="text">Overview</span>
      </div>
      <div className="nav-item">
        <FontAwesomeIcon icon={faUsers} />
        <span className="text">Teams</span>
      </div>
      <div className="nav-item">
        <FontAwesomeIcon icon={faProjectDiagram} />
        <span className="text">Projects</span>
      </div>
      <div className="nav-item">
        <FontAwesomeIcon icon={faCog} />
        <span className="text">Settings</span>
      </div>
    </aside>
  );
};

export default Sidebar;
