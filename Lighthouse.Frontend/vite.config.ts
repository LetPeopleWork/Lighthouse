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
        coverage: {
            reporter: ['text', 'lcov']
          }
      },
    build: {
        outDir: '../Lighthouse.Backend/wwwroot',
        emptyOutDir: false,
        minify: true,
    },
    base: '/'
});
