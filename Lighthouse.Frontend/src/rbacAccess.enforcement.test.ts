import {
	chmodSync,
	mkdirSync,
	mkdtempSync,
	readdirSync,
	readFileSync,
	rmSync,
	writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { afterEach, describe, expect, it } from "vitest";

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");

/** The one file that handles the members and group mappings of a Team or a Portfolio. */
const ACCESS_HOOK = "src/hooks/useScopedAccess.ts";

/** The one file that may name the authorization summary endpoint. */
const RBAC_SERVICE = "src/services/Api/RbacService.ts";

const SUMMARY_ENDPOINT = "my-summary";

/** The members and group mappings of one Team or Portfolio. Pages go through the access hook for these. */
const SCOPED_ACCESS_METHODS = [
	"getTeamMembers",
	"upsertTeamMember",
	"removeTeamMember",
	"getPortfolioMembers",
	"upsertPortfolioMember",
	"removePortfolioMember",
	"getTeamGroupMappings",
	"getPortfolioGroupMappings",
	"createGroupMapping",
	"removeGroupMapping",
];

/**
 * Creating and removing a group mapping is also what the system-wide administration page does,
 * for rules that belong to no single Team or Portfolio. That page is the only other owner of them.
 */
const SYSTEM_WIDE_ADMINISTRATION = "src/pages/Settings/Rbac/RbacSettings.tsx";
const SYSTEM_WIDE_METHODS = ["createGroupMapping", "removeGroupMapping"];

const slashed = (path: string): string => path.replaceAll("\\", "/");

const bySortOrder = (a: string, b: string): number => a.localeCompare(b);

const isProductionSource = (path: string): boolean => {
	const name = path.split("/").pop() ?? "";

	return (
		/\.tsx?$/.test(name) &&
		!/\.test\.tsx?$/.test(name) &&
		!name.endsWith(".d.ts") &&
		!/TestKit|Fixture/.test(name) &&
		!path.startsWith("src/tests/")
	);
};

/** Every production file under `src` with its text. A folder that cannot be read fails the scan loudly. */
const productionFiles = (root: string): { path: string; text: string }[] => {
	const found: { path: string; text: string }[] = [];
	const walk = (folder: string): void => {
		for (const entry of readdirSync(folder, { withFileTypes: true })) {
			const full = join(folder, entry.name);
			if (entry.isDirectory()) {
				walk(full);
				continue;
			}
			const path = slashed(relative(root, full));
			if (entry.isFile() && isProductionSource(path)) {
				found.push({ path, text: readFileSync(full, "utf8") });
			}
		}
	};

	try {
		walk(join(root, "src"));
	} catch (error) {
		throw new Error(
			`The access check could not read the source tree: ${String(error)}`,
		);
	}

	return found;
};

const callsMethod = (text: string, method: string): boolean =>
	new RegExp(String.raw`\.\s*${method}\b`).test(text);

const mayCall = (path: string, method: string): boolean =>
	path === ACCESS_HOOK ||
	(path === SYSTEM_WIDE_ADMINISTRATION && SYSTEM_WIDE_METHODS.includes(method));

/** One entry per file and what it reaches for that it does not own, sorted. */
const accessViolations = (root: string): string[] =>
	productionFiles(root)
		.flatMap(({ path, text }) => [
			...SCOPED_ACCESS_METHODS.filter(
				(method) => callsMethod(text, method) && !mayCall(path, method),
			).map((method) => `${path} calls rbacService.${method}`),
			...(path !== RBAC_SERVICE && text.includes(SUMMARY_ENDPOINT)
				? [`${path} fetches ${SUMMARY_ENDPOINT}`]
				: []),
		])
		.sort(bySortOrder);

const describeViolations = (found: string[]): string =>
	`Files that reach the access methods or the summary endpoint themselves: ${found.join("; ") || "none"}. Members and group mappings of a Team or Portfolio go through ${ACCESS_HOOK}, and ${SUMMARY_ENDPOINT} is fetched only in ${RBAC_SERVICE}; move the call there, do not list the file.`;

const trees: string[] = [];
const lockedFolders: string[] = [];

/** Writes a small project to a throwaway folder, removed after each test. */
const projectWith = (files: Record<string, string>): string => {
	const root = mkdtempSync(join(tmpdir(), "rbac-access-"));
	trees.push(root);

	for (const [path, content] of Object.entries(files)) {
		mkdirSync(dirname(join(root, path)), { recursive: true });
		writeFileSync(join(root, path), content);
	}

	return root;
};

/**
 * A page reaching for the service, kept next to a scenario's own files. A scenario that expects
 * nothing else to be reported still sees this one, so a scan that applied no rule fails.
 */
const control = {
	files: {
		"src/pages/Control.tsx": "rbacService.getTeamMembers(1);\n",
	},
	entry: "src/pages/Control.tsx calls rbacService.getTeamMembers",
};

describe("the Team and Portfolio access rule", () => {
	afterEach(() => {
		for (const folder of lockedFolders.splice(0)) {
			chmodSync(folder, 0o700);
		}
		for (const root of trees.splice(0)) {
			rmSync(root, { recursive: true, force: true });
		}
	});

	describe("the real source tree", () => {
		it("has no file besides the access hook that handles the members and group mappings of a Team or Portfolio, and none besides the service that fetches the summary", () => {
			const found = accessViolations(frontendRoot);

			expect(found, describeViolations(found)).toEqual([]);
		});
	});

	describe("a planted project", () => {
		it.each(SCOPED_ACCESS_METHODS)("names a page that calls %s", (method) => {
			const root = projectWith({
				"src/pages/Teams/Page.tsx": `await rbacService.${method}(1);\n`,
			});

			expect(
				accessViolations(root),
				`A page calling ${method} must be named, or a copy of the handlers can come back unseen.`,
			).toEqual([`src/pages/Teams/Page.tsx calls rbacService.${method}`]);
		});

		it("does not name the access hook for calling them", () => {
			const root = projectWith({
				...control.files,
				[ACCESS_HOOK]: SCOPED_ACCESS_METHODS.map(
					(method) => `rbacService.${method}(1);`,
				).join("\n"),
			});

			expect(
				accessViolations(root),
				"The hook owns these calls; only the control page may be reported.",
			).toEqual([control.entry]);
		});

		it("lets the system-wide administration page create and remove group mappings, and nothing more", () => {
			const root = projectWith({
				...control.files,
				[SYSTEM_WIDE_ADMINISTRATION]: [
					"rbacService.createGroupMapping(request);",
					"rbacService.removeGroupMapping(1);",
					"rbacService.getTeamMembers(1);",
				].join("\n"),
			});

			expect(
				accessViolations(root),
				"The system-wide page owns system-wide group mappings, but not the members of a Team.",
			).toEqual([
				control.entry,
				`${SYSTEM_WIDE_ADMINISTRATION} calls rbacService.getTeamMembers`,
			]);
		});

		it("names a file other than the service that fetches the summary, and not the service itself", () => {
			const root = projectWith({
				...control.files,
				[RBAC_SERVICE]: `get("/authorization/${SUMMARY_ENDPOINT}");\n`,
				"src/hooks/useSomething.ts": `fetch("/api/latest/authorization/${SUMMARY_ENDPOINT}");\n`,
			});

			expect(
				accessViolations(root),
				`The summary is fetched only in ${RBAC_SERVICE}; another fetcher must be named next to the control.`,
			).toEqual([
				`src/hooks/useSomething.ts fetches ${SUMMARY_ENDPOINT}`,
				control.entry,
			]);
		});

		it("does not look at test files, test kits or anything under src/tests", () => {
			const call =
				"rbacService.getTeamMembers(1); '/authorization/my-summary';\n";
			const root = projectWith({
				...control.files,
				"src/pages/Page.test.tsx": call,
				"src/pages/Page.test.ts": call,
				"src/tests/Mock.ts": call,
				"src/pages/PageTestKit.tsx": call,
				"src/pages/PageFixture.ts": call,
			});

			expect(
				accessViolations(root),
				"Test code is not production code; only the control page may be reported.",
			).toEqual([control.entry]);
		});

		it("fails loudly when a folder cannot be read, instead of reporting nothing", () => {
			const root = projectWith({
				...control.files,
				"src/locked/Page.tsx": "rbacService.getTeamMembers(1);\n",
			});
			const locked = join(root, "src/locked");
			chmodSync(locked, 0o000);
			lockedFolders.push(locked);

			expect(() => accessViolations(root)).toThrow(/could not read/);
		});
	});
});
