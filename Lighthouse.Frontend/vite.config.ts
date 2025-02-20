/// <reference types="vitest" />

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    build: {
        outDir: '../Lighthouse.Backend/Lighthouse.Backend/wwwroot',
        emptyOutDir: false,
        minify: true,
    },
    base: '/'
});
