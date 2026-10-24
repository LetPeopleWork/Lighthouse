import * as fs from "node:fs";
import * as os from "node:os";
import path from "node:path";
import {
	type APIRequestContext,
	type Browser,
	type BrowserContext,
	type Page,
	request,
	test,
} from "@playwright/test";
import type { RefreshLogRow } from "./helpers/apiVersionMap";
import {
	type BackendLog,
	type CounterSample,
	formatNumber,
	type MachineLoad,
	markdownTable,
	median,
	parseBackendLog,
	readCounters,
	readLogRecords,
	scriptingAndRendering,
	waitForQuietMachine,
} from "./helpers/collect";
import {
	type RoundReading,
	readRefreshLog,
	startComputedRound,
	startSkippedRound,
	waitUntilNothingIsQueued,
} from "./helpers/rounds";
import {
	type BackendSettings,
	classify,
	cpuStatOf,
	databaseShareOfRequests,
	figuresOf,
	type RoundInterval,
	type RunningBackend,
	startBackend,
	stopBackend,
	type WindowFigures,
	type WindowName,
	type WindowReading,
	windowPlans,
} from "./helpers/windows";
import { perfOutputDirectory } from "./paths";

type OpenName = "overview" | "settings" | "portfolio" | "team";
type StateName = "idle" | "computed" | "skipped";

const environment = process.env;
const list = (value: string) =>
	value
		.split(",")
		.map((item) => item.trim())
		.filter(Boolean);
const contentRoot =
	environment.PERF_BACKEND_DIR ??
	path.join(__dirname, "..", "..", "Lighthouse.Backend", "Lighthouse.Backend");
const scalePoints = list(
	environment.PERF_SCALE ?? environment.PERF_SCALE_POINTS ?? "100,400,920",
).map(Number);

const settings = {
	provider: environment.PERF_PROVIDER ?? "sqlite",
	version: environment.PERF_VERSION ?? "main",
	contentRoot,
	executable:
		environment.PERF_BACKEND_EXECUTABLE ??
		path.join(
			contentRoot,
			"bin",
			"Debug",
			"net10.0",
			"linux-x64",
			"Lighthouse",
		),
	seededDatabase: environment.PERF_SEEDED_DB,
	connectionString: environment.PERF_CONNECTION_STRING,
	keysFolder: environment.PERF_KEYS_DIR,
	port: Number(environment.PERF_PORT ?? 5421),
	scalePoints,
	states: list(
		environment.PERF_STATES ?? "idle,computed,skipped",
	) as StateName[],
	opens: list(
		environment.PERF_OPENS ?? "overview,settings,portfolio,team",
	) as OpenName[],
	repetitions: Number(
		environment.PERF_REPEATS ?? environment.PERF_REPETITIONS ?? 5,
	),
	warmUps: Number(environment.PERF_WARMUPS ?? 1),
	windows: list(environment.PERF_WINDOWS ?? "T,R,RT,C,C0") as WindowName[],
	windowSeconds: Number(environment.PERF_WINDOW_SECONDS ?? 300),
	windowScale: Number(
		environment.PERF_WINDOW_SCALE ?? Math.max(...scalePoints),
	),
	minThreads: Number(environment.PERF_MIN_THREADS ?? 64),
	processorCount: environment.PERF_PROCESSOR_COUNT ?? "4",
	launchPrefix: list(
		(environment.PERF_LAUNCH_PREFIX ?? "").replaceAll(" ", ","),
	),
	team: environment.PERF_TEAM,
	maxLoad: Number(environment.PERF_MAX_LOAD ?? 6),
	loadWaitSeconds: Number(environment.PERF_LOAD_WAIT_SECONDS ?? 600),
	roundDrainSeconds: Number(environment.PERF_ROUND_DRAIN_SECONDS ?? 900),
	countersTool:
		environment.PERF_COUNTERS ??
		path.join(os.homedir(), ".dotnet", "tools", "dotnet-counters"),
	fillers: environment.PERF_FILLERS === "1",
};

// Seeds the two records a returning browser carries, so neither the usage-data ask nor an update notice covers
// the page, then records paint, long tasks, the last click and the moment each awaited element first appears.
const pageProbe = `
try {
	localStorage.setItem("lighthouse:usagedata:asked", new Date().toISOString());
	localStorage.setItem("lighthouse-hide-all-update-notifications", "true");
} catch {}
const probe = { longTasks: [], marks: {}, fcp: undefined, click: undefined };
window.__perf = probe;
new PerformanceObserver((list) => {
	for (const entry of list.getEntries()) probe.longTasks.push({ start: entry.startTime, duration: entry.duration });
}).observe({ type: "longtask", buffered: true });
new PerformanceObserver((list) => {
	for (const entry of list.getEntries()) if (entry.name === "first-contentful-paint") probe.fcp = entry.startTime;
}).observe({ type: "paint", buffered: true });
document.addEventListener("click", () => { if (probe.click === undefined) probe.click = performance.now(); }, true);
const watched = {
	settingsUsable: '[data-testid="configuration-panel"] [data-testid^="refresh-interval-"]',
	firstFeatureRow: '.MuiDataGrid-row[data-rowindex]',
};
const check = () => {
	for (const [name, selector] of Object.entries(watched)) {
		if (probe.marks[name] === undefined && document.querySelector(selector)) probe.marks[name] = performance.now();
	}
};
new MutationObserver(check).observe(document, { childList: true, subtree: true, attributes: true });
`;

type ProbeState = {
	longTasks: { start: number; duration: number }[];
	marks: Record<string, number | undefined>;
	fcp?: number;
	click?: number;
};

type RequestSample = {
	method: string;
	pathAndQuery: string;
	status: number;
	bytes: number;
	startedAt: number;
};

type OpenSample = {
	open: OpenName;
	scale: number;
	state: StateName | WindowName;
	repetition: "warm-up" | "measured" | "trace";
	backend: string;
	startedAt: number;
	finishedAt: number;
	primaryMs: number;
	fcpMs?: number;
	longTaskCount: number;
	longTaskMs: number;
	requests: RequestSample[];
	roundActiveAtStart: boolean;
	roundActiveAtEnd: boolean;
	activeTasks: string;
	traceFile?: string;
};

type RoundRecord = {
	context: string;
	portfolio: string;
	verdict: string;
	startedAt: number;
	finishedAt: number;
	durationMs: number;
	success: boolean;
};

type Targets = {
	portfolioId: number;
	portfolioName: string;
	teamId: number;
	teamName: string;
};

type BlockLoad = { label: string; machine: MachineLoad };

class Run {
	readonly startedAt = Date.now();
	readonly folder: string;
	readonly samples: OpenSample[] = [];
	readonly rounds: RoundRecord[] = [];
	readonly windows: WindowReading[] = [];
	readonly blockLoads: BlockLoad[] = [];
	readonly refreshLog = new Map<number, RefreshLogRow>();
	readonly backends: RunningBackend[] = [];
	readonly backendSettings: BackendSettings;

	constructor() {
		const stamp = new Date().toISOString().replaceAll(/[:.]/g, "-");
		this.folder = path.join(
			perfOutputDirectory,
			"runs",
			`${stamp}-${settings.provider}-${settings.version}`,
		);
		fs.mkdirSync(path.join(this.folder, "traces"), { recursive: true });
		this.backendSettings = {
			contentRoot: settings.contentRoot,
			executable: settings.executable,
			port: settings.port,
			provider: settings.provider,
			connectionString: this.prepareDatabase(),
			keysFolder: this.prepareKeys(),
			processorCount: settings.processorCount,
			launchPrefix: settings.launchPrefix,
			countersTool: settings.countersTool,
		};
	}

	/** Works on a copy, so a run's rounds never change the seeded set the next run starts from. */
	private prepareDatabase(): string {
		if (settings.provider !== "sqlite") {
			if (!settings.connectionString) {
				throw new Error("PERF_CONNECTION_STRING is required for PostgreSQL");
			}
			return settings.connectionString;
		}
		if (!settings.seededDatabase || !fs.existsSync(settings.seededDatabase)) {
			throw new Error("PERF_SEEDED_DB must name the seeded SQLite file");
		}
		const target = path.join(this.folder, "db", "lighthouse.db");
		fs.mkdirSync(path.dirname(target), { recursive: true });
		for (const suffix of ["", "-wal", "-shm"]) {
			if (fs.existsSync(settings.seededDatabase + suffix)) {
				fs.copyFileSync(settings.seededDatabase + suffix, target + suffix);
			}
		}
		return `Data Source=${target}`;
	}

	private prepareKeys(): string {
		const target = path.join(this.folder, "keys");
		const source =
			settings.keysFolder ??
			(settings.seededDatabase
				? path.join(path.dirname(settings.seededDatabase), "keys")
				: undefined);
		if (source && fs.existsSync(source)) {
			fs.cpSync(source, target, { recursive: true });
		} else {
			fs.mkdirSync(target, { recursive: true });
		}
		return target;
	}

	ownProcessIds(): Set<number> {
		const ids = new Set([process.pid]);
		for (const backend of this.backends) {
			ids.add(backend.process.pid as number);
			ids.add(backend.counters.pid as number);
		}
		return ids;
	}

	async quietMachine(label: string): Promise<MachineLoad> {
		const machine = await waitForQuietMachine(
			settings.maxLoad,
			settings.loadWaitSeconds,
			this.ownProcessIds(),
		);
		this.blockLoads.push({ label, machine });
		return machine;
	}

	async start(label: string, minThreads?: number): Promise<RunningBackend> {
		const backend = await startBackend(
			this.backendSettings,
			this.folder,
			label,
			minThreads,
		);
		this.backends.push(backend);
		return backend;
	}

	async stop(backend: RunningBackend, api: APIRequestContext): Promise<void> {
		try {
			for (const row of await readRefreshLog(api)) {
				this.refreshLog.set(row.id, row);
			}
		} finally {
			await stopBackend(backend);
		}
	}

	record(context: string, portfolio: string, round: RoundReading): void {
		this.rounds.push({
			context,
			portfolio,
			verdict: round.verdict,
			startedAt: round.startedAt.getTime(),
			finishedAt: round.finishedAt.getTime(),
			durationMs: round.refreshLogRow.durationMs,
			success: round.refreshLogRow.success,
		});
	}
}

type Feature = { id: number; name: string; features: unknown[] };

async function readTargets(
	api: APIRequestContext,
	scale: number,
): Promise<Targets> {
	const portfolios = (await (
		await api.get("/api/latest/portfolios")
	).json()) as Feature[];
	const portfolio = portfolios.find((p) => p.features.length === scale);
	if (!portfolio) {
		throw new Error(
			`No Portfolio holds exactly ${scale} Features: ${portfolios.map((p) => `${p.name}=${p.features.length}`).join(", ")}`,
		);
	}
	const teams = (await (
		await api.get("/api/latest/teams")
	).json()) as Feature[];
	const team =
		teams.find((t) => t.name === settings.team) ??
		[...teams].sort((a, b) => b.features.length - a.features.length)[0];
	return {
		portfolioId: portfolio.id,
		portfolioName: portfolio.name,
		teamId: team.id,
		teamName: team.name,
	};
}

async function updateStatus(
	api: APIRequestContext,
): Promise<{ active: boolean; tasks: string }> {
	const status = await (await api.get("/api/latest/update/status")).json();
	const tasks = (await (await api.get("/api/latest/update/tasks")).json()) as {
		updateType: string | number;
	}[];
	const counts = new Map<string, number>();
	for (const task of tasks) {
		const type = String(task.updateType);
		counts.set(type, (counts.get(type) ?? 0) + 1);
	}
	return {
		active: status.hasActiveUpdates === true,
		tasks:
			[...counts.entries()].map(([type, n]) => `${type}×${n}`).join(" ") ||
			"none",
	};
}

/** Keeps one round of the state's kind running, starting the next as soon as the last one finishes. */
class RoundKeeper {
	private pending: Promise<void> | undefined;

	constructor(
		private readonly run: Run,
		private readonly api: APIRequestContext,
		private readonly kind: "computed" | "skipped",
		private readonly targets: Targets,
		private readonly context: string,
	) {}

	async ensureRunning(): Promise<void> {
		if (this.pending) {
			return;
		}
		const start =
			this.kind === "computed" ? startComputedRound : startSkippedRound;
		this.pending = start(this.api, this.targets.portfolioId)
			.then((round) =>
				this.run.record(this.context, this.targets.portfolioName, round),
			)
			.catch((error) => {
				console.warn(`${this.context}: round failed: ${error}`);
			})
			.finally(() => {
				this.pending = undefined;
			});
		const deadline = Date.now() + 180_000;
		while (Date.now() < deadline && this.pending) {
			if ((await updateStatus(this.api)).active) {
				return;
			}
			await new Promise((resolve) => setTimeout(resolve, 250));
		}
	}

	async drain(seconds: number): Promise<void> {
		if (this.pending) {
			await Promise.race([
				this.pending,
				new Promise((resolve) => setTimeout(resolve, seconds * 1000)),
			]);
		}
	}
}

async function newBrowserContext(
	browser: Browser,
	baseURL: string,
): Promise<BrowserContext> {
	const context = await browser.newContext({
		baseURL,
		viewport: { width: 1920, height: 1080 },
		timezoneId: "Europe/Zurich",
	});
	await context.addInitScript({ content: pageProbe });
	return context;
}

const markTimeout = 5 * 60_000;

// A request the page abandoned (the server logs 499) never reports its sizes, so every such question is capped.
function within<T>(promise: Promise<T>, fallback: T): Promise<T> {
	return Promise.race([
		promise.catch(() => fallback),
		new Promise<T>((resolve) => setTimeout(() => resolve(fallback), 5_000)),
	]);
}

async function waitForMark(page: Page, condition: string): Promise<void> {
	await page.waitForFunction(condition, undefined, {
		timeout: markTimeout,
		polling: 50,
	});
}

async function takeOpen(
	browser: Browser,
	context: BrowserContext,
	api: APIRequestContext,
	open: OpenName,
	targets: Targets,
	traceFile?: string,
): Promise<Omit<OpenSample, "scale" | "state" | "repetition" | "backend">> {
	const before = await updateStatus(api);
	const page = await context.newPage();
	const requests: RequestSample[] = [];
	const pending: Promise<void>[] = [];
	page.on("requestfinished", (finished) => {
		pending.push(
			(async () => {
				const sizes = await within(finished.sizes(), undefined);
				const response = await within(finished.response(), null);
				const url = new URL(finished.url());
				requests.push({
					method: finished.method(),
					pathAndQuery: url.pathname + url.search,
					status: response?.status() ?? 0,
					bytes: sizes ? sizes.responseBodySize + sizes.responseHeadersSize : 0,
					startedAt: finished.timing().startTime,
				});
			})(),
		);
	});
	const startedAt = Date.now();
	if (traceFile) {
		// Only what the scripting and rendering split reads; the default categories write about 100 MB per open.
		await browser.startTracing(page, {
			path: traceFile,
			screenshots: false,
			categories: ["devtools.timeline", "v8.execute"],
		});
	}
	let primaryMs: number;
	if (open === "overview") {
		await page.goto("/");
		await waitForMark(page, "window.__perf.fcp !== undefined");
		primaryMs = await page.evaluate("window.__perf.fcp");
	} else if (open === "settings") {
		await page.goto("/");
		const navigation = page.getByTestId("main-navigation");
		await navigation.waitFor({ timeout: markTimeout });
		await page.getByRole("link", { name: "Settings" }).click();
		await waitForMark(page, "window.__perf.marks.settingsUsable !== undefined");
		primaryMs = await page.evaluate(
			"window.__perf.marks.settingsUsable - window.__perf.click",
		);
	} else {
		const route =
			open === "portfolio"
				? `/portfolios/${targets.portfolioId}/features`
				: `/teams/${targets.teamId}/features`;
		await page.goto(route);
		await waitForMark(
			page,
			"window.__perf.marks.firstFeatureRow !== undefined",
		);
		primaryMs = await page.evaluate("window.__perf.marks.firstFeatureRow");
	}
	await page
		.waitForLoadState("networkidle", { timeout: 30_000 })
		.catch(() => undefined);
	if (traceFile) {
		await browser.stopTracing();
	}
	const probe = (await page.evaluate("window.__perf")) as ProbeState;
	const finishedAt = Date.now();
	await Promise.all(pending);
	await page.close();
	const after = await updateStatus(api);
	const longTasks =
		open === "settings"
			? probe.longTasks.filter((task) => task.start >= (probe.click ?? 0))
			: probe.longTasks;
	return {
		open,
		startedAt,
		finishedAt,
		primaryMs,
		fcpMs: open === "settings" ? undefined : probe.fcp,
		longTaskCount: longTasks.length,
		longTaskMs: longTasks.reduce((sum, task) => sum + task.duration, 0),
		requests: requests.sort((a, b) => a.startedAt - b.startedAt),
		roundActiveAtStart: before.active,
		roundActiveAtEnd: after.active,
		activeTasks: before.tasks,
		traceFile,
	};
}

async function readStateBlock(
	run: Run,
	browser: Browser,
	backend: RunningBackend,
	api: APIRequestContext,
	scale: number,
	state: StateName,
	targets: Targets,
): Promise<void> {
	await run.quietMachine(`${scale} ${state}`);
	const context = await newBrowserContext(browser, backend.baseURL);
	const keeper =
		state === "idle"
			? undefined
			: new RoundKeeper(run, api, state, targets, `${scale} ${state}`);
	try {
		for (const open of settings.opens) {
			const repetitions: OpenSample["repetition"][] = [
				...Array<OpenSample["repetition"]>(settings.warmUps).fill("warm-up"),
				...Array<OpenSample["repetition"]>(settings.repetitions).fill(
					"measured",
				),
				"trace",
			];
			for (const repetition of repetitions) {
				await keeper?.ensureRunning();
				const traceFile =
					repetition === "trace"
						? path.join(run.folder, "traces", `${open}-${scale}-${state}.trace`)
						: undefined;
				const sample = await takeOpen(
					browser,
					context,
					api,
					open,
					targets,
					traceFile,
				);
				run.samples.push({
					...sample,
					scale,
					state,
					repetition,
					backend: backend.label,
				});
			}
		}
	} finally {
		await context.close();
		await keeper?.drain(settings.roundDrainSeconds);
	}
}

async function runWindow(
	run: Run,
	browser: Browser,
	backend: RunningBackend,
	api: APIRequestContext,
	name: WindowName,
	targets: Targets,
): Promise<void> {
	const plan = windowPlans[name];
	await waitUntilNothingIsQueued(api, settings.roundDrainSeconds * 1000);
	const machine = await run.quietMachine(`window ${name}`);
	const context = await newBrowserContext(browser, backend.baseURL);
	const cpuBefore = cpuStatOf(backend);
	const startedAt = Date.now();
	const deadline = startedAt + settings.windowSeconds * 1000;
	const rounds: RoundInterval[] = [];
	let opens = 0;

	const traffic = async () => {
		while (Date.now() < deadline) {
			for (const open of settings.opens) {
				if (Date.now() >= deadline) {
					break;
				}
				const sample = await takeOpen(browser, context, api, open, targets);
				run.samples.push({
					...sample,
					scale: settings.windowScale,
					state: name,
					repetition: "measured",
					backend: backend.label,
				});
				opens++;
			}
		}
	};
	const roundsLoop = async () => {
		while (Date.now() < deadline) {
			const roundStartedAt = Date.now();
			const round = await startComputedRound(api, targets.portfolioId);
			rounds.push({ startedAt: roundStartedAt, finishedAt: Date.now() });
			run.record(`window ${name}`, targets.portfolioName, round);
		}
	};
	try {
		await Promise.all([
			plan.traffic ? traffic() : Promise.resolve(),
			plan.rounds ? roundsLoop() : Promise.resolve(),
		]);
	} finally {
		await context.close();
	}
	run.windows.push({
		name,
		minThreads: backend.minThreads,
		startedAt,
		finishedAt: Date.now(),
		machine,
		cpuBefore,
		cpuAfter: cpuStatOf(backend),
		opens,
		rounds,
	});
}

type Parsed = { log: BackendLog; counters: CounterSample[] };

function parseBackends(run: Run): Map<string, Parsed> {
	const parsed = new Map<string, Parsed>();
	for (const backend of run.backends) {
		parsed.set(backend.label, {
			log: parseBackendLog(readLogRecords(backend.logFolder)),
			counters: readCounters(backend.countersFile),
		});
	}
	return parsed;
}

function serverSplit(
	sample: OpenSample,
	log: BackendLog,
): { serverMs: number; databaseMs: number } {
	const wanted = new Set(
		sample.requests
			.filter((r) => r.pathAndQuery.startsWith("/api/"))
			.map((r) => r.pathAndQuery),
	);
	const finished = log.finishedRequests.filter(
		(r) =>
			wanted.has(r.pathAndQuery) &&
			r.at >= sample.startedAt - 500 &&
			r.at <= sample.finishedAt + 500,
	);
	const ids = new Set(finished.map((r) => r.requestId));
	return {
		serverMs: finished.reduce((sum, r) => sum + r.milliseconds, 0),
		databaseMs: log.databaseCommands
			.filter((c) => c.requestId !== "" && ids.has(c.requestId))
			.reduce((sum, c) => sum + c.milliseconds, 0),
	};
}

const clock = (at: number) => new Date(at).toLocaleTimeString("en-GB");
const kilobytes = (requests: RequestSample[]) =>
	requests.reduce((sum, r) => sum + r.bytes, 0) / 1024;

function opensSection(run: Run, parsed: Map<string, Parsed>): string[] {
	const rows: string[][] = [];
	const orders: string[][] = [];
	for (const scale of settings.scalePoints) {
		for (const state of settings.states) {
			for (const open of settings.opens) {
				const block = run.samples.filter(
					(s) => s.open === open && s.scale === scale && s.state === state,
				);
				const measured = block.filter((s) => s.repetition === "measured");
				// Without a warm-up, the first measured open is the cold one.
				const warmUp =
					block.find((s) => s.repetition === "warm-up") ?? measured[0];
				const trace = block.find((s) => s.repetition === "trace");
				if (measured.length === 0) {
					continue;
				}
				const splits = measured.map((s) =>
					serverSplit(s, (parsed.get(s.backend) as Parsed).log),
				);
				const primary = measured.map((s) => s.primaryMs);
				const traced = trace?.traceFile
					? scriptingAndRendering(trace.traceFile)
					: undefined;
				const load = run.blockLoads.find(
					(b) => b.label === `${scale} ${state}`,
				)?.machine;
				rows.push([
					open,
					String(scale),
					state,
					formatNumber(warmUp?.primaryMs),
					formatNumber(median(primary)),
					`${formatNumber(Math.min(...primary))}–${formatNumber(Math.max(...primary))}`,
					open === "settings"
						? "n/a (soft navigation)"
						: formatNumber(median(measured.map((s) => s.fcpMs ?? Number.NaN))),
					formatNumber(median(measured.map((s) => s.requests.length))),
					formatNumber(
						median(
							measured.map(
								(s) =>
									s.requests.filter((r) => r.pathAndQuery.startsWith("/api/"))
										.length,
							),
						),
					),
					formatNumber(median(measured.map((s) => kilobytes(s.requests))), 1),
					formatNumber(median(splits.map((s) => s.serverMs))),
					formatNumber(median(splits.map((s) => s.databaseMs))),
					`${formatNumber(median(measured.map((s) => s.longTaskCount)))} / ${formatNumber(median(measured.map((s) => s.longTaskMs)))}`,
					formatNumber(traced?.scriptingMs),
					formatNumber(traced?.renderingMs),
					`${measured.filter((s) => s.roundActiveAtStart && s.roundActiveAtEnd).length}/${measured.length}`,
					measured[measured.length - 1].activeTasks,
					load ? formatNumber(load.load1, 2) : "n/a",
					load ? (load.disturbed ? "disturbed" : "quiet") : "n/a",
				]);
				const last = measured[measured.length - 1];
				orders.push([
					open,
					String(scale),
					state,
					last.requests
						.filter((r) => r.pathAndQuery.startsWith("/api/"))
						.map(
							(r) =>
								`${r.method} ${r.pathAndQuery} (${r.status}, ${(r.bytes / 1024).toFixed(1)} KB)`,
						)
						.join(" → "),
				]);
			}
		}
	}
	return [
		"## Opens",
		"",
		`Median of ${settings.repetitions} after ${settings.warmUps} warm-up(s); the first open of the idle block is the cold start after the backend started. Primary mark: Overview = first contentful paint, Settings = configuration usable after the click from the Overview, Portfolio / Team = first Feature row; all in ms. Server and EF columns are the sums over the open's API requests (median over repetitions). Scripting and rendering come from one Chrome trace per open, taken on an extra repetition so tracing never slows the measured ones.`,
		"",
		markdownTable(
			[
				"open",
				"scale",
				"state",
				"warm-up / cold ms",
				"median ms",
				"min–max ms",
				"FCP ms",
				"requests",
				"API requests",
				"KB",
				"server ms",
				"EF ms",
				"long tasks n / ms",
				"scripting ms",
				"rendering ms",
				"inside a round",
				"update tasks at start",
				"load1",
				"machine",
			],
			rows,
		),
		"",
		"## Request order (last measured repetition)",
		"",
		markdownTable(["open", "scale", "state", "API requests in order"], orders),
		"",
	];
}

function windowsSection(run: Run, parsed: Map<string, Parsed>): string[] {
	const figures: Partial<Record<WindowName, WindowFigures>> = {};
	const rows: string[][] = [];
	let databaseShare = Number.NaN;
	for (const window of run.windows) {
		const backend = run.backends.find(
			(b) =>
				b.minThreads === window.minThreads && b.label.startsWith("windows"),
		) as RunningBackend;
		const { log, counters } = parsed.get(backend.label) as Parsed;
		const figure = figuresOf(window, log, counters);
		figures[window.name] = figure;
		if (window.name === "RT") {
			databaseShare = databaseShareOfRequests(window, log);
		}
		rows.push([
			window.name,
			window.minThreads === undefined ? "default" : String(window.minThreads),
			clock(window.startedAt),
			formatNumber((window.finishedAt - window.startedAt) / 1000),
			formatNumber(window.machine.load1, 2),
			window.machine.foreignTests.length === 0
				? "none"
				: window.machine.foreignTests.join("; "),
			window.machine.disturbed
				? `disturbed (waited ${window.machine.waitedSeconds} s)`
				: `quiet (waited ${window.machine.waitedSeconds} s)`,
			String(figure.heartbeatWarnings),
			formatNumber(figure.heartbeatDelayMs),
			formatNumber(figure.queueIntegral),
			String(figure.maxThreads),
			`${formatNumber(figure.gcPauseShare * 100, 2)}%`,
			Number.isNaN(figure.gcPauseShareAroundWarnings)
				? "n/a (no warnings)"
				: `${formatNumber(figure.gcPauseShareAroundWarnings * 100, 1)}%`,
			formatNumber(figure.gen2Collections),
			`${formatNumber(figure.throttledShare * 100, 1)}% (${figure.throttledPeriods})`,
			String(window.rounds.length),
			String(window.opens),
		]);
	}
	if (run.windows.length === 0) {
		return [];
	}
	const verdict = classify(figures, databaseShare);
	return [
		"## Windows",
		"",
		`Each window ${settings.windowSeconds} s planned; it ends when its last open or round ends. Heartbeat delay is the sum of the durations Kestrel reported; the pool queue integral is the sum of the per-second queue length samples (work-item seconds); throttled periods come from the backend's cgroup cpu.stat.`,
		"",
		markdownTable(
			[
				"window",
				"min threads",
				"start",
				"seconds",
				"load1",
				"foreign tests",
				"machine",
				"heartbeat warnings",
				"heartbeat delay ms",
				"pool queue integral",
				"max pool threads",
				"GC pause share",
				"GC share ±1 s of warnings",
				"gen 2",
				"throttled",
				"rounds",
				"opens",
			],
			rows,
		),
		"",
		"## Ratios and classification",
		"",
		markdownTable(
			["reading", "value"],
			[
				[
					"Q_r heartbeat delay = R ÷ RT",
					formatNumber(verdict.roundRatioHeartbeat, 2),
				],
				["Q_r pool queue = R ÷ RT", formatNumber(verdict.roundRatioQueue, 2)],
				[
					"S_q = EF ÷ request time inside RT's rounds",
					formatNumber(verdict.databaseShare, 3),
				],
				["pool starvation", verdict.poolStarvation],
				["GC", verdict.garbageCollection],
				["CPU quota", verdict.cpuQuota],
			],
		),
		"",
	];
}

function roundsSection(run: Run, parsed: Map<string, Parsed>): string[] {
	const splits: string[][] = [];
	const summaries: string[][] = [];
	for (const [label, { log }] of parsed) {
		for (const split of log.forecastSplits) {
			splits.push([
				label,
				clock(split.at),
				split.portfolio,
				String(split.gather),
				String(split.simulate),
				String(split.save),
				String(split.publish),
			]);
		}
		for (const summary of log.roundSummaries) {
			summaries.push([
				label,
				clock(summary.at),
				summary.portfolio,
				summary.summary,
			]);
		}
	}
	const refreshTypes = ["Team", "Portfolio", "Forecast"];
	const history = [...run.refreshLog.values()]
		.filter((row) => Date.parse(row.executedAt) >= run.startedAt)
		.sort((a, b) => a.id - b.id)
		.map((row) => [
			clock(Date.parse(row.executedAt)),
			typeof row.type === "number" ? refreshTypes[row.type] : row.type,
			row.entityName,
			String(row.itemCount),
			String(row.durationMs),
			row.success ? "yes" : "no",
			row.cancelled ? "yes" : "no",
			row.skipped === undefined ? "n/a" : row.skipped ? "yes" : "no",
		]);
	return [
		"## Rounds the harness started",
		"",
		markdownTable(
			[
				"context",
				"Portfolio",
				"verdict",
				"start",
				"end",
				"Refresh History ms",
				"success",
			],
			run.rounds.map((r) => [
				r.context,
				r.portfolio,
				r.verdict,
				clock(r.startedAt),
				clock(r.finishedAt),
				String(r.durationMs),
				r.success ? "yes" : "no",
			]),
		),
		"",
		"## Forecast round split (ForecastUpdater Debug line)",
		"",
		markdownTable(
			[
				"backend",
				"time",
				"Portfolio",
				"gather ms",
				"simulate ms",
				"save ms",
				"publish ms",
			],
			splits,
		),
		"",
		"## Round summary lines",
		"",
		markdownTable(["backend", "time", "Portfolio", "summary"], summaries),
		"",
		"## Refresh History during the run",
		"",
		markdownTable(
			[
				"executed",
				"type",
				"entity",
				"items",
				"duration ms",
				"success",
				"cancelled",
				"skipped",
			],
			history,
		),
		"",
	];
}

function writeResults(run: Run): string {
	const parsed = parseBackends(run);
	const header = [
		`# Harness run ${new Date(run.startedAt).toISOString()}`,
		"",
		markdownTable(
			["setting", "value"],
			[
				["provider", settings.provider],
				["version", settings.version],
				["processor count (DOTNET_PROCESSOR_COUNT)", settings.processorCount],
				["launch prefix", settings.launchPrefix.join(" ") || "none"],
				["raised min threads (C, C0)", String(settings.minThreads)],
				["scale points", settings.scalePoints.join(", ")],
				["fillers", settings.fillers ? "yes" : "no"],
				["repetitions", String(settings.repetitions)],
				["warm-ups", String(settings.warmUps)],
				["window seconds", String(settings.windowSeconds)],
				["host cores", String(os.cpus().length)],
				["finished", new Date().toISOString()],
			],
		),
		"",
	];
	const markdown = [
		...header,
		...opensSection(run, parsed),
		...windowsSection(run, parsed),
		...roundsSection(run, parsed),
	].join("\n");
	const resultsFile = path.join(run.folder, "results.md");
	fs.writeFileSync(resultsFile, markdown);
	// One line, and not .json, because the E2E build's Biome pass walks perf/ and flags large JSON files.
	fs.writeFileSync(
		path.join(run.folder, "results.ndjson"),
		JSON.stringify({
			settings,
			samples: run.samples,
			rounds: run.rounds,
			windows: run.windows,
			blockLoads: run.blockLoads,
		}),
	);
	return resultsFile;
}

test("takes every reading the baseline needs", async ({ browser }) => {
	test.setTimeout(24 * 60 * 60_000);
	const run = new Run();
	let backend: RunningBackend | undefined;
	let api: APIRequestContext | undefined;
	const stopCurrent = async () => {
		if (backend && api) {
			await run.stop(backend, api);
			await api.dispose();
		}
		backend = undefined;
		api = undefined;
	};

	try {
		for (const scale of settings.scalePoints) {
			if (settings.states.length === 0) {
				break;
			}
			backend = await run.start(`readings-${scale}`);
			api = await request.newContext({ baseURL: backend.baseURL });
			await waitUntilNothingIsQueued(api, settings.roundDrainSeconds * 1000);
			const targets = await readTargets(api, scale);
			for (const state of settings.states) {
				await readStateBlock(run, browser, backend, api, scale, state, targets);
				// A run stopped by hand still leaves the table of everything read so far.
				writeResults(run);
			}
			await stopCurrent();
		}

		const defaultWindows = settings.windows.filter(
			(name) => !windowPlans[name].raisedMinThreads,
		);
		const raisedWindows = settings.windows.filter(
			(name) => windowPlans[name].raisedMinThreads,
		);
		for (const [names, minThreads] of [
			[defaultWindows, undefined],
			[raisedWindows, settings.minThreads],
		] as const) {
			if (names.length === 0) {
				continue;
			}
			backend = await run.start(
				`windows-${minThreads ?? "default"}`,
				minThreads,
			);
			api = await request.newContext({ baseURL: backend.baseURL });
			await waitUntilNothingIsQueued(api, settings.roundDrainSeconds * 1000);
			const targets = await readTargets(api, settings.windowScale);
			for (const name of names) {
				await runWindow(run, browser, backend, api, name, targets);
				writeResults(run);
			}
			await stopCurrent();
		}
	} finally {
		await stopCurrent();
		const resultsFile = writeResults(run);
		console.log(`Results: ${resultsFile}`);
	}
});
