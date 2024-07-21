import { Box, Container } from '@mui/material';
import React from 'react';
import Tab from '@mui/material/Tab';
import TabContext from '@mui/lab/TabContext';
import TabList from '@mui/lab/TabList';
import TabPanel from '@mui/lab/TabPanel';
import GeneralSettings from './General/GeneralSettings';
import WorkTrackingSystemConnectionSettings from './Connections/WorkTrackingSystemConnectionSettings';
import LogSettings from './LogSettings/LogSettings';

const Settings: React.FC = () => {
  const [value, setValue] = React.useState('1');

  const handleChange = (_event: React.SyntheticEvent, newValue: string) => {
    setValue(newValue);
  };

  return (
    <Container>
      <TabContext value={value}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <TabList onChange={handleChange} >
            <Tab label="General" value="1" />
            <Tab label="Work Tracking Systems" value="2" />
            <Tab label="Logs" value="3" />
          </TabList>
        </Box>
        <TabPanel value="1">
          <GeneralSettings />
        </TabPanel>
        <TabPanel value="2">
          <WorkTrackingSystemConnectionSettings />
        </TabPanel>
        <TabPanel value="3">
          <LogSettings />
        </TabPanel>
      </TabContext>
    </Container>
  );
};

export default Settings;
