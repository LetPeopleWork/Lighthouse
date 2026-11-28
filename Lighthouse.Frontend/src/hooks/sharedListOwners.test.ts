import { readdirSync, readFileSync } from "node:fs";
import { join, relative, sep } from "node:path";
import { describe, expect, it } from "vitest";

// Every component that shows the Teams list, the Portfolios list or the license status reads them from one
// shared answer. A component that asks the service itself gets its own answer with its own freshness, and the
// shared answer is no longer the one the user sees. So each read has one owner in the source, and this scan
// covers every source file, including the ones added later.
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

// A call through a member (`service.getTeams(`), through a destructured alias (`const { getTeams } = service`,
// `const { getTeams: ask } = service`) or through a string key (`service["getTeams"]`) reaches the service.
const reachesTheService = (text: string, method: string): boolean => {
	const throughAMember = new RegExp(String.raw`\.\s*${method}\s*\(`);
	const throughADestructuring = new RegExp(
		String.raw`\{[^{}]*\b${method}\b[^{}]*\}\s*=[^=>]`,
	);
	const throughAStringKey = new RegExp(
		String.raw`\[\s*["'\`]${method}["'\`]\s*\]`,
	);
	return (
		throughAMember.test(text) ||
		throughADestructuring.test(text) ||
		throughAStringKey.test(text)
	);
};

const filesThatReach = (
	source: Map<string, string>,
	method: string,
): string[] =>
	[...source]
		.filter(([, text]) => reachesTheService(text, method))
		.map(([path]) => path)
		.sort((a, b) => a.localeCompare(b));

const expectOnlyOwner = (
	method: string,
	what: string,
	owner: string,
	hook: string,
) => {
	const callers = filesThatReach(theProductSource(), method);
	expect(
		callers,
		`WHAT: the files asking for ${what} are ${JSON.stringify(callers)}, expected only ${owner}. WHY: every component shows one shared answer; a second asker shows ${what} of its own age. HOW: read ${what} through ${hook} instead of calling the service.`,
	).toEqual([owner]);
};

describe("The Teams, the Portfolios and the license each have one owner in the product source", () => {
	it("lets only the shared lists hook ask for the Teams", () => {
		expectOnlyOwner(
			"getTeams",
			"the Teams",
			"hooks/useUnitLists.ts",
			"useTeamsQuery",
		);
	});

	it("lets only the shared lists hook ask for the Portfolios", () => {
		expectOnlyOwner(
			"getPortfolios",
			"the Portfolios",
			"hooks/useUnitLists.ts",
			"usePortfoliosQuery",
		);
	});

	it("lets only the license hook ask for the license status", () => {
		expectOnlyOwner(
			"getLicenseStatus",
			"the license status",
			"hooks/useLicenseRestrictions.ts",
			"useLicenseStatusQuery",
		);
	});

	it("notices a destructured alias of the service method", () => {
		const aliased = new Map([
			["a.ts", "const { getTeams: ask } = teamService; ask();"],
			["b.ts", "const { teamService } = useContext(Ctx);"],
			["c.ts", 'teamService["getTeams"]();'],
		]);

		expect(filesThatReach(aliased, "getTeams")).toEqual(["a.ts", "c.ts"]);
	});
});
