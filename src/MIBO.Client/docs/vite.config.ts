import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import tailwindcss from "@tailwindcss/vite";
import mdx from "@mdx-js/rollup";
import remarkFrontmatter from "remark-frontmatter";
import remarkMdxFrontmatter from "remark-mdx-frontmatter";
import remarkGfm from "remark-gfm";
import rehypeSlug from "rehype-slug";
import rehypeAutolinkHeadings from "rehype-autolink-headings";
import path from "node:path";

export default defineConfig({
    plugins: [
        mdx({
            remarkPlugins: [remarkFrontmatter, remarkMdxFrontmatter, remarkGfm],
            rehypePlugins: [rehypeSlug, [rehypeAutolinkHeadings, { behavior: "wrap" }]],
        }),
        react(),
        tailwindcss(),
    ],
    resolve: {
        alias: {
            "@": path.resolve(__dirname, "src"),
        },
    },

    //Blocked request. This host ("inexplicitly-osteal-maison.ngrok-free.dev") is not allowed.
    // To allow this host, add "inexplicitly-osteal-maison.ngrok-free.dev" to `server.allowedHosts` in vite.config.js.
    
    preview: {
        port: 3030,
    },
    server: {
        host: true,
        allowedHosts: ["inexplicitly-osteal-maison.ngrok-free.dev"],
        port: 3030,
    },
});
