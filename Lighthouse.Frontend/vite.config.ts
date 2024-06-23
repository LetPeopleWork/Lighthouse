import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    build: {
        outDir: '../Lighthouse/wwwroot/NewFrontend',
        emptyOutDir: true, // Ensure the output directory is cleaned before building
    },
    base: '/NewFrontend/', // Ensure the base path matches the subfolder
});
