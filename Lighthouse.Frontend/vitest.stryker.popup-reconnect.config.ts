import { defineConfig } from "vitest/config";

export default defineConfig({
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		env: {
			VITE_API_SERVICE_TYPE: "DEMO",
		},
		css: {
			modules: {
				classNameStrategy: "non-scoped",
			},
		},
		include: [
			"src/hooks/useOAuthPopup.test.ts",
			"src/components/Common/Connections/OAuthPopupComplete.test.tsx",
			"src/components/Common/Connections/ReconnectBanner.test.tsx",
			"src/components/Common/Connections/OAuthAuthForm.test.tsx",
		],
		server: {
			deps: {
				inline: ["@mui/x-data-grid"],
			},
		},
		pool: "forks",
		isolate: true,
		fileParallelism: false,
	},
});
