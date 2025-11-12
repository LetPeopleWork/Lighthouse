/// <reference types="vitest" />

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import { PRIMARY_BRAND_COLOR } from './src/utils/config';

export default defineConfig({
    plugins: [
        react(),
        VitePWA({
            registerType: 'autoUpdate',
            includeAssets: ['favicon.ico', 'icons/*.png'],
            workbox: {
                maximumFileSizeToCacheInBytes: 3 * 1024 * 1024, // 3 MB
            },
            manifest: {
                name: 'Lighthouse',
                short_name: 'Lighthouse',
                description: 'Lighthouse Frontend Application',
                theme_color: PRIMARY_BRAND_COLOR,
                icons: [
                    {
                        src: '/icons/icon-192x192.png',
                        sizes: '192x192',
                        type: 'image/png'
                    },
                    {
                        src: '/icons/icon-512x512.png',
                        sizes: '512x512',
                        type: 'image/png',
                        purpose: 'any maskable'
                    }
                ]
            }
        })
    ],
    build: {
        outDir: '../Lighthouse.Backend/Lighthouse.Backend/wwwroot',
        emptyOutDir: false,
        minify: true,
    },
    base: '/'
});
