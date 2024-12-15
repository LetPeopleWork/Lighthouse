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
import TutorialButton from '../../components/App/LetPeopleWork/Tutorial/TutorialButton';
import SettingsTutorial from '../../components/App/LetPeopleWork/Tutorial/Tutorials/SettingsTutorial';
import PreviewFeaturesTab from './PreviewFeatures/PreviewFeaturesTab';

const Settings: React.FC = () => {
  const [value, setValue] = React.useState('1');

  const handleChange = (_event: React.SyntheticEvent, newValue: string) => {
    setValue(newValue);
  };

  return (
    <Container maxWidth={false}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <TabContext value={value}>
          <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
            <TabList onChange={handleChange}>
              <Tab label="Work Tracking Systems" value="1" data-testid="work-tracking-tab" />
              <Tab label="Default Team Settings" value="2" data-testid="default-team-settings-tab" />
              <Tab label="Default Project Settings" value="3" data-testid="default-project-settings-tab" />
              <Tab label="Periodic Refresh Settings" value="4" data-testid="periodic-refresh-settings-tab" />
              <Tab label="Preview Features" value="5" data-testid="preview-features-tab" />
              <Tab label="Logs" value="99" data-testid="logs-tab" />
            </TabList>
          </Box>
        </TabContext>
        <TutorialButton tutorialComponent={<SettingsTutorial />} />
      </Box>
      <TabContext value={value}>
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
        <TabPanel value="5" data-testid="preview-features-panel">
          <PreviewFeaturesTab />
        </TabPanel>
        <TabPanel value="99" data-testid="logs-panel">
          <LogSettings />
        </TabPanel>
      </TabContext>
    </Container>
  );
};

export default Settings;
