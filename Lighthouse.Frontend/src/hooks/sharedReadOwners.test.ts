import { readdirSync, readFileSync } from "node:fs";
import { join, relative, sep } from "node:path";
import { describe, expect, it } from "vitest";

// Every component that gates on the user's permissions reads them from one shared answer. A component that
// asks the service itself gets its own answer with its own freshness, and the shared answer is no longer the
// one the user sees. So the permissions read has one owner in the source, and this scan covers every source
// file, including the ones added later.
const SOURCE_ROOT = join(__dirname, "..");

const isTestOrTestSupport = (path: string): boolean =>
	/\.test\.tsx?$/.test(path) ||
	path.startsWith(`tests${sep}`) ||
	path.includes(`${sep}tests${sep}`);

const theProductSource = (): Map<string, string> => {
	const files = new Map<string, string>();
	for (const entry of readdirSync(SOURCE_ROOT, {
		recursive: true,
		withFileTypes: true,
	})) {
		if (!entry.isFile() || !/\.tsx?$/.test(entry.name)) continue;
		const path = relative(SOURCE_ROOT, join(entry.parentPath, entry.name));
		if (isTestOrTestSupport(path)) continue;
		files.set(
			path.split(sep).join("/"),
			readFileSync(join(SOURCE_ROOT, path), "utf8"),
		);
	}
	return files;
};

const filesThatCall = (
	source: Map<string, string>,
	method: string,
): string[] => {
	const call = new RegExp(String.raw`\.\s*${method}\s*\(`);
	return [...source]
		.filter(([, text]) => call.test(text))
		.map(([path]) => path)
		.sort((a, b) => a.localeCompare(b));
};

describe("The permissions read has one owner in the product source", () => {
	it("lets only the permissions hook ask for the user's permissions", () => {
		const source = theProductSource();

		const callers = filesThatCall(source, "getAuthorizationSummary");

		expect(
			callers,
			`WHAT: the files asking for the user's permissions are ${JSON.stringify(callers)}, expected only hooks/useRbac.ts. WHY: every component gates on one shared permissions answer; a second asker shows rights of its own age. HOW: read the permissions through useRbac() instead of calling the service.`,
		).toEqual(["hooks/useRbac.ts"]);
	});
});
