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

/** A production file of more than this many lines is too wide: it is split by responsibility. */
const LINE_LIMIT = 1000;

/** The only files the list may ever name: the two that were too wide when the rule landed. */
const BASELINE_OFFENDERS = [
	"src/pages/Common/MetricsView/BaseMetricsView.tsx",
	"src/pages/Common/MetricsView/ragRules.ts",
];

const offendersModule = "./fileSize.offenders.ts";

interface Offender {
	path: string;
	lines: number;
}

interface SizeDrift {
	newcomers: string[];
	stale: string[];
}

/** One entry of the offender list: path relative to src, and why the file still waits. */
interface ListedEntry {
	path: string;
	reason: string;
}

const slashed = (path: string): string => path.replaceAll("\\", "/");

const bySortOrder = (a: string, b: string): number => a.localeCompare(b);

const isProductionSource = (path: string): boolean => {
	const name = path.split("/").at(-1) ?? "";

	return (
		/\.tsx?$/.test(name) &&
		!/\.test\.tsx?$/.test(name) &&
		!name.endsWith(".d.ts") &&
		!/TestKit|Fixture/.test(name) &&
		!path.startsWith("src/tests/")
	);
};

/** Counts line terminators, the way `wc -l` does. */
const lineCount = (text: string): number => text.split("\n").length - 1;

/**
 * Every production file under `src` with its length. A folder that cannot be read fails the
 * scan loudly; it is never taken for "no files".
 */
const productionFiles = (root: string): Offender[] => {
	const found: Offender[] = [];
	const walk = (folder: string): void => {
		for (const entry of readdirSync(folder, { withFileTypes: true })) {
			const full = join(folder, entry.name);
			if (entry.isDirectory()) {
				walk(full);
				continue;
			}
			const path = slashed(relative(root, full));
			if (entry.isFile() && isProductionSource(path)) {
				found.push({ path, lines: lineCount(readFileSync(full, "utf8")) });
			}
		}
	};

	try {
		walk(join(root, "src"));
	} catch (error) {
		throw new Error(
			`The size check could not read the source tree: ${String(error)}`,
		);
	}

	return found;
};

const sizeDrift = (root: string, listed: string[]): SizeDrift => {
	const files = productionFiles(root);
	const lengthOf = new Map(files.map((file) => [file.path, file.lines]));

	return {
		newcomers: files
			.filter((file) => file.lines > LINE_LIMIT && !listed.includes(file.path))
			.map((file) => `${file.path} (${file.lines} lines)`)
			.sort(bySortOrder),
		stale: listed
			.filter((path) => (lengthOf.get(path) ?? 0) <= LINE_LIMIT)
			.sort(bySortOrder),
	};
};

const describeDrift = (drift: SizeDrift): string =>
	[
		`Too wide and not listed (split it by responsibility, do not list it): ${drift.newcomers.join("; ") || "none"}`,
		`Listed but no longer over ${LINE_LIMIT} lines (delete these entries): ${drift.stale.join("; ") || "none"}`,
	].join("\n");

/** The offender list entries, or null when the list does not exist yet. */
const loadOffenderEntries = async (): Promise<ListedEntry[] | null> => {
	try {
		const loaded = await import(/* @vite-ignore */ offendersModule);

		return loaded.TOO_WIDE_TODAY as ListedEntry[];
	} catch {
		return null;
	}
};

/** The paths named on the offender list, as paths from the frontend root. */
const listedOffenders = async (): Promise<string[] | null> => {
	const entries = await loadOffenderEntries();

	return (
		entries?.map((entry) => `src/${slashed(entry.path)}`).sort(bySortOrder) ??
		null
	);
};

const trees: string[] = [];
const lockedFolders: string[] = [];

const linesOf = (count: number): string => "\n".repeat(count);

/** Writes a small project to a throwaway folder, removed after each test. */
const projectWith = (files: Record<string, string>): string => {
	const root = mkdtempSync(join(tmpdir(), "file-size-"));
	trees.push(root);

	for (const [path, content] of Object.entries(files)) {
		mkdirSync(dirname(join(root, path)), { recursive: true });
		writeFileSync(join(root, path), content);
	}

	return root;
};

/**
 * A file that is too wide, kept next to a scenario's own files. A scenario that expects
 * nothing else to be reported still sees this one, so a scan that applied no rule fails.
 */
const control = {
	path: "src/Control.ts",
	files: { "src/Control.ts": linesOf(LINE_LIMIT + 1) },
	entry: `src/Control.ts (${LINE_LIMIT + 1} lines)`,
};

describe("the frontend file size rule", () => {
	afterEach(() => {
		for (const folder of lockedFolders.splice(0)) {
			chmodSync(folder, 0o700);
		}
		for (const root of trees.splice(0)) {
			rmSync(root, { recursive: true, force: true });
		}
	});

	describe("the real source tree", () => {
		it("names only files from the starting list, so it can only shrink", async () => {
			const listed = await listedOffenders();

			expect(
				listed,
				"The offender list src/fileSize.offenders.ts is missing or unreadable. The size rule needs it to name the files still over the limit; create it as TOO_WIDE_TODAY with { path, reason } entries for BaseMetricsView.tsx and ragRules.ts.",
			).not.toBeNull();
			const gained = (listed ?? []).filter(
				(path) => !BASELINE_OFFENDERS.includes(path),
			);
			expect(
				gained,
				`The list gained names that were not on it at the start: ${gained.join("; ")}. A list that grows hides a file that should be split; split the file instead of listing it.`,
			).toEqual([]);
		});

		it("gives every listed file a reason in plain words", async () => {
			const loaded = await loadOffenderEntries();
			const entries = loaded ?? [];

			const withoutReason = entries
				.filter((entry) => !entry.reason?.trim())
				.map((entry) => entry.path);

			expect(
				loaded,
				"The offender list is missing, so no reason can be checked; create TOO_WIDE_TODAY with { path, reason } entries.",
			).not.toBeNull();
			expect(
				withoutReason,
				"Entries without a reason cannot tell a maintainer why the file waits; give each one a reason in plain words.",
			).toEqual([]);
		});

		it("has no production file over the limit that is not listed, and no listed file under it", async () => {
			const listed = (await listedOffenders()) ?? [];

			const drift = sizeDrift(frontendRoot, listed);

			expect(drift, describeDrift(drift)).toEqual({
				newcomers: [],
				stale: [],
			});
		});
	});

	describe("a planted project", () => {
		it("reports a file of one line more than the limit, with its length", () => {
			const root = projectWith({
				"src/pages/Wide.tsx": linesOf(LINE_LIMIT + 1),
			});

			const drift = sizeDrift(root, []);

			expect(
				drift,
				"A file of one line over the limit must be reported with its path and length, or the limit is set too high.",
			).toEqual({
				newcomers: [`src/pages/Wide.tsx (${LINE_LIMIT + 1} lines)`],
				stale: [],
			});
		});

		it("does not report a file of exactly the limit", () => {
			const root = projectWith({
				...control.files,
				"src/Exact.ts": linesOf(LINE_LIMIT),
			});

			const drift = sizeDrift(root, []);

			expect(
				drift,
				"A file of exactly the limit is allowed; only the control file may be reported.",
			).toEqual({ newcomers: [control.entry], stale: [] });
		});

		it("does not report test files, test kits, fixtures, declarations or anything under src/tests", () => {
			const long = linesOf(1500);
			const root = projectWith({
				...control.files,
				"src/pages/Big.test.tsx": long,
				"src/pages/Big.test.ts": long,
				"src/tests/Kit.tsx": long,
				"src/pages/TestKitBuilder.tsx": long,
				"src/pages/ModelFixture.ts": long,
				"src/types/global.d.ts": long,
			});

			const drift = sizeDrift(root, []);

			expect(
				drift,
				"Test code, test kits, fixtures, declarations and src/tests are not production files; only the control file may be reported.",
			).toEqual({ newcomers: [control.entry], stale: [] });
		});

		it("accepts a listed file that is still over the limit", () => {
			const root = projectWith(control.files);

			const drift = sizeDrift(root, [control.path]);

			expect(
				drift,
				"A listed file that is still over the limit is allowed to wait; nothing is new and nothing is stale.",
			).toEqual({ newcomers: [], stale: [] });
		});

		it("reports a listed file that is now short as a stale entry", () => {
			const root = projectWith({
				...control.files,
				"src/Short.ts": linesOf(10),
			});

			const drift = sizeDrift(root, [control.path, "src/Short.ts"]);

			expect(
				drift,
				"A listed file now at or under the limit must be reported stale so its entry is deleted.",
			).toEqual({ newcomers: [], stale: ["src/Short.ts"] });
		});

		it("reports a listed file that no longer exists as a stale entry", () => {
			const root = projectWith(control.files);

			const drift = sizeDrift(root, [control.path, "src/Gone.ts"]);

			expect(
				drift,
				"A listed file that no longer exists must be reported stale so its entry is deleted.",
			).toEqual({ newcomers: [], stale: ["src/Gone.ts"] });
		});

		it("fails loudly when a folder cannot be read, instead of reporting nothing", () => {
			const root = projectWith({
				...control.files,
				"src/locked/Wide.ts": linesOf(LINE_LIMIT + 1),
			});
			const locked = join(root, "src/locked");
			chmodSync(locked, 0o000);
			lockedFolders.push(locked);

			expect(() => sizeDrift(root, [])).toThrow(/could not read/);
		});
	});
});
