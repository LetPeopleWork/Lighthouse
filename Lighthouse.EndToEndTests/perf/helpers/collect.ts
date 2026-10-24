import { type ChildProcess, execFileSync, spawn } from "node:child_process";
import * as fs from "node:fs";
import * as os from "node:os";
import path from "node:path";

/** One record of the measured backend's file log, written in the harness's own template. */
export type LogRecord = {
	at: number;
	level: string;
	source: string;
	requestId: string;
	message: string;
};

export type FinishedRequest = {
	at: number;
	requestId: string;
	method: string;
	pathAndQuery: string;
	status: number;
	milliseconds: number;
};

export type DatabaseCommand = {
	at: number;
	requestId: string;
	milliseconds: number;
};

export type HeartbeatWarning = { at: number; delayMilliseconds: number };

export type ForecastSplit = {
	at: number;
	portfolio: string;
	gather: number;
	simulate: number;
	save: number;
	publish: number;
};

export type RoundSummary = { at: number; portfolio: string; summary: string };

export type BackendLog = {
	finishedRequests: FinishedRequest[];
	databaseCommands: DatabaseCommand[];
	heartbeats: HeartbeatWarning[];
	forecastSplits: ForecastSplit[];
	roundSummaries: RoundSummary[];
};

export type CounterSample = {
	at: number;
	queueLength?: number;
	threadCount?: number;
	gcPauseMilliseconds?: number;
	gen2Collections?: number;
};

export type CpuStat = { periods: number; throttled: number };

export type MachineLoad = {
	load1: number;
	foreignTests: string[];
	waitedSeconds: number;
	disturbed: boolean;
};

/**
 * The shipped template prints neither the date nor milliseconds nor the request, and the server split needs all
 * three: the date and milliseconds to place a line inside an open or a window, the request to tell a request's own
 * database commands from the round's.
 */
export const harnessLogTemplate =
	"{@t:yyyy-MM-ddTHH:mm:ss.fffzzz} | {@l:u} | {SourceContext} | rid={RequestId} | {@m}\n{@x}";

/** Category overrides the readings need; the shipped settings keep all three above what the harness reads. */
export const harnessLogLevels: Record<string, string> = {
	"Microsoft.AspNetCore.Hosting.Diagnostics": "Information",
	"Microsoft.EntityFrameworkCore.Database.Command": "Information",
	"Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.ForecastUpdater":
		"Debug",
};

const recordPattern =
	/^(\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d{3}[+-]\d\d:\d\d) \| (\w+) \| ([^|]*) \| rid=(\S*) \| (.*)$/;
const finishedPattern =
	/^Request finished "?\S+?"? "?(\w+)"? "?(\S+?)"? - "?(\d{3})"? .* "?([\d.]+)"?ms$/;
const commandPattern = /^Executed DbCommand \((\d+)ms\)/;
const legacyHeartbeatPattern = /Heartbeat took longer than "([\d:.]+)"/;
const heartbeatPattern =
	/heartbeat has been running for "([\d:.]+)" which is longer than/;
const splitPattern =
	/^Forecast round timings \| Portfolio '"?(.*?)"?' \| gather=(\d+)ms \| simulate=(\d+)ms \| save=(\d+)ms \| publish=(\d+)ms/;
const summaryPattern = /^Update completed \| Portfolio '"?([^'"]*)"?' \| (.*)$/;

function timeSpanMilliseconds(span: string): number {
	const [hours, minutes, seconds] = span.split(":").map(Number);
	return ((hours * 60 + minutes) * 60 + seconds) * 1000;
}

function pathAndQueryOf(url: string): string {
	try {
		const parsed = new URL(url);
		return parsed.pathname + parsed.search;
	} catch {
		return url;
	}
}

export function readLogRecords(logFolder: string): LogRecord[] {
	if (!fs.existsSync(logFolder)) {
		return [];
	}
	const records: LogRecord[] = [];
	for (const file of fs.readdirSync(logFolder).sort()) {
		const text = fs.readFileSync(path.join(logFolder, file), "utf8");
		for (const line of text.split("\n")) {
			const match = recordPattern.exec(line);
			if (match) {
				records.push({
					at: Date.parse(match[1]),
					level: match[2],
					source: match[3],
					requestId: match[4],
					message: match[5],
				});
			}
		}
	}
	return records;
}

export function parseBackendLog(records: LogRecord[]): BackendLog {
	const log: BackendLog = {
		finishedRequests: [],
		databaseCommands: [],
		heartbeats: [],
		forecastSplits: [],
		roundSummaries: [],
	};
	for (const record of records) {
		const { at, requestId, message } = record;
		const finished = finishedPattern.exec(message);
		if (finished) {
			log.finishedRequests.push({
				at,
				requestId,
				method: finished[1],
				pathAndQuery: pathAndQueryOf(finished[2]),
				status: Number(finished[3]),
				milliseconds: Number(finished[4]),
			});
			continue;
		}
		const command = commandPattern.exec(message);
		if (command) {
			log.databaseCommands.push({
				at,
				requestId,
				milliseconds: Number(command[1]),
			});
			continue;
		}
		const heartbeat =
			heartbeatPattern.exec(message) ?? legacyHeartbeatPattern.exec(message);
		if (heartbeat) {
			log.heartbeats.push({
				at,
				delayMilliseconds: timeSpanMilliseconds(heartbeat[1]),
			});
			continue;
		}
		const split = splitPattern.exec(message);
		if (split) {
			log.forecastSplits.push({
				at,
				portfolio: split[1],
				gather: Number(split[2]),
				simulate: Number(split[3]),
				save: Number(split[4]),
				publish: Number(split[5]),
			});
			continue;
		}
		const summary = summaryPattern.exec(message);
		if (summary) {
			log.roundSummaries.push({
				at,
				portfolio: summary[1],
				summary: summary[2],
			});
		}
	}
	return log;
}

/**
 * dotnet-counters shows the System.Runtime meter's pool gauges as per-second changes, which hide the queue length
 * itself, so the readings come from the runtime's older event counters, which report the value. dotnet-counters will
 * not mix the two forms of one provider, so pause time and gen 2 come from the event counters too.
 */
export const preferredCounters =
	"EventCounters\\System.Runtime[threadpool-queue-length,threadpool-thread-count,total-pause-time-by-gc,gen-2-gc-count]";

/** When a dotnet-counters build refuses the selection above: the pool gauges are then rebuilt from their changes. */
export const fallbackCounters = "System.Runtime";

/** Starts a 1 s System.Runtime capture of the given process; stop it with stopProcess to flush the file. */
export function startCounters(
	countersTool: string,
	processId: number,
	csvFile: string,
	counterSelection: string,
): ChildProcess {
	const output = fs.openSync(`${csvFile}.out.txt`, "a");
	return spawn(
		countersTool,
		[
			"collect",
			"--process-id",
			String(processId),
			"--refresh-interval",
			"1",
			"--format",
			"csv",
			"--output",
			csvFile,
			"--counters",
			counterSelection,
		],
		{
			stdio: ["ignore", output, output],
			// The invariant culture pins the timestamp format the parser reads.
			env: { ...process.env, DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: "1" },
		},
	);
}

export async function stopProcess(
	child: ChildProcess,
	signal: NodeJS.Signals = "SIGINT",
	graceMilliseconds = 30_000,
): Promise<void> {
	if (child.exitCode !== null || child.signalCode !== null) {
		return;
	}
	const exited = new Promise<void>((resolve) => child.once("exit", resolve));
	child.kill(signal);
	const timer = setTimeout(() => child.kill("SIGKILL"), graceMilliseconds);
	await exited;
	clearTimeout(timer);
}

function counterTimestamp(text: string): number {
	const invariant = /^(\d\d)\/(\d\d)\/(\d{4}) (\d\d):(\d\d):(\d\d)/.exec(text);
	if (invariant) {
		const [, month, day, year, hours, minutes, seconds] = invariant.map(Number);
		return new Date(year, month - 1, day, hours, minutes, seconds).getTime();
	}
	return Date.parse(text);
}

type CounterField = keyof Omit<CounterSample, "at">;

const meterPoolGauge = /thread_pool\.(queue\.length|thread\.count)/;

function counterFieldOf(name: string): CounterField | undefined {
	if (
		/thread_pool\.queue\.length|threadpool-queue-length|ThreadPool Queue Length/i.test(
			name,
		)
	) {
		return "queueLength";
	}
	if (
		/thread_pool\.thread\.count|threadpool-thread-count|ThreadPool Thread Count/i.test(
			name,
		)
	) {
		return "threadCount";
	}
	if (/gc\.pause\.time|total-pause-time-by-gc|Time paused by GC/i.test(name)) {
		return "gcPauseMilliseconds";
	}
	if (/gc\.collections.*gen2|gen-2-gc-count|Gen 2 GC Count/i.test(name)) {
		return "gen2Collections";
	}
	return undefined;
}

/**
 * One sample per second. Rate rows already hold the increment over that second; cumulative rows are turned into
 * increments here, so every pause and collection figure is "during this second". The meter reports pause time in
 * seconds, the older event counter in milliseconds.
 */
export function readCounters(csvFile: string): CounterSample[] {
	if (!fs.existsSync(csvFile)) {
		return [];
	}
	const samples = new Map<number, CounterSample>();
	const lastCumulative = new Map<string, number>();
	const lines = fs.readFileSync(csvFile, "utf8").split("\n").slice(1);
	for (const line of lines) {
		const parts = line.trim().split(",");
		if (parts.length < 5) {
			continue;
		}
		const at = counterTimestamp(parts[0]);
		const name = parts.slice(2, -2).join(",");
		const type = parts[parts.length - 2];
		const raw = Number(parts[parts.length - 1]);
		const field = counterFieldOf(name);
		if (!field || Number.isNaN(at) || Number.isNaN(raw)) {
			continue;
		}
		let value = raw;
		const isGauge = field === "queueLength" || field === "threadCount";
		if (meterPoolGauge.test(name)) {
			// Shown as changes: summed back into a level, relative to the moment the capture attached.
			value = (lastCumulative.get(name) ?? 0) + raw;
			lastCumulative.set(name, value);
		} else if (!isGauge && type !== "Rate") {
			const previous = lastCumulative.get(name);
			lastCumulative.set(name, raw);
			value = previous === undefined ? 0 : Math.max(0, raw - previous);
		}
		if (field === "gcPauseMilliseconds" && /\(s\b/.test(name)) {
			value *= 1000;
		}
		const sample = samples.get(at) ?? { at };
		sample[field] = (sample[field] ?? 0) + value;
		samples.set(at, sample);
	}
	return [...samples.values()].sort((a, b) => a.at - b.at);
}

export function readCpuStat(processId: number): CpuStat {
	try {
		const cgroup = fs
			.readFileSync(`/proc/${processId}/cgroup`, "utf8")
			.split("\n")
			.find((line) => line.startsWith("0::"));
		if (!cgroup) {
			return { periods: 0, throttled: 0 };
		}
		const stat = fs.readFileSync(
			path.join("/sys/fs/cgroup", cgroup.slice(3), "cpu.stat"),
			"utf8",
		);
		const field = (name: string) =>
			Number(new RegExp(`^${name} (\\d+)$`, "m").exec(stat)?.[1] ?? 0);
		return { periods: field("nr_periods"), throttled: field("nr_throttled") };
	} catch {
		return { periods: 0, throttled: 0 };
	}
}

function foreignTestProcesses(ownProcessIds: Set<number>): string[] {
	const listing = execFileSync("ps", ["-eo", "pid=,args="], {
		encoding: "utf8",
	});
	return listing
		.split("\n")
		.map((line) => line.trim())
		.filter((line) => /dotnet test|testhost|vstest|vitest/.test(line))
		.filter((line) => !ownProcessIds.has(Number(line.split(" ")[0])))
		.filter((line) => !line.includes(" ps -eo"))
		.map((line) => line.slice(0, 120));
}

/**
 * Another workstream may be running test suites on this machine. A reading taken under that load measures the
 * other suite, so wait for a quiet machine, and say so when it never came.
 */
export async function waitForQuietMachine(
	maxLoad1: number,
	maxWaitSeconds: number,
	ownProcessIds: Set<number>,
	pollSeconds = 30,
): Promise<MachineLoad> {
	const started = Date.now();
	for (;;) {
		const load1 = os.loadavg()[0];
		const foreignTests = foreignTestProcesses(ownProcessIds);
		const waitedSeconds = Math.round((Date.now() - started) / 1000);
		if (load1 <= maxLoad1 && foreignTests.length === 0) {
			return { load1, foreignTests, waitedSeconds, disturbed: false };
		}
		if (waitedSeconds >= maxWaitSeconds) {
			return { load1, foreignTests, waitedSeconds, disturbed: true };
		}
		await new Promise((resolve) => setTimeout(resolve, pollSeconds * 1000));
	}
}

const scriptingEvents = new Set([
	"EvaluateScript",
	"v8.compile",
	"v8.compileModule",
	"v8.evaluateModule",
	"FunctionCall",
	"TimerFire",
	"EventDispatch",
	"FireAnimationFrame",
	"RunMicrotasks",
	"V8.Execute",
]);
const renderingEvents = new Set([
	"Layout",
	"UpdateLayoutTree",
	"RecalculateStyles",
	"PrePaint",
	"Paint",
	"Layerize",
	"UpdateLayerTree",
	"CompositeLayers",
]);

type TraceEvent = {
	name: string;
	ph: string;
	ts: number;
	dur?: number;
	pid: number;
	tid: number;
	args?: { name?: string };
};

/** Nested events (a FunctionCall inside an EventDispatch) are merged, so no time is counted twice. */
function mergedMilliseconds(intervals: [number, number][]): number {
	const sorted = intervals.sort((a, b) => a[0] - b[0]);
	let total = 0;
	let end = Number.NEGATIVE_INFINITY;
	for (const [start, stop] of sorted) {
		if (stop <= end) {
			continue;
		}
		total += stop - Math.max(start, end);
		end = stop;
	}
	return total / 1000;
}

/** Main-thread scripting versus rendering time of one Chrome trace, in milliseconds. */
export function scriptingAndRendering(traceFile: string): {
	scriptingMs: number;
	renderingMs: number;
} {
	const parsed = JSON.parse(fs.readFileSync(traceFile, "utf8"));
	const events: TraceEvent[] = Array.isArray(parsed)
		? parsed
		: parsed.traceEvents;
	const mainThreads = new Set(
		events
			.filter(
				(e) => e.name === "thread_name" && e.args?.name === "CrRendererMain",
			)
			.map((e) => `${e.pid}:${e.tid}`),
	);
	const scripting = new Map<string, [number, number][]>();
	const rendering = new Map<string, [number, number][]>();
	for (const event of events) {
		const thread = `${event.pid}:${event.tid}`;
		if (event.ph !== "X" || !event.dur || !mainThreads.has(thread)) {
			continue;
		}
		const bucket = scriptingEvents.has(event.name)
			? scripting
			: renderingEvents.has(event.name)
				? rendering
				: undefined;
		if (bucket) {
			const list = bucket.get(thread) ?? [];
			list.push([event.ts, event.ts + event.dur]);
			bucket.set(thread, list);
		}
	}
	const sum = (bucket: Map<string, [number, number][]>) =>
		[...bucket.values()].reduce(
			(total, intervals) => total + mergedMilliseconds(intervals),
			0,
		);
	return { scriptingMs: sum(scripting), renderingMs: sum(rendering) };
}

export function median(values: number[]): number {
	if (values.length === 0) {
		return Number.NaN;
	}
	const sorted = [...values].sort((a, b) => a - b);
	const middle = Math.floor(sorted.length / 2);
	return sorted.length % 2 === 1
		? sorted[middle]
		: (sorted[middle - 1] + sorted[middle]) / 2;
}

export function formatNumber(value: number | undefined, digits = 0): string {
	if (value === undefined || Number.isNaN(value)) {
		return "n/a";
	}
	return value.toFixed(digits);
}

export function markdownTable(header: string[], rows: string[][]): string {
	const line = (cells: string[]) =>
		`| ${cells.map((cell) => cell.replaceAll("|", "\\|")).join(" | ")} |`;
	return [line(header), line(header.map(() => "---")), ...rows.map(line)].join(
		"\n",
	);
}
