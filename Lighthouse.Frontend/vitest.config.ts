import { defineConfig } from 'vitest/config';

export default defineConfig({
    test: {
        globals: true,
        environment: "jsdom",
        setupFiles: ["./setupTests.ts"],
        env: {
            VITE_API_SERVICE_TYPE: 'DEMO',
        },
        css: true,
        coverage: {
            reporter: ['text', 'lcov'],
        },
    },
});
