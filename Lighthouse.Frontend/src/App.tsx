// App.tsx
import React from 'react';
import Header from './components/App/Header/Header';
import Sidebar from './components/App/Sidebar/Sidebar';
import Footer from './components/App/Footer';
import OverviewDashboard from './components/Overview/OverviewDashboard';

const App: React.FC = () => {
  return (
    <div >
      <Header />
      <div >
        <Sidebar />
        <div>
          <OverviewDashboard />
        </div>
      </div>
      <Footer />
    </div>
  );
};

export default App;
