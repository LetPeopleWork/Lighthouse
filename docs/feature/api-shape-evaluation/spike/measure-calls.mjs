// Spike probe, not product code: opens each page cold and records every API call it makes.
// Usage: node measure-calls.mjs <baseUrl> <outFile.json> [repeats] [cold|warm]
// Needs the E2E project's dependencies installed (Playwright + Chromium).
import { createRequire } from "node:module";
import * as fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const require = createRequire(
	path.join(here, "../../../../Lighthouse.EndToEndTests/package.json"),
);
const { chromium } = require("@playwright/test");

const baseUrl = process.argv[2] ?? "http://localhost:5431";
const outFile = process.argv[3] ?? path.join(here, "results.json");
const repeats = Number(process.argv[4] ?? 3);
// "warm" measures an in-app navigation from the Overview instead of a cold first load.
const warm = process.argv[5] === "warm";

const pages = [
	["overview", "/"],
	["team-features", "/teams/1/features"],
	["team-forecasts", "/teams/1/forecasts"],
	["team-metrics", "/teams/1/metrics"],
	["team-refinement", "/teams/1/refinement"],
	["team-reports", "/teams/1/reports"],
	["team-settings", "/teams/1/settings"],
	["portfolio-features", "/portfolios/1/features"],
	["portfolio-deliveries", "/portfolios/1/deliveries"],
	["portfolio-metrics", "/portfolios/1/metrics"],
	["portfolio-reports", "/portfolios/1/reports"],
	["portfolio-settings", "/portfolios/1/settings"],
	["features", "/features"],
	["reports", "/reports"],
	["settings-organisation", "/settings/organization/terminology"],
	["settings-system", "/settings/system/connections"],
];

// "slow" is Chrome DevTools' "Slow 4G" preset: 562.5 ms latency is "3G"; this one is 150 ms RTT.
// We use a deliberately harsh profile too, because a waterfall costs one RTT per step.
const profiles = {
	fast: null,
	slow4g: { latency: 150, downloadThroughput: (1.6 * 1024 * 1024) / 8, uploadThroughput: (750 * 1024) / 8 },
	slow3g: { latency: 400, downloadThroughput: (400 * 1024) / 8, uploadThroughput: (400 * 1024) / 8 },
};

const isApi = (url) => new URL(url).pathname.startsWith("/api/");

// A request "follows" another when it starts within this gap after the other finished: the usual sign of
// a fetch that waited for the other's answer (an enabled-when query, a component mounted on data).
const FOLLOW_GAP_MS = 60;

function waterfallDepth(requests) {
	const sorted = [...requests].sort((a, b) => a.start - b.start);
	const depth = new Map();
	const parent = new Map();
	for (const r of sorted) {
		let best = 1;
		let from = null;
		for (const p of sorted) {
			if (p === r) break;
			if (p.end <= r.start && r.start - p.end <= FOLLOW_GAP_MS) {
				const d = (depth.get(p) ?? 1) + 1;
				if (d > best) {
					best = d;
					from = p;
				}
			}
		}
		depth.set(r, best);
		parent.set(r, from);
	}
	let deepest = null;
	for (const r of sorted) if (!deepest || depth.get(r) > depth.get(deepest)) deepest = r;
	const chain = [];
	for (let r = deepest; r; r = parent.get(r)) chain.unshift(r.endpoint);
	return { depth: deepest ? depth.get(deepest) : 0, chain };
}

function endpointOf(url) {
	const u = new URL(url);
	return u.pathname.replace(/\/\d+(?=\/|$)/g, "/{id}").replace(/^\/api\/(latest|v\d+)/, "/api");
}

async function measure(browser, route, profile) {
	const context = await browser.newContext({ viewport: { width: 1920, height: 1080 } });
	const page = await context.newPage();
	if (profile) {
		const cdp = await context.newCDPSession(page);
		await cdp.send("Network.enable");
		await cdp.send("Network.emulateNetworkConditions", { offline: false, ...profile });
	}
	const requests = [];
	const t0 = { at: 0 };
	page.on("request", (req) => {
		if (!isApi(req.url())) return;
		requests.push({ req, url: req.url(), method: req.method(), endpoint: `${req.method()} ${endpointOf(req.url())}`, start: Date.now() - t0.at, end: null, bytes: 0, status: 0 });
	});
	page.on("requestfinished", async (req) => {
		const r = requests.find((x) => x.req === req);
		if (!r) return;
		r.end = Date.now() - t0.at;
		try {
			const resp = await req.response();
			r.status = resp?.status() ?? 0;
			const sizes = await req.sizes();
			r.bytes = sizes.responseBodySize;
		} catch {}
	});
	page.on("requestfailed", (req) => {
		const r = requests.find((x) => x.req === req);
		if (r) r.end = Date.now() - t0.at;
	});
	// Quiet period: no API request in flight or started for 2 s (SignalR's long-lived connection excluded).
	const waitQuiet = async () => {
		// On a slow line the JavaScript takes seconds to arrive, and no API call happens meanwhile: that is not quiet.
		const firstDeadline = Date.now() + 60_000;
		while (requests.length === 0 && Date.now() < firstDeadline) await page.waitForTimeout(100);
		let quietSince = Date.now();
		let lastCount = -1;
		const deadline = Date.now() + 120_000;
		while (Date.now() < deadline) {
			const open = requests.filter((r) => r.end === null && !r.url.includes("/hubs/")).length;
			if (open > 0 || requests.length !== lastCount) {
				quietSince = Date.now();
				lastCount = requests.length;
			}
			if (Date.now() - quietSince > 2000) break;
			await page.waitForTimeout(100);
		}
	};
	if (warm) {
		// In-app navigation: the shell and assets are already loaded, as when a user clicks from page to page.
		await page.goto(`${baseUrl}/`, { waitUntil: "load", timeout: 120_000 });
		await waitQuiet();
		requests.length = 0;
		t0.at = Date.now();
		await page.evaluate((to) => {
			window.history.pushState({}, "", to);
			window.dispatchEvent(new PopStateEvent("popstate"));
		}, route);
	} else {
		t0.at = Date.now();
		await page.goto(baseUrl + route, { waitUntil: "load", timeout: 120_000 });
	}
	await waitQuiet();
	await context.close();
	const done = requests.filter((r) => r.end !== null).map(({ req, ...rest }) => rest);
	const counts = new Map();
	for (const r of done) counts.set(r.url + r.method, (counts.get(r.url + r.method) ?? 0) + 1);
	const duplicates = [...counts.values()].filter((c) => c > 1).reduce((s, c) => s + c - 1, 0);
	const { depth, chain } = waterfallDepth(done);
	return {
		requests: done.length,
		distinctEndpoints: new Set(done.map((r) => r.endpoint)).size,
		duplicates,
		waterfallDepth: depth,
		waterfallChain: chain,
		firstApiStart: Math.min(...done.map((r) => r.start)),
		lastApiEnd: Math.max(...done.map((r) => r.end)),
		apiBytes: done.reduce((s, r) => s + (r.bytes > 0 ? r.bytes : 0), 0),
		calls: done.map((r) => ({ endpoint: r.endpoint, url: r.url.replace(baseUrl, ""), start: r.start, end: r.end, status: r.status, bytes: r.bytes })),
	};
}

const browser = await chromium.launch();
const results = [];
for (const [name, route] of pages) {
	if (warm && route === "/") continue;
	for (const [profileName, profile] of Object.entries(profiles)) {
		// Slow 3G costs about a minute per open, so it gets one run: it shows the shape, not a spread.
		if (warm && profileName === "slow3g") continue;
		const runs = profileName === "slow3g" ? 1 : repeats;
		for (let i = 0; i < runs; i++) {
			const m = await measure(browser, route, profile);
			results.push({ page: name, route, profile: profileName, run: i, ...m });
			console.log(`${name.padEnd(24)} ${profileName.padEnd(7)} #${i} req=${m.requests} distinct=${m.distinctEndpoints} dup=${m.duplicates} depth=${m.waterfallDepth} lastApiEnd=${m.lastApiEnd}ms`);
		}
	}
	fs.writeFileSync(outFile, JSON.stringify(results, null, 1));
}
await browser.close();
