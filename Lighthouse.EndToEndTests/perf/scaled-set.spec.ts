import * as fs from "node:fs";
import path from "node:path";
import { type APIRequestContext, expect, test } from "@playwright/test";
import { createPortfolio } from "../tests/helpers/api/portfolios";
import { createTeam, updateTeam } from "../tests/helpers/api/teams";
import {
	canSkipRounds,
	isRowOf,
	type RefreshLogRow,
} from "./helpers/apiVersionMap";
import {
	generateScaledSet,
	portfolioStates,
	portfolioWorkItemTypes,
	scaledSetTeamNames,
	teamStates,
	teamWorkItemTypes,
} from "./helpers/csvScaledSet";
import {
	quietTheInstance,
	readRefreshLog,
	startComputedRound,
	startSkippedRound,
	waitUntilNothingIsQueued,
} from "./helpers/rounds";
import { perfOutputDirectory } from "./paths";

// The licence fixture is gitignored and is read where it lies, never copied: more than three Teams and
// Portfolios need Premium.
const licenceFile =
	process.env.PERF_LICENSE_FILE ??
	path.join(__dirname, "..", "tests", "fixtures", "valid_premium_license.json");

// Every Portfolio refresh after the first took minutes in the scaled set: Payments took 10
// minutes and Mobile 5 on SQLite, a filler up to 37, so seeding runs far past the default per-test budget.
const seedingTimeout = 4 * 60 * 60_000;

// PERF_FILLERS=1 adds the three filler Portfolios that bring the instance to about 3,000 Features.
const withFillers = process.env.PERF_FILLERS === "1";

const scaledSetDirectory = path.join(perfOutputDirectory, "scaled-set");

type Named = { id: number; name: string };

function outputFolderFor(baseURL: string | undefined): string {
	const port = new URL(baseURL ?? "http://localhost").port || "default";
	const folder = path.join(scaledSetDirectory, `port-${port}`);
	fs.mkdirSync(folder, { recursive: true });
	return folder;
}

async function ensurePremium(api: APIRequestContext): Promise<void> {
	const status = await (await api.get("/api/latest/license")).json();
	if (status.canUsePremiumFeatures) {
		return;
	}
	const response = await api.post("/api/latest/license/import", {
		multipart: {
			file: {
				name: "license.json",
				mimeType: "application/json",
				buffer: fs.readFileSync(licenceFile),
			},
		},
	});
	expect(response.ok(), await response.text()).toBeTruthy();
}

async function createCsvConnection(api: APIRequestContext): Promise<number> {
	const supported = (await (
		await api.get("/api/latest/worktrackingsystemconnections/supported")
	).json()) as { workTrackingSystem: string | number }[];
	const csvTemplate = supported.find(
		(system) => system.workTrackingSystem === "Csv",
	);
	expect(csvTemplate, "the instance offers no CSV connection").toBeDefined();

	const response = await api.post("/api/latest/worktrackingsystemconnections", {
		data: { ...csvTemplate, id: 0, name: "Northwind CSV" },
	});
	expect(response.ok(), await response.text()).toBeTruthy();
	return ((await response.json()) as Named).id;
}

async function readThroughputHistoryOf90Days(
	api: APIRequestContext,
	teamId: number,
): Promise<void> {
	const settings = await (
		await api.get(`/api/latest/teams/${teamId}/settings`)
	).json();
	const response = await api.put(`/api/latest/teams/${teamId}`, {
		data: { ...settings, throughputHistory: 90 },
	});
	expect(response.ok(), await response.text()).toBeTruthy();
}

// Not the shared updatePortfolio helper: it still posts to a route the backend no longer serves, and it does
// not look at the answer, so its failure goes unnoticed.
async function refreshPortfolio(
	api: APIRequestContext,
	portfolioId: number,
): Promise<void> {
	const response = await api.post(
		`/api/latest/portfolios/${portfolioId}/refresh`,
	);
	expect(response.ok(), await response.text()).toBeTruthy();
}

async function list(
	api: APIRequestContext,
	url: string,
): Promise<(Named & { features: unknown[] })[]> {
	return (await api.get(url)).json();
}

async function findNorthwindPlatform(api: APIRequestContext): Promise<number> {
	const portfolios = await list(api, "/api/latest/portfolios");
	const platform = portfolios.find(
		(portfolio) => portfolio.name === "Northwind Platform",
	);
	expect(platform, "seed the scaled set first").toBeDefined();
	return (platform as Named).id;
}

test("builds the scaled Northwind set over the public REST API", async ({
	request,
	baseURL,
}) => {
	test.setTimeout(seedingTimeout * 2);
	const outputFolder = outputFolderFor(baseURL);
	const existing = await list(request, "/api/latest/portfolios");
	expect(
		existing.map((portfolio) => portfolio.name),
		"the seeder expects a fresh instance",
	).not.toContain("Northwind Platform");

	await ensurePremium(request);

	const set = generateScaledSet(new Date(), withFillers);
	for (const entity of [...set.teams, ...set.portfolios]) {
		fs.writeFileSync(path.join(outputFolder, `${entity.name}.csv`), entity.csv);
	}

	const connectionId = await createCsvConnection(request);

	const teams: Named[] = [];
	for (const team of set.teams) {
		const created = await createTeam(
			request,
			team.name,
			connectionId,
			team.csv,
			teamWorkItemTypes,
			teamStates,
		);
		expect(created.id, `Team ${team.name} was not created`).toBeGreaterThan(0);
		await readThroughputHistoryOf90Days(request, created.id);
		teams.push(created);
	}

	const portfolios: Named[] = [];
	for (const portfolio of set.portfolios) {
		const created = await createPortfolio(
			request,
			portfolio.name,
			connectionId,
			portfolio.csv,
			portfolioWorkItemTypes,
			portfolioStates,
		);
		expect(
			created.id,
			`Portfolio ${portfolio.name} was not created`,
		).toBeGreaterThan(0);
		portfolios.push(created);
	}

	const seedingStartedAt = Date.now();
	for (const team of teams) {
		await updateTeam(request, team.id);
	}
	await waitUntilNothingIsQueued(request, seedingTimeout);
	const teamsUpdatedAt = Date.now();

	for (const portfolio of portfolios) {
		await refreshPortfolio(request, portfolio.id);
	}
	await waitUntilNothingIsQueued(request, seedingTimeout);
	const portfoliosUpdatedAt = Date.now();

	const forecasts = [];
	for (const portfolio of portfolios) {
		const round = await startComputedRound(
			request,
			portfolio.id,
			seedingTimeout,
		);
		forecasts.push({
			portfolio: portfolio.name,
			success: round.refreshLogRow.success,
			verdict: round.verdict,
			durationMs: round.refreshLogRow.durationMs,
		});
	}
	await waitUntilNothingIsQueued(request, seedingTimeout);
	// Last, not first: a Portfolio never updated counts as due however long the wait is set, so a one-minute
	// tick during seeding would queue a second refresh behind every Portfolio still waiting for its first.
	await quietTheInstance(request);

	const refreshLog = await readRefreshLog(request);
	const servedPortfolios = await list(request, "/api/latest/portfolios");
	const servedTeams = await list(request, "/api/latest/teams");

	const summary = {
		seededAt: new Date().toISOString(),
		minutes: {
			teamUpdates: (teamsUpdatedAt - seedingStartedAt) / 60_000,
			portfolioUpdates: (portfoliosUpdatedAt - teamsUpdatedAt) / 60_000,
			forecasts: (Date.now() - portfoliosUpdatedAt) / 60_000,
		},
		portfolios: set.portfolios.map((generated) => ({
			name: generated.name,
			generatedFeatures: generated.features,
			withDependencies: generated.withDependencies,
			withParent: generated.withParent,
			servedFeatures: servedPortfolios.find(
				(served) => served.name === generated.name,
			)?.features.length,
		})),
		teams: set.teams.map((generated) => {
			const team = teams.find(
				(created) => created.name === generated.name,
			) as Named;
			const lastRefresh = refreshLog
				.filter((row) => isRowOf(row, "Team", team.id))
				.reduce<RefreshLogRow | undefined>(
					(latest, row) => (latest && latest.id > row.id ? latest : row),
					undefined,
				);
			return {
				name: generated.name,
				remainingItems: generated.remainingItems,
				closedItems: generated.closedItems,
				refreshedItems: lastRefresh?.itemCount,
				refreshSucceeded: lastRefresh?.success,
				servedFeatures: servedTeams.find(
					(served) => served.name === generated.name,
				)?.features.length,
			};
		}),
		forecasts,
	};
	fs.writeFileSync(
		path.join(outputFolder, "summary.json"),
		JSON.stringify(summary, null, 2),
	);

	for (const portfolio of summary.portfolios) {
		expect(portfolio.servedFeatures, portfolio.name).toBe(
			portfolio.generatedFeatures,
		);
	}
	for (const team of summary.teams) {
		expect(team.refreshSucceeded, team.name).toBe(true);
		expect(team.refreshedItems, team.name).toBe(
			team.remainingItems + team.closedItems,
		);
		expect(team.servedFeatures, team.name).toBeGreaterThan(0);
	}
	for (const forecast of forecasts) {
		expect(forecast.success, forecast.portfolio).toBe(true);
	}
	expect(servedTeams.map((team) => team.name).sort()).toEqual(
		[...scaledSetTeamNames].sort(),
	);
	expect(servedPortfolios).toHaveLength(set.portfolios.length);
});

test("starts a computed and a skipped round on Northwind Platform", async ({
	request,
	baseURL,
}) => {
	const platformId = await findNorthwindPlatform(request);
	await waitUntilNothingIsQueued(request, seedingTimeout);

	const computed = await startComputedRound(request, platformId);
	await waitUntilNothingIsQueued(request, seedingTimeout);
	const skipped = await startSkippedRound(request, platformId);
	await waitUntilNothingIsQueued(request, seedingTimeout);

	fs.writeFileSync(
		path.join(outputFolderFor(baseURL), "rounds.json"),
		JSON.stringify({ computed, skipped }, null, 2),
	);

	expect(computed.verdict).toBe("computed");
	expect(computed.refreshLogRow.success).toBe(true);
	expect(skipped.refreshLogRow.success).toBe(true);
	if (canSkipRounds(skipped.refreshLogRow)) {
		expect(skipped.verdict).toBe("skipped");
	}
});
