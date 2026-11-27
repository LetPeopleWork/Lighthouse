// Calls every GET operation in scope on the running instance, then checks each real answer two ways:
//  1. against the orval-generated zod schema (what the frontend would run), and
//  2. against the OpenAPI schema itself, strictly: undeclared properties, null where not nullable,
//     enum values, date formats, and which declared properties are always present (candidates for "required").
// usage: node validate-live.mjs <openapi.json> <generated-zod-index.ts> <base-url> <answers-dir> <report.json>
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const [docPath, zodIndex, base, answersDir, reportPath] = process.argv.slice(2);
const doc = JSON.parse(readFileSync(docPath, "utf8"));
const zodModule = await import(zodIndex);
mkdirSync(answersDir, { recursive: true });

const START = "2026-08-29";
const END = "2026-11-27";
const queryValues = {
	startDate: START,
	endDate: END,
	asOfDate: END,
	date: END,
	view: "filtered",
	horizon: "30",
	metricType: "CycleTime",
	type: "Throughput",
	state: "Implementation",
};

const pascal = (s) =>
	s
		.split(/[^A-Za-z0-9]+/)
		.filter(Boolean)
		.map((w) => w[0].toUpperCase() + w.slice(1))
		.join("");
const orvalName = (method, path) =>
	pascal(method) + path.split("/").map(pascal).join("");

const resolve = (schema) => {
	let s = schema;
	for (;;) {
		if (s?.$ref) s = doc.components.schemas[s.$ref.split("/").pop()];
		// UseAllOfToExtendReferenceSchemas wraps a nullable reference as { allOf: [ref], nullable: true }.
		else if (s?.allOf?.length === 1) s = { ...resolve(s.allOf[0]), nullable: !!s.nullable };
		else return s;
	}
};

const ISO = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})$/;

// presence[schemaPath][prop] = {seen, present, nonNull}
function check(schema, value, at, issues, presence, owner) {
	const s = resolve(schema);
	if (!s) return;
	const name = schema?.$ref ? schema.$ref.split("/").pop() : owner;
	if (value === null) {
		if (!s.nullable && !schema.nullable) issues.push({ at, kind: "null-not-nullable", schema: name });
		return;
	}
	if (s.enum) {
		if (typeof value !== "string") issues.push({ at, kind: "enum-not-string", value, schema: name });
		else if (!s.enum.includes(value)) issues.push({ at, kind: "enum-unknown-value", value, schema: name });
		return;
	}
	switch (s.type) {
		case "array":
			if (!Array.isArray(value)) return issues.push({ at, kind: "type", expected: "array", got: typeof value });
			value.forEach((v, i) => i < 50 && check(s.items, v, `${at}[${i}]`, issues, presence, name));
			return;
		case "integer":
			if (!Number.isInteger(value)) issues.push({ at, kind: "type", expected: "integer", got: value });
			return;
		case "number":
			if (typeof value !== "number") issues.push({ at, kind: "type", expected: "number", got: typeof value });
			return;
		case "boolean":
			if (typeof value !== "boolean") issues.push({ at, kind: "type", expected: "boolean", got: typeof value });
			return;
		case "string":
			if (typeof value !== "string") issues.push({ at, kind: "type", expected: "string", got: typeof value });
			else if (s.format === "date-time" && !ISO.test(value)) issues.push({ at, kind: "date-format", value });
			return;
	}
	if (s.properties || s.additionalProperties || s.type === "object") {
		if (typeof value !== "object" || Array.isArray(value))
			return issues.push({ at, kind: "type", expected: "object", got: typeof value });
		const props = s.properties ?? {};
		for (const key of Object.keys(value)) {
			if (props[key]) check(props[key], value[key], `${at}.${key}`, issues, presence, name);
			else if (s.additionalProperties)
				check(s.additionalProperties, value[key], `${at}.${key}`, issues, presence, name);
			else issues.push({ at: `${at}.${key}`, kind: "undeclared-property", schema: name });
		}
		if (s.properties) {
			presence[name] ??= {};
			for (const key of Object.keys(props)) {
				const p = (presence[name][key] ??= { seen: 0, present: 0, nonNull: 0, nullable: !!resolve(props[key])?.nullable || !!props[key].nullable, required: (s.required ?? []).includes(key) });
				p.seen++;
				if (key in value) p.present++;
				if (value[key] !== null && value[key] !== undefined) p.nonNull++;
			}
		}
	}
}

const results = [];
const presence = {};
for (const [path, item] of Object.entries(doc.paths)) {
	if (!path.includes("/teams/{teamId}/metrics") && path !== "/api/v1/Deliveries/portfolio/{portfolioId}") continue;
	const op = item.get;
	if (!op) continue;
	const qs = new URLSearchParams();
	for (const p of op.parameters ?? []) if (p.in === "query" && queryValues[p.name]) qs.set(p.name, queryValues[p.name]);
	const url = `${base}${path.replace("{teamId}", "1").replace("{portfolioId}", "1")}?${qs}`;
	const res = await fetch(url);
	const text = await res.text();
	const name = orvalName("get", path);
	const entry = { path, url, status: res.status, bytes: text.length, zodExport: `${name}Response` };
	if (res.status !== 200) {
		entry.error = text.slice(0, 200);
		results.push(entry);
		continue;
	}
	const body = JSON.parse(text);
	writeFileSync(join(answersDir, `${name}.json`), text);
	const zodSchema = zodModule[`${name}Response`];
	if (!zodSchema) entry.zod = "NO GENERATED SCHEMA";
	else {
		const r = zodSchema.safeParse(body);
		entry.zod = r.success ? "pass" : "FAIL";
		if (!r.success) entry.zodIssues = r.error.issues.slice(0, 8).map((i) => ({ path: i.path.join("."), code: i.code, message: i.message }));
	}
	const schema = op.responses["200"].content?.["application/json"]?.schema;
	const issues = [];
	check(schema, body, "$", issues, presence, "root");
	const grouped = {};
	for (const i of issues) {
		const k = `${i.kind} ${i.at.replace(/\[\d+\]/g, "[]")}`;
		grouped[k] ??= { ...i, count: 0 };
		grouped[k].count++;
	}
	entry.strictIssues = Object.values(grouped);
	results.push(entry);
}

// Properties the doc leaves optional/nullable but every real answer had (candidates for "required").
const neverMissing = [];
const neverNull = [];
let declared = 0;
for (const [schemaName, props] of Object.entries(presence))
	for (const [prop, p] of Object.entries(props)) {
		declared++;
		if (p.present === p.seen && !p.required) neverMissing.push(`${schemaName}.${prop}`);
		if (p.nonNull === p.seen && p.nullable) neverNull.push(`${schemaName}.${prop}`);
	}

writeFileSync(reportPath, JSON.stringify({ results, presence, neverMissing, neverNull }, null, 1));
const zodPass = results.filter((r) => r.zod === "pass").length;
console.log(`operations: ${results.length}, http200: ${results.filter((r) => r.status === 200).length}, zod pass: ${zodPass}`);
for (const r of results) {
	const kinds = (r.strictIssues ?? []).map((i) => `${i.kind}@${i.at}${i.value !== undefined ? `=${JSON.stringify(i.value)}` : ""}`);
	console.log(`${r.status} zod=${r.zod ?? "-"} ${r.path.replace("/api/v1/teams/{teamId}/metrics", "T")} ${kinds.slice(0, 6).join(" ")}${r.zodIssues ? ` ZOD:${JSON.stringify(r.zodIssues.slice(0, 3))}` : ""}${r.error ? ` ERR:${r.error}` : ""}`);
}
console.log(`declared object properties observed: ${declared}; always present but not required: ${neverMissing.length}; nullable but never null: ${neverNull.length}`);
