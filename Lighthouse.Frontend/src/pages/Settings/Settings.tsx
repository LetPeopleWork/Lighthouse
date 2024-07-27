import { Box, Container } from '@mui/material';
import React from 'react';
import Tab from '@mui/material/Tab';
import TabContext from '@mui/lab/TabContext';
import TabList from '@mui/lab/TabList';
import TabPanel from '@mui/lab/TabPanel';
import WorkTrackingSystemConnectionSettings from './Connections/WorkTrackingSystemConnectionSettings';
import LogSettings from './LogSettings/LogSettings';
import RefreshSettingsTab from './Refresh/RefreshSettingsTab';
import DefaultSettings from './DefaultSettings/DefaultSettings';

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
            <Tab label="Default Settings" value="2" data-testid="default-settings-tab" />
            <Tab label="Refresh" value="3" data-testid="refresh-tab" />
            <Tab label="Logs" value="4" data-testid="logs-tab" />
          </TabList>
        </Box>
        <TabPanel value="1" data-testid="work-tracking-panel">
          <WorkTrackingSystemConnectionSettings />
        </TabPanel>
        <TabPanel value="2" data-testid="default-settings-panel">
          <DefaultSettings />
        </TabPanel>
        <TabPanel value="3" data-testid="refresh-panel">
          <RefreshSettingsTab />
        </TabPanel>
        <TabPanel value="4" data-testid="logs-panel">
          <LogSettings />
        </TabPanel>
      </TabContext>
    </Container>
  );
};

export default Settings;
