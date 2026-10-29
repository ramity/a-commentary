/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        chess: {
          brilliant: '#1baca6',
          great: '#3692e7',
          best: '#7fa650',
          excellent: '#98bc64',
          good: '#a7bd86',
          book: '#a88865',
          inaccuracy: '#e58f2a',
          mistake: '#e6912c',
          blunder: '#ca3431',
          miss: '#e06453'
        }
      },
      fontFamily: {
        sans: ['"Plus Jakarta Sans"', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'monospace']
      }
    },
  },
  plugins: [],
}
