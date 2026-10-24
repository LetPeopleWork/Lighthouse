import { type ChildProcess, execFileSync, spawn } from "node:child_process";
import * as fs from "node:fs";
import path from "node:path";
import {
	type BackendLog,
	type CounterSample,
	type CpuStat,
	fallbackCounters,
	harnessLogLevels,
	harnessLogTemplate,
	type MachineLoad,
	preferredCounters,
	readCpuStat,
	startCounters,
	stopProcess,
} from "./collect";

export type BackendSettings = {
	contentRoot: string;
	executable: string;
	port: number;
	provider: string;
	connectionString: string;
	keysFolder: string;
	processorCount: string;
	launchPrefix: string[];
	countersTool: string;
};

export type RunningBackend = {
	label: string;
	minThreads: number | undefined;
	process: ChildProcess;
	counters: ChildProcess;
	countersFile: string;
	logFolder: string;
	baseURL: string;
};

export type WindowName = "T" | "R" | "RT" | "C" | "C0";

export type WindowPlan = {
	traffic: boolean;
	rounds: boolean;
	raisedMinThreads: boolean;
};

/**
 * T is page traffic alone, R a round alone, RT both. C is RT with only the pool's minimum worker threads raised, and
 * C0 is T with that same minimum, because raising it costs threads, context switches and allocation of its own.
 */
export const windowPlans: Record<WindowName, WindowPlan> = {
	T: { traffic: true, rounds: false, raisedMinThreads: false },
	R: { traffic: false, rounds: true, raisedMinThreads: false },
	RT: { traffic: true, rounds: true, raisedMinThreads: false },
	C: { traffic: true, rounds: true, raisedMinThreads: true },
	C0: { traffic: true, rounds: false, raisedMinThreads: true },
};

export type RoundInterval = { startedAt: number; finishedAt: number };

export type WindowReading = {
	name: WindowName;
	minThreads: number | undefined;
	startedAt: number;
	finishedAt: number;
	machine: MachineLoad;
	cpuBefore: CpuStat;
	cpuAfter: CpuStat;
	opens: number;
	rounds: RoundInterval[];
};

export type WindowFigures = {
	heartbeatWarnings: number;
	heartbeatDelayMs: number;
	queueIntegral: number;
	maxThreads: number;
	gcPauseShare: number;
	gcPauseShareAroundWarnings: number;
	gen2Collections: number;
	throttledShare: number;
	throttledPeriods: string;
};

/** A copy of the build whose runtime configuration raises the pool's minimum; the original build stays untouched. */
function executableWithMinThreads(
	executable: string,
	minThreads: number,
	scratchFolder: string,
): string {
	const sourceFolder = path.dirname(executable);
	const copyFolder = path.join(scratchFolder, `build-minthreads-${minThreads}`);
	if (!fs.existsSync(copyFolder)) {
		// Hard links: the build is a few hundred megabytes and only one file differs.
		execFileSync("cp", ["-al", sourceFolder, copyFolder]);
		const configFile = path.join(
			copyFolder,
			`${path.basename(executable)}.runtimeconfig.json`,
		);
		const config = JSON.parse(fs.readFileSync(configFile, "utf8"));
		config.runtimeOptions.configProperties = {
			...config.runtimeOptions.configProperties,
			"System.Threading.ThreadPool.MinThreads": minThreads,
		};
		fs.rmSync(configFile);
		fs.writeFileSync(configFile, JSON.stringify(config, null, 2));
	}
	return path.join(copyFolder, path.basename(executable));
}

async function waitUntilAnswering(
	baseURL: string,
	child: ChildProcess,
): Promise<void> {
	const deadline = Date.now() + 180_000;
	while (Date.now() < deadline) {
		if (child.exitCode !== null) {
			throw new Error(`The backend exited with code ${child.exitCode}`);
		}
		try {
			const response = await fetch(`${baseURL}api/latest/systeminfo`);
			if (response.ok) {
				return;
			}
		} catch {
			// Not listening yet.
		}
		await new Promise((resolve) => setTimeout(resolve, 1_000));
	}
	throw new Error(`The backend at ${baseURL} did not answer within 3 minutes`);
}

export async function startBackend(
	settings: BackendSettings,
	runFolder: string,
	label: string,
	minThreads: number | undefined,
): Promise<RunningBackend> {
	const folder = path.join(runFolder, "backends", label);
	const logFolder = path.join(folder, "logs");
	fs.mkdirSync(logFolder, { recursive: true });
	const executable =
		minThreads === undefined
			? settings.executable
			: executableWithMinThreads(settings.executable, minThreads, runFolder);
	const overrides = Object.entries(harnessLogLevels).map(
		([category, level]) =>
			`--Serilog:MinimumLevel:Override:${category}=${level}`,
	);
	const args = [
		`--Kestrel:Endpoints:Http:Url=http://localhost:${settings.port}`,
		`--Kestrel:Endpoints:Https:Url=https://localhost:${settings.port + 1000}`,
		`--Database:Provider=${settings.provider}`,
		`--Database:ConnectionString=${settings.connectionString}`,
		`--Encryption:KeyStorePath=${settings.keysFolder}`,
		`--Serilog:WriteTo:0:Args:path=${path.join(logFolder, "log-.txt")}`,
		`--Serilog:WriteTo:0:Args:formatter:template=${harnessLogTemplate}`,
		// Every database command is logged during a reading; writing each line straight through, as the shipped
		// shared file sink does, would load the disk the readings are taken on.
		"--Serilog:WriteTo:0:Args:shared=false",
		"--Serilog:WriteTo:0:Args:buffered=true",
		"--Serilog:WriteTo:0:Args:flushToDiskInterval=00:00:01",
		...overrides,
	];
	const [command, ...prefixArgs] = [
		...settings.launchPrefix,
		executable,
		...args,
	];
	const output = fs.openSync(path.join(folder, "stdout.txt"), "a");
	const child = spawn(command, prefixArgs, {
		cwd: settings.contentRoot,
		stdio: ["ignore", output, output],
		env: {
			...process.env,
			ASPNETCORE_ENVIRONMENT: "Development",
			DOTNET_PROCESSOR_COUNT: settings.processorCount,
		},
	});
	const baseURL = `http://localhost:${settings.port}/`;
	try {
		await waitUntilAnswering(baseURL, child);
	} catch (error) {
		await stopProcess(child, "SIGTERM");
		throw error;
	}
	const countersFile = path.join(folder, "counters.csv");
	let counters = startCounters(
		settings.countersTool,
		child.pid as number,
		countersFile,
		preferredCounters,
	);
	await new Promise((resolve) => setTimeout(resolve, 5_000));
	if (counters.exitCode !== null) {
		counters = startCounters(
			settings.countersTool,
			child.pid as number,
			countersFile,
			fallbackCounters,
		);
	}
	return {
		label,
		minThreads,
		process: child,
		counters,
		countersFile,
		logFolder,
		baseURL,
	};
}

export async function stopBackend(backend: RunningBackend): Promise<void> {
	await stopProcess(backend.counters, "SIGINT", 15_000);
	await stopProcess(backend.process, "SIGTERM", 30_000);
}

export function cpuStatOf(backend: RunningBackend): CpuStat {
	return readCpuStat(backend.process.pid as number);
}

function inside(at: number, from: number, to: number): boolean {
	return at >= from && at <= to;
}

/**
 * Pause and collection figures are per-second increments; the queue length is a per-second gauge, so its sum over
 * the window is the integral in work-item seconds.
 */
export function figuresOf(
	window: WindowReading,
	log: BackendLog,
	counters: CounterSample[],
): WindowFigures {
	const { startedAt, finishedAt } = window;
	const seconds = (finishedAt - startedAt) / 1000;
	const heartbeats = log.heartbeats.filter((h) =>
		inside(h.at, startedAt, finishedAt),
	);
	const samples = counters.filter((s) => inside(s.at, startedAt, finishedAt));
	const pauseMs = samples.reduce(
		(sum, s) => sum + (s.gcPauseMilliseconds ?? 0),
		0,
	);
	let pauseAroundWarnings = 0;
	for (const heartbeat of heartbeats) {
		pauseAroundWarnings += counters
			.filter((s) => inside(s.at, heartbeat.at - 1000, heartbeat.at + 1000))
			.reduce((sum, s) => sum + (s.gcPauseMilliseconds ?? 0), 0);
	}
	const periods = window.cpuAfter.periods - window.cpuBefore.periods;
	const throttled = window.cpuAfter.throttled - window.cpuBefore.throttled;
	return {
		heartbeatWarnings: heartbeats.length,
		heartbeatDelayMs: heartbeats.reduce(
			(sum, h) => sum + h.delayMilliseconds,
			0,
		),
		queueIntegral: samples.reduce((sum, s) => sum + (s.queueLength ?? 0), 0),
		maxThreads: samples.reduce(
			(max, s) => Math.max(max, s.threadCount ?? 0),
			0,
		),
		gcPauseShare: seconds > 0 ? pauseMs / (seconds * 1000) : Number.NaN,
		gcPauseShareAroundWarnings:
			heartbeats.length > 0
				? pauseAroundWarnings / (heartbeats.length * 2000)
				: Number.NaN,
		gen2Collections: samples.reduce(
			(sum, s) => sum + (s.gen2Collections ?? 0),
			0,
		),
		throttledShare: periods > 0 ? throttled / periods : 0,
		throttledPeriods: `${throttled}/${periods}`,
	};
}

/**
 * Database command time over request wall time, for requests that finished while one of RT's rounds ran. A floor:
 * turning rows into objects is not part of a command's time.
 */
export function databaseShareOfRequests(
	window: WindowReading,
	log: BackendLog,
): number {
	const duringRound = (at: number) =>
		window.rounds.some((round) =>
			inside(at, round.startedAt, round.finishedAt),
		) && inside(at, window.startedAt, window.finishedAt);
	const requests = log.finishedRequests.filter(
		(request) => request.requestId !== "" && duringRound(request.at),
	);
	const requestIds = new Set(requests.map((request) => request.requestId));
	const commandMs = log.databaseCommands
		.filter((command) => requestIds.has(command.requestId))
		.reduce((sum, command) => sum + command.milliseconds, 0);
	const requestMs = requests.reduce(
		(sum, request) => sum + request.milliseconds,
		0,
	);
	return requestMs > 0 ? commandMs / requestMs : Number.NaN;
}

export type Classification = {
	roundRatioHeartbeat: number;
	roundRatioQueue: number;
	databaseShare: number;
	poolStarvation: string;
	garbageCollection: string;
	cpuQuota: string;
};

function ratio(numerator: number, denominator: number): number {
	return denominator > 0 ? numerator / denominator : Number.NaN;
}

/** The thresholds were fixed before any reading was taken; they are applied here, never tuned to the readings. */
export function classify(
	figures: Partial<Record<WindowName, WindowFigures>>,
	databaseShare: number,
): Classification {
	const { T, R, RT, C, C0 } = figures;
	const verdict = (holds: boolean | undefined, detail: string) =>
		holds === undefined
			? `not decidable (${detail})`
			: `${holds ? "holds" : "does not hold"} (${detail})`;

	let poolStarvation: string;
	if (!RT || !C || !C0 || !T) {
		poolStarvation = "not decidable (needs T, RT, C and C0)";
	} else if (RT.heartbeatDelayMs === 0) {
		poolStarvation = "not decidable (RT shows no heartbeat delay)";
	} else {
		const removed = 1 - C.heartbeatDelayMs / RT.heartbeatDelayMs;
		const noNewWarnings = C0.heartbeatWarnings <= T.heartbeatWarnings;
		poolStarvation = verdict(
			removed >= 0.8 && noNewWarnings,
			`C removes ${(removed * 100).toFixed(0)}% of RT's delay; C0 ${C0.heartbeatWarnings} warnings vs T ${T.heartbeatWarnings}`,
		);
	}

	const garbageCollection = RT
		? verdict(
				RT.gcPauseShare >= 0.1 || RT.gcPauseShareAroundWarnings >= 0.5,
				`pause ${(RT.gcPauseShare * 100).toFixed(1)}% of RT, ${Number.isNaN(RT.gcPauseShareAroundWarnings) ? "no warnings" : `${(RT.gcPauseShareAroundWarnings * 100).toFixed(0)}% around warnings`}`,
			)
		: "not decidable (no RT)";

	const cpuQuota = RT
		? verdict(
				RT.throttledShare >= 0.2,
				`${RT.throttledPeriods} periods throttled${RT.throttledPeriods.endsWith("/0") ? ", no CPU quota on this cgroup" : ""}`,
			)
		: "not decidable (no RT)";

	return {
		roundRatioHeartbeat: ratio(
			R?.heartbeatDelayMs ?? Number.NaN,
			RT?.heartbeatDelayMs ?? Number.NaN,
		),
		roundRatioQueue: ratio(
			R?.queueIntegral ?? Number.NaN,
			RT?.queueIntegral ?? Number.NaN,
		),
		databaseShare,
		poolStarvation,
		garbageCollection,
		cpuQuota,
	};
}
