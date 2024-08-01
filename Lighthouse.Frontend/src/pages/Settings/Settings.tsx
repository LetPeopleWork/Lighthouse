import { Box, Container } from '@mui/material';
import React from 'react';
import Tab from '@mui/material/Tab';
import TabContext from '@mui/lab/TabContext';
import TabList from '@mui/lab/TabList';
import TabPanel from '@mui/lab/TabPanel';
import WorkTrackingSystemConnectionSettings from './Connections/WorkTrackingSystemConnectionSettings';
import LogSettings from './LogSettings/LogSettings';
import RefreshSettingsTab from './Refresh/RefreshSettingsTab';
import DefaultTeamSettings from './DefaultTeamSettings/DefaultTeamSettings';
import DefaultProjectSettings from './DefaultProjectSettings/DefaultProjectSettings';

const Settings: React.FC = () => {
  const [value, setValue] = React.useState('1');

  const handleChange = (_event: React.SyntheticEvent, newValue: string) => {
    setValue(newValue);
  };

  return (
    <Container>
      <TabContext value={value}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <TabList onChange={handleChange}>
            <Tab label="Work Tracking Systems" value="1" data-testid="work-tracking-tab" />
            <Tab label="Default Team Settings" value="2" data-testid="default-team-settings-tab" />
            <Tab label="Default Project Settings" value="3" data-testid="default-project-settings-tab" />
            <Tab label="Periodic Refresh Settings" value="4" data-testid="periodic-refresh-settings-tab" />
            <Tab label="Logs" value="99" data-testid="logs-tab" />
          </TabList>
        </Box>
        <TabPanel value="1" data-testid="work-tracking-panel">
          <WorkTrackingSystemConnectionSettings />
        </TabPanel>
        <TabPanel value="2" data-testid="default-team-settings-panel">
          <DefaultTeamSettings />
        </TabPanel>
        <TabPanel value="3" data-testid="default-project-settings-panel">
          <DefaultProjectSettings />
        </TabPanel>
        <TabPanel value="4" data-testid="periodic-refresh-settings-panel">
          <RefreshSettingsTab />
        </TabPanel>
        <TabPanel value="99" data-testid="logs-panel">
          <LogSettings />
        </TabPanel>
      </TabContext>
    </Container>
  );
};

export default Settings;
