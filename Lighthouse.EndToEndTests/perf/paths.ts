import path from "node:path";

// Readings can carry host data, so the harness writes them here and nowhere else; the folder is gitignored.
export const perfOutputDirectory = path.join(__dirname, "out");
