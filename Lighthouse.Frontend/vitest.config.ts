import { defineConfig } from 'vitest/config';

export default defineConfig({
    test: {
        globals: true,
        environment: "jsdom",
        setupFiles: ["./setupTests.ts"],
        env: {
            VITE_API_SERVICE_TYPE: 'DEMO',
        },
        css: {
            modules: {
                classNameStrategy: 'non-scoped',
            },
        },
        coverage: {
            reporter: ['text', 'lcov'],
        },
        reporters: [
            'default',
            ['vitest-sonar-reporter', { outputFile: 'sonar-report.xml' }]
        ],
        server: {
            deps: {
                inline: ['@mui/x-data-grid'],
            },
        },
    },
});
