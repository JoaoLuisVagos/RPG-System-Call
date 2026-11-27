import { defineConfig } from "vite";

export default defineConfig({
  server: {
    proxy: {
      "/api": "http://localhost:5080",
      "/hubs": {
        target: "http://localhost:5080",
        ws: true
      }
    }
  }
});
