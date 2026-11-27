// Spike probe, not product code: splits a cold page load on slow 4G into JavaScript bytes and API bytes and
// times, over HTTP/1.1 (plain http) and HTTP/2 (https), to tell round trips from bandwidth.
// Usage: node measure-bytes.mjs <httpBase> <httpsBase> <outFile.json>
import { createRequire } from "node:module";
import * as fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const require = createRequire(path.join(here, "../../../../Lighthouse.EndToEndTests/package.json"));
const { chromium } = require("@playwright/test");

const [httpBase, httpsBase, outFile] = process.argv.slice(2);
const routes = [
	["overview", "/"],
	["team-features", "/teams/1/features"],
	["team-metrics", "/teams/1/metrics"],
	["portfolio-features", "/portfolios/1/features"],
	["portfolio-metrics", "/portfolios/1/metrics"],
	["settings-system", "/settings/system/connections"],
];
const slow4g = { latency: 150, downloadThroughput: (1.6 * 1024 * 1024) / 8, uploadThroughput: (750 * 1024) / 8 };

async function measure(browser, base, route) {
	const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1920, height: 1080 } });
	const page = await context.newPage();
	const cdp = await context.newCDPSession(page);
	await cdp.send("Network.enable");
	await cdp.send("Network.emulateNetworkConditions", { offline: false, ...slow4g });
	const done = [];
	const t0 = Date.now();
	page.on("requestfinished", async (req) => {
		const at = Date.now() - t0;
		let bytes = 0;
		try {
			bytes = (await req.sizes()).responseBodySize;
		} catch {}
		const p = new URL(req.url()).pathname;
		const kind = p.startsWith("/api/") ? "api" : req.resourceType() === "script" ? "js" : "other";
		done.push({ kind, at, bytes, path: p });
	});
	await page.goto(base + route, { waitUntil: "load", timeout: 180_000 });
	let last = -1;
	let quietSince = Date.now();
	while (Date.now() - quietSince < 3000) {
		if (done.length !== last) {
			last = done.length;
			quietSince = Date.now();
		}
		await page.waitForTimeout(200);
	}
	await context.close();
	const sum = (k) => done.filter((d) => d.kind === k).reduce((s, d) => s + d.bytes, 0);
	const lastAt = (k) => Math.max(0, ...done.filter((d) => d.kind === k).map((d) => d.at));
	return {
		jsKB: Math.round(sum("js") / 1024),
		apiKB: Math.round(sum("api") / 1024),
		otherKB: Math.round(sum("other") / 1024),
		lastJsMs: lastAt("js"),
		lastApiMs: lastAt("api"),
		jsFiles: done.filter((d) => d.kind === "js").length,
	};
}

const browser = await chromium.launch();
const results = [];
for (const [name, route] of routes) {
	for (const [protocol, base] of [["http1.1", httpBase], ["h2", httpsBase]]) {
		const m = await measure(browser, base, route);
		results.push({ page: name, protocol, ...m });
		console.log(`${name.padEnd(20)} ${protocol.padEnd(8)} js=${m.jsKB}KB in ${m.jsFiles} files, last js ${m.lastJsMs}ms | api=${m.apiKB}KB, last api ${m.lastApiMs}ms`);
		fs.writeFileSync(outFile, JSON.stringify(results, null, 1));
	}
}
await browser.close();
