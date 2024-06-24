/// <reference types="vitest" />

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    test: {
        globals: true,
        environment: "jsdom",
        setupFiles: ["./setupTests.ts"],
        css: true,
        reporters: [
            ['vitest-sonar-reporter', { outputFile: 'sonar-report.xml' }],
        ],
      },
    build: {
        outDir: '../Lighthouse.Backend/wwwroot',
        emptyOutDir: false,
        minify: true,
    },
    base: '/'
});
