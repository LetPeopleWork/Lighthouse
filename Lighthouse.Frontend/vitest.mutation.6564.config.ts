// @ts-nocheck
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
		// Only the files that cover report notes run: the notes area, its byline and refusal copy, and the report service.
		include: [
			"src/pages/Common/Reports/ReportNotes.add.test.tsx",
			"src/pages/Common/Reports/ReportNotes.change.test.tsx",
			"src/pages/Common/Reports/ReportNotes.reader.test.tsx",
			"src/pages/Common/Reports/ReportNotes.byline.test.ts",
			"src/pages/Common/Reports/reportNoteRefusalCopy.test.ts",
			"src/pages/Common/Reports/ReportView.test.tsx",
			"src/services/Api/ReportService.test.ts",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/],
			},
		},
		pool: "threads",
		isolate: true,
	},
});
