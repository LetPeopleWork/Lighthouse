import { Box, Container } from '@mui/material';
import React from 'react';
import Tab from '@mui/material/Tab';
import TabContext from '@mui/lab/TabContext';
import TabList from '@mui/lab/TabList';
import TabPanel from '@mui/lab/TabPanel';
import GeneralSettings from './General/GeneralSettings';
import ConnectionSettings from './Connections/ConnectionSettings';

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
            <Tab label="Connections" value="2" />
          </TabList>
        </Box>
        <TabPanel value="1">
          <GeneralSettings />
        </TabPanel>
        <TabPanel value="2">
          <ConnectionSettings />
        </TabPanel>
      </TabContext>
    </Container>
  );
};

export default Settings;
