import { formatLocalDate } from "../../tests/helpers/dates";

export const teamStates = {
	toDo: ["New"],
	doing: ["Active"],
	done: ["Closed"],
};
export const teamWorkItemTypes = ["User Story", "Bug"];

export const portfolioStates = {
	toDo: ["New"],
	doing: ["Active"],
	done: ["Done"],
};
export const portfolioWorkItemTypes = ["Epic"];

const teams = [
	{ name: "Atlas", prefix: "ATL" },
	{ name: "Borealis", prefix: "BOR" },
	{ name: "Cobalt", prefix: "COB" },
	{ name: "Delta Ops", prefix: "DEL" },
	{ name: "Ember", prefix: "EMB" },
	{ name: "Fjord", prefix: "FJO" },
	{ name: "Granite", prefix: "GRA" },
	{ name: "Harbour", prefix: "HAR" },
];

const scalePointPortfolios = [
	{ name: "Northwind Platform", prefix: "NP", featureCount: 920 },
	{ name: "Northwind Payments", prefix: "NPAY", featureCount: 400 },
	{ name: "Northwind Mobile", prefix: "NMOB", featureCount: 100 },
];

// These only bring the instance to about 3,000 Features, so a page that loads every Feature on the instance
// feels the size of a large customer. Off by default: with them, seeding took over an hour and a half.
const fillerPortfolios = [
	{ name: "Northwind Analytics", prefix: "NANA", featureCount: 540 },
	{ name: "Northwind Retail", prefix: "NRET", featureCount: 520 },
	{ name: "Northwind Logistics", prefix: "NLOG", featureCount: 520 },
];

export function scaledSetPortfolios(withFillers: boolean) {
	return withFillers
		? [...scalePointPortfolios, ...fillerPortfolios]
		: scalePointPortfolios;
}

export const scaledSetTeamNames = teams.map((team) => team.name);

const teamHeader =
	"ID,Name,State,Type,Started Date,Closed Date,Created Date,Parent Reference Id";
const portfolioHeader =
	"ID,Name,State,Type,Started Date,Closed Date,Created Date,Parent Reference Id,Owning Team,Estimated Size,Depends On";

const historyDays = 90;
const mostRemainingChildren = 15;
const mostTeamsPerFeature = 3;
const parentsPerPortfolio = 20;

export type ScaledSetCsv = {
	teams: {
		name: string;
		csv: string;
		remainingItems: number;
		closedItems: number;
	}[];
	portfolios: {
		name: string;
		csv: string;
		features: number;
		withDependencies: number;
		withParent: number;
	}[];
};

type Feature = { id: string; active: boolean; teams: number[] };

class Draws {
	private state: number;

	constructor(seed: number) {
		this.state = seed;
	}

	// mulberry32: a fixed seed gives every instance the same set, so main and the release tag are measured
	// on identical data.
	next(): number {
		this.state = (this.state + 0x6d2b79f5) | 0;
		let value = Math.imul(this.state ^ (this.state >>> 15), 1 | this.state);
		value ^= value + Math.imul(value ^ (value >>> 7), 61 | value);
		return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
	}

	upTo(most: number): number {
		return Math.floor(this.next() * (most + 1));
	}

	between(least: number, most: number): number {
		return least + this.upTo(most - least);
	}

	chance(probability: number): boolean {
		return this.next() < probability;
	}

	pick<T>(items: T[]): T {
		return items[Math.floor(this.next() * items.length)];
	}
}

function daysBefore(today: Date, days: number): string {
	return formatLocalDate(
		new Date(today.getTime() - days * 24 * 60 * 60 * 1000),
	);
}

function pad(value: number): string {
	return String(value).padStart(4, "0");
}

export function generateScaledSet(
	today: Date,
	withFillers: boolean,
	seed = 6400,
): ScaledSetCsv {
	const portfolioSet = scaledSetPortfolios(withFillers);
	const draws = new Draws(seed);
	const teamRows: string[][] = teams.map(() => []);
	const teamCounters = teams.map(() => 0);
	const remainingPerTeam = teams.map(() => 0);
	const activeFeaturesPerTeam: string[][] = teams.map(() => []);

	const nextTeamItemId = (teamIndex: number) => {
		teamCounters[teamIndex] += 1;
		return `${teams[teamIndex].prefix}-${pad(teamCounters[teamIndex])}`;
	};

	const featuresPerPortfolio: Feature[][] = portfolioSet.map((portfolio) =>
		Array.from({ length: portfolio.featureCount }, (_, index) => {
			const teamCount = draws.between(1, mostTeamsPerFeature);
			const chosenTeams = new Set<number>();
			while (chosenTeams.size < teamCount) {
				chosenTeams.add(draws.upTo(teams.length - 1));
			}
			return {
				id: `${portfolio.prefix}-${pad(index + 1)}`,
				active: draws.chance(0.35),
				teams: [...chosenTeams],
			};
		}),
	);

	const portfolios = portfolioSet.map((portfolio, portfolioIndex) => {
		const rows: string[] = [portfolioHeader];
		let withDependencies = 0;
		let withParent = 0;

		for (const [index, feature] of featuresPerPortfolio[
			portfolioIndex
		].entries()) {
			const created = daysBefore(today, draws.between(60, 240));
			const started = feature.active
				? daysBefore(today, draws.between(1, 60))
				: "";
			const parent = draws.chance(0.5)
				? `${portfolio.prefix}-INIT-${draws.between(1, parentsPerPortfolio)}`
				: "";
			if (parent) {
				withParent += 1;
			}

			const dependsOn = dependenciesOf(
				draws,
				featuresPerPortfolio,
				portfolioIndex,
				feature.id,
			);
			if (dependsOn) {
				withDependencies += 1;
			}

			rows.push(
				[
					feature.id,
					`${portfolio.name} Feature ${pad(index + 1)}`,
					feature.active ? "Active" : "New",
					"Epic",
					started,
					"",
					created,
					parent,
					"",
					String(draws.between(3, 21)),
					dependsOn,
				].join(","),
			);

			const children = draws.upTo(mostRemainingChildren);
			for (let child = 0; child < children; child++) {
				const teamIndex = feature.teams[child % feature.teams.length];
				const childActive = feature.active && draws.chance(0.3);
				teamRows[teamIndex].push(
					[
						nextTeamItemId(teamIndex),
						`Story ${child + 1} of ${feature.id}`,
						childActive ? "Active" : "New",
						"User Story",
						childActive ? daysBefore(today, draws.between(1, 20)) : "",
						"",
						daysBefore(today, draws.between(20, 60)),
						feature.id,
					].join(","),
				);
				remainingPerTeam[teamIndex] += 1;
			}

			if (feature.active) {
				for (const teamIndex of feature.teams) {
					activeFeaturesPerTeam[teamIndex].push(feature.id);
				}
			}
		}

		return {
			name: portfolio.name,
			csv: rows.join("\n"),
			features: portfolio.featureCount,
			withDependencies,
			withParent,
		};
	});

	const teamCsv = teams.map((team, teamIndex) => {
		let closedItems = 0;
		for (let day = historyDays; day >= 1; day--) {
			const closedThatDay = draws.upTo(3);
			for (let item = 0; item < closedThatDay; item++) {
				const parents = activeFeaturesPerTeam[teamIndex];
				teamRows[teamIndex].push(
					[
						nextTeamItemId(teamIndex),
						`Closed work ${closedItems + 1} of ${team.name}`,
						"Closed",
						draws.chance(0.2) ? "Bug" : "User Story",
						daysBefore(today, day + draws.between(1, 10)),
						daysBefore(today, day),
						daysBefore(today, day + draws.between(11, 20)),
						parents.length > 0 ? draws.pick(parents) : "",
					].join(","),
				);
				closedItems += 1;
			}
		}

		return {
			name: team.name,
			csv: [teamHeader, ...teamRows[teamIndex]].join("\n"),
			remainingItems: remainingPerTeam[teamIndex],
			closedItems,
		};
	});

	return { teams: teamCsv, portfolios };
}

// About one Feature in ten waits on one or two others, and about a quarter of those on a Feature of another
// Portfolio, because a dependency across Portfolios is what makes a page reach beyond the one it shows.
function dependenciesOf(
	draws: Draws,
	featuresPerPortfolio: Feature[][],
	portfolioIndex: number,
	ownId: string,
): string {
	if (!draws.chance(0.1)) {
		return "";
	}

	const references = new Set<string>();
	const count = draws.between(1, 2);
	while (references.size < count) {
		const sourceIndex = draws.chance(0.25)
			? draws.upTo(featuresPerPortfolio.length - 1)
			: portfolioIndex;
		const candidate = draws.pick(featuresPerPortfolio[sourceIndex]).id;
		if (candidate !== ownId) {
			references.add(candidate);
		}
	}

	return [...references].join(";");
}
