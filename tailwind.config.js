/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./apps/client/src/**/*.{html,ts,scss}'],
  theme: {
    extend: {
      fontFamily: {
        display: ['Manrope', 'Segoe UI', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
      },
    },
  },
  plugins: [],
};
