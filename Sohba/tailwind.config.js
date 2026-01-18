/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./Views/**/*.cshtml",
        "./wwwroot/js/**/*.js",
        "./Pages/**/*.cshtml"
    ],
    theme: {
        extend: {},
    },
    plugins: [],
    corePlugins: {
        preflight: false, // Ì„‰⁄ Tailwind „‰ ≈⁄«œ… ﬂ «»… ﬂ· «·‹ CSS «·√”«”Ì
    },
}
