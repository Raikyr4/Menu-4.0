import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Proxy /api -> back-end .NET (porta definida em back-end/Properties/launchSettings.json)
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5263',
        changeOrigin: true,
      },
    },
  },
});
