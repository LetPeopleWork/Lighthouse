import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    build: {
        outDir: '../Lighthouse/wwwroot',
        emptyOutDir: false,
        minify: true,
    },
    base: '/', // Ensure the base path matches the subfolder
});
