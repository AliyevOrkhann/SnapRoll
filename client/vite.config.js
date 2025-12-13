import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import mkcert from 'vite-plugin-mkcert'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), mkcert()],
  server: {
    host: true,
    https: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5038',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'http://localhost:5038',
        ws: true,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
