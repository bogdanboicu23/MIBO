import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import tailwindcss from "@tailwindcss/vite";
import checker from 'vite-plugin-checker';
import path from "path";

// https://vite.dev/config/
export default defineConfig({
    plugins: [
        react(),
        tailwindcss(),
        checker({
            typescript: true,
            eslint: {
                useFlatConfig: true,
                lintCommand: 'eslint "./src/**/*.{js,jsx,ts,tsx}"',
                dev: { logLevel: ['error'] },
            },
            overlay: {
                position: 'tl',
                initialIsOpen: false,
            },
        }),
    ],
    resolve: {
        alias: {
            '@': path.resolve(process.cwd(), 'src'),
            src: path.resolve(process.cwd(), 'src'),
        },
    },
})
