// App.tsx
import React from 'react';
import Header from './components/App/Header';
import Sidebar from './components/App/Sidebar';
import Footer from './components/App/Footer';
import OverviewDashboard from './components/Overview/OverviewDashboard';

const App: React.FC = () => {
  return (
    <div className="app">
      <Header />
      <div className="main">
        <Sidebar />
        <div className="content">
          <OverviewDashboard />
        </div>
      </div>
      <Footer />
    </div>
  );
};

export default App;
