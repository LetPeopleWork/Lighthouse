import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'

import { createTheme, ThemeOptions, ThemeProvider } from '@mui/material/styles';
import { registerLicense } from '@syncfusion/ej2-base';

registerLicense('Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWH1fdHRTQmFcVENwXUo=');

const themeOptions: ThemeOptions = createTheme({
    palette: {
      mode: 'light',
      primary: {
        main: '#30574e',
      },
      secondary: {
        main: '#46232d',
      },
    },
  }
);


ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ThemeProvider theme={themeOptions}>
      <App />
    </ThemeProvider>
  </React.StrictMode>,
)
