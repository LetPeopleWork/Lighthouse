import { spawnSync } from "node:child_process";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { createRequire } from "node:module";
import { tmpdir } from "node:os";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { afterEach, describe, expect, it } from "vitest";

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const nodeRequire = createRequire(import.meta.url);

/**
 * Every import that crosses a layer in a direction the layers do not allow, as it stands today.
 *
 * The list only ever shrinks. A new leak is not added here to make the build pass — it is fixed.
 * A leak that has been fixed is deleted from here in the same change, so the list can never
 * promise a leak the code no longer has.
 */
const ALLOWED_LEAKS = [
	"src/components/Common/Connection/ModifyConnectionSettings.tsx → src/pages/Settings/Connections/AdditionalFieldsEditor.tsx",
	"src/components/Common/Connection/ModifyConnectionSettings.tsx → src/pages/Settings/Connections/WriteBackMappingsEditor.tsx",
	"src/components/Common/DataOverviewTable/ReportsChips.tsx → src/pages/Common/Reports/reportOwner.ts",
	"src/components/Common/DataOverviewTable/ReportsChips.tsx → src/pages/Common/Reports/useOwnerReports.ts",
	"src/components/Common/DateRangeSelector/DateRangePresets.tsx → src/pages/Common/MetricsView/dateWindow.ts",
	"src/components/Common/DateRangeSelector/DateRangeSelector.tsx → src/pages/Common/MetricsView/dateWindow.ts",
	"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.tsx → src/pages/Common/MetricsView/WidgetShell.tsx",
	"src/components/Common/ProjectSettings/ModifyProjectSettings.tsx → src/pages/Common/AdvancedInputs/AdvancedInputs.tsx",
	"src/components/Common/Team/ModifyTeamSettings.tsx → src/pages/Common/AdvancedInputs/AdvancedInputs.tsx",
	"src/components/Common/Team/ModifyTeamSettings.tsx → src/pages/Teams/Edit/ForecastSettingsComponent.tsx",
	"src/components/Common/Team/RefinementBandSettings.tsx → src/pages/Teams/Detail/Refinement/InfoTooltip.tsx",
	"src/components/Common/Team/RefinementBandSettings.tsx → src/pages/Teams/Detail/Refinement/needWording.ts",
	"src/hooks/useCreateWizard.ts → src/components/DataRetrievalWizards/index.ts",
	"src/hooks/useMetricsData.ts → src/pages/Common/MetricsView/categoryMetadata.ts",
	"src/hooks/useMetricsData.ts → src/pages/Common/MetricsView/widgetStatus.ts",
	"src/hooks/useRuleRowDraft.ts → src/components/Common/DeliveryRuleBuilder/types.ts",
	"src/hooks/useUsageDataConsent.tsx → src/components/UsageData/UsageDataIndicator.tsx",
	"src/models/Metrics/InfoWidgetData.ts → src/pages/Common/MetricsView/trendTypes.ts",
	"src/services/UsageData/usageDataEvents.ts → src/hooks/useUsageDataConsent.tsx",
	"src/services/UsageData/usageDataReporter.ts → src/hooks/useUsageDataConsent.tsx",
	"src/utils/deliveries/deliveryArchivedRefusal.ts → src/services/Api/ApiError.ts",
];

interface BoundaryViolation {
	from_path: string;
	to_path: string;
}

const bySortOrder = (a: string, b: string): number => a.localeCompare(b);

const toEntry = (violation: BoundaryViolation): string =>
	`${violation.from_path} → ${violation.to_path}`;

/** Runs the boundary check over a project root with the real configuration; one entry per leaking import. */
const findLeaks = (root: string): string[] => {
	const launcher = nodeRequire.resolve("fallow/bin/fallow");
	const run = spawnSync(
		process.execPath,
		[
			launcher,
			"architecture",
			"--boundaries",
			"--production",
			"-r",
			root,
			"-c",
			join(frontendRoot, ".fallowrc.json"),
			"-f",
			"json",
		],
		{ encoding: "utf8", maxBuffer: 64 * 1024 * 1024 },
	);
	const report = JSON.parse(run.stdout) as {
		boundary_violations: BoundaryViolation[];
	};

	return report.boundary_violations.map(toEntry).sort(bySortOrder);
};

const compareWithAllowList = (found: string[], allowed: string[]) => ({
	newcomers: found.filter((entry) => !allowed.includes(entry)),
	stale: allowed.filter((entry) => !found.includes(entry)),
});

const describeDrift = (drift: ReturnType<typeof compareWithAllowList>) =>
	[
		`New leaks (fix them, do not list them): ${drift.newcomers.join("; ") || "none"}`,
		`Listed but gone (delete these entries): ${drift.stale.join("; ") || "none"}`,
	].join("\n");

const trees: string[] = [];

/** Writes a small project to a throwaway folder; nothing under src/ has to hold a deliberate leak. */
const projectWith = (files: Record<string, string>): string => {
	const root = mkdtempSync(join(tmpdir(), "layer-boundaries-"));
	trees.push(root);

	for (const [path, content] of Object.entries(files)) {
		mkdirSync(dirname(join(root, path)), { recursive: true });
		writeFileSync(join(root, path), content);
	}

	return root;
};

const specifierFrom = (importer: string, target: string): string => {
	const path = relative(dirname(importer), target).replace(/\.(ts|tsx)$/, "");

	return path.startsWith(".") ? path : `./${path}`;
};

/** A project whose one file reaches into another, plus the target it reaches. */
const importing = (importer: string, target: string): string =>
	projectWith({
		[importer]: `import { value } from "${specifierFrom(importer, target)}";\nexport const used = value;\n`,
		[target]: "export const value = 1;\n",
	});

const leaksUnder = (layer: string, entries: string[]): string[] =>
	entries.filter((entry) => entry.startsWith(`src/${layer}/`));

describe("the frontend layers", () => {
	afterEach(() => {
		for (const root of trees.splice(0)) {
			rmSync(root, { recursive: true, force: true });
		}
	});

	describe("today's leaks", () => {
		it.skip("components reach into pages in exactly the twelve listed places", () => {
			const drift = compareWithAllowList(
				leaksUnder("components", findLeaks(frontendRoot)),
				leaksUnder("components", ALLOWED_LEAKS),
			);

			expect(drift, describeDrift(drift)).toEqual({
				newcomers: [],
				stale: [],
			});
		});

		it.skip("the whole frontend leaks across layers in exactly the twenty-one listed places", () => {
			const drift = compareWithAllowList(
				findLeaks(frontendRoot),
				[...ALLOWED_LEAKS].sort(bySortOrder),
			);

			expect(drift, describeDrift(drift)).toEqual({
				newcomers: [],
				stale: [],
			});
		});
	});

	describe("a component reaching into a page", () => {
		const importer = "src/components/Widget.tsx";
		const target = "src/pages/Page.tsx";

		it.skip("is refused as a new leak, naming the component and the page", () => {
			const drift = compareWithAllowList(
				findLeaks(importing(importer, target)),
				[],
			);

			expect(drift).toEqual({
				newcomers: [`${importer} → ${target}`],
				stale: [],
			});
		});

		it.skip("is not reported when it is only a test that does so", () => {
			const root = importing("src/components/Widget.test.ts", target);

			expect(findLeaks(root)).toEqual([]);
		});
	});

	describe("a listed leak that has been fixed", () => {
		it.skip("is refused as stale until its entry is deleted, naming the entry", () => {
			const fixed = "src/components/Widget.tsx → src/pages/Page.tsx";
			const root = projectWith({
				"src/components/Widget.tsx": "export const used = 1;\n",
				"src/pages/Page.tsx": "export const value = 1;\n",
			});

			const drift = compareWithAllowList(findLeaks(root), [fixed]);

			expect(drift).toEqual({ newcomers: [], stale: [fixed] });
		});
	});

	describe("directions the layers allow", () => {
		it.skip.each([
			{
				direction: "a component using a hook",
				importer: "src/components/Widget.tsx",
				target: "src/hooks/useThing.ts",
			},
			{
				direction: "a page using a component",
				importer: "src/pages/Page.tsx",
				target: "src/components/Widget.tsx",
			},
		])("report nothing for $direction", ({ importer, target }) => {
			expect(findLeaks(importing(importer, target))).toEqual([]);
		});
	});

	describe("each remaining layer rule is in force", () => {
		it.skip.each([
			{
				rule: "a hook reaching into a page",
				importer: "src/hooks/useThing.ts",
				target: "src/pages/Page.tsx",
			},
			{
				rule: "a hook reaching into a component",
				importer: "src/hooks/useThing.ts",
				target: "src/components/Widget.tsx",
			},
			{
				rule: "a service reaching into a hook",
				importer: "src/services/Reporter.ts",
				target: "src/hooks/useThing.ts",
			},
			{
				rule: "a model reaching into a page",
				importer: "src/models/Shape.ts",
				target: "src/pages/Page.tsx",
			},
			{
				rule: "a utility reaching into a service",
				importer: "src/utils/refusal.ts",
				target: "src/services/Api.ts",
			},
		])("refuses $rule, naming both ends", ({ importer, target }) => {
			const drift = compareWithAllowList(
				findLeaks(importing(importer, target)),
				[],
			);

			expect(drift).toEqual({
				newcomers: [`${importer} → ${target}`],
				stale: [],
			});
		});
	});
});
