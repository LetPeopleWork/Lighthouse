import React from 'react';
import { Box, CssBaseline } from '@mui/material';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Header from './components/App/Header/Header';
import Footer from './components/App/Footer';
import OverviewDashboard from './pages/Overview/OverviewDashboard';
import TeamsOverview from './pages/Teams/TeamsOverview';
import ProjectsOverview from './pages/Projects/ProjectsOverview';
import Settings from './pages/Settings/Settings';
import './App.css';

const App: React.FC = () => {
  return (
    <Router>
      <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
        <CssBaseline />
        <Header />
        <Box component="main" sx={{ flexGrow: 1, display: 'flex', justifyContent: 'center', p: 3 }}>
          <Routes>
            <Route path="/" element={<OverviewDashboard />} />
            <Route path="/teams" element={<TeamsOverview />} />
            <Route path="/projects" element={<ProjectsOverview />} />
            <Route path="/settings" element={<Settings />} />
          </Routes>
        </Box>
        <Footer />
      </Box>
    </Router>
  );
};

export default App;
