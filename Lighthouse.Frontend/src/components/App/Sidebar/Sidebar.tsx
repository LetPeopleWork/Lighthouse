import React from 'react';
import { faChartPie, faUsers, faProjectDiagram, faCog } from '@fortawesome/free-solid-svg-icons';
import SidebarElement from './SidebarElement';

const Sidebar: React.FC = () => {
  return (
    <aside>
      <SidebarElement text='Overview' link='/Overview' icon={faChartPie}  />
      <SidebarElement text='Teams' link='/Teams' icon={faUsers}  />
      <SidebarElement text='Projects' link='/Overview' icon={faProjectDiagram}  />
      <SidebarElement text='Settings' link='/Settings' icon={faCog}  />
    </aside>
  );
};

export default Sidebar;
