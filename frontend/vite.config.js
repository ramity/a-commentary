import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [vue()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_BACKEND_URL || 'http://backend:5000',
        changeOrigin: true,
      },
      '/posts': {
        target: process.env.VITE_NGINX_URL || 'http://nginx',
        changeOrigin: true,
      }
    }
  }
});
