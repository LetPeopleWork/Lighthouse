import React from 'react';
import { Box, CssBaseline } from '@mui/material';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Header from './components/App/Header/Header';
import Footer from './components/App/Footer/Footer';
import OverviewDashboard from './pages/Overview/OverviewDashboard';
import TeamsOverview from './pages/Teams/Overview/TeamsOverview';
import ProjectsOverview from './pages/Projects/Overview/ProjectsOverview';
import Settings from './pages/Settings/Settings';
import './App.css';
import TeamDetail from './pages/Teams/Detail/TeamDetail';
import EditTeam from './pages/Teams/Edit/EditTeam';
import EditProject from './pages/Projects/Edit/EditProject';
import ProjectDetail from './pages/Projects/Detail/ProjectDetail';
import LighthouseFullTutorial from './components/App/LetPeopleWork/Tutorial/LighthouseFullTutorial';

const App: React.FC = () => {
  return (
    <Router>
      <Box className="container">
        <CssBaseline />
        <Header />
        <Box component="main" className="main-content">
          <Routes>
            <Route path="/" element={<OverviewDashboard />} />
            <Route path="/teams">
              <Route index element={<TeamsOverview />} />
              <Route path=":id" element={<TeamDetail />} />
              <Route path="edit/:id" element={<EditTeam />} />
              <Route path="new" element={<EditTeam />} />
            </Route>
            <Route path="/projects">
              <Route index element={<ProjectsOverview />} />
              <Route path=":id" element={<ProjectDetail />} />
              <Route path="edit/:id" element={<EditProject />} />
              <Route path="new" element={<EditProject />} />
            </Route>
            <Route path="/settings" element={<Settings />} />
            <Route path="/tutorial" element={<LighthouseFullTutorial />} />
          </Routes>
        </Box>
        <Footer />
      </Box>
    </Router>
  );
};

export default App;
