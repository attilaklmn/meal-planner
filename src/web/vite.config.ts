/// <reference types="vitest/config" />
import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Aspire passes the Api's address through service discovery env vars (WithReference(api) in the AppHost).
// Without Aspire (plain `npm run dev`) fall back to the Api's own launch profile.
const apiUrl =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  'http://localhost:5030'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  server: {
    // The browser only talks to the Vite dev server; it forwards API calls, so no CORS setup is needed.
    proxy: {
      '/api': { target: apiUrl, changeOrigin: true, secure: false },
      '/health': { target: apiUrl, changeOrigin: true, secure: false },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
