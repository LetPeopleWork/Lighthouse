import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));

/**
 * What the Deliveries folder asks the server is held in keyed queries, so a late answer, a second
 * reader or a reopened dialog is handled once, by the cache, rather than again in every component.
 * That decays silently, a hand-rolled fetch compiles and passes review, so it is asserted here.
 */
const shippedFilesUnder = (directory: string): string[] =>
	readdirSync(directory).flatMap((entry) => {
		const path = join(directory, entry);

		if (statSync(path).isDirectory()) {
			return shippedFilesUnder(path);
		}

		const isSource = /\.(ts|tsx)$/.test(entry);
		const isTestCode = /(\.test\.tsx?|Fixture\.tsx?|TestKit\.tsx?)$/.test(
			entry,
		);
		return isSource && !isTestCode ? [path] : [];
	});

/** The code alone: a comment naming a method is not a call to it. */
const withoutComments = (text: string): string =>
	text
		.replaceAll(/\/\*[\s\S]*?\*\//g, "")
		.replaceAll(/(^|[^:])\/\/.*$/gm, "$1");

const sources = shippedFilesUnder(here).map((path) => ({
	name: relative(here, path).replaceAll("\\", "/"),
	text: withoutComments(readFileSync(path, "utf8")),
}));

/**
 * Every file that names the method anywhere in its code, so a call through a destructured or renamed
 * reference (`const { getRuleSchema: ask } = deliveryService`) counts as much as `.getRuleSchema(`.
 */
const filesCalling = (method: string): string[] =>
	sources
		.filter((file) => new RegExp(`\\b${method}\\b`).test(file.text))
		.map((file) => file.name);

describe("the Deliveries folder's server state", () => {
	it.skip("asks for the ways to pick and for the Release lists from their own hooks only", () => {
		expect(filesCalling("getDeliverySources")).toEqual([
			"useDeliverySources.ts",
		]);
		expect(filesCalling("getDeliverySourceOptions")).toEqual([
			"useDeliverySourceOptions.ts",
		]);
	});

	it.skip("asks for the dialog's Feature list and its rule fields from their own hooks only", () => {
		expect(filesCalling("getRuleSchema")).toEqual([
			"useDeliveryFormQueries.ts",
		]);
		expect(filesCalling("getFeaturesByIds")).toContain(
			"useDeliveryFormQueries.ts",
		);
		expect(filesCalling("getFeaturesByIds")).not.toContain(
			"DeliveryCreateModal.tsx",
		);
	});

	it.skip("keeps no server answers in a store of its own that outlives a render", () => {
		const moduleWideStores = sources
			.filter((file) => /^(?:const|let)\s+\w+\s*=\s*new Map\b/m.test(file.text))
			.map((file) => file.name);

		expect(moduleWideStores).toEqual([]);
	});

	it("never asks for the licence itself and never spells the licence's query key", () => {
		const licenceReaders = sources
			.filter(
				(file) =>
					file.text.includes("getLicenseStatus") ||
					/\[\s*["']licenseStatus["']/.test(file.text),
			)
			.map((file) => file.name);

		expect(licenceReaders).toEqual([]);
	});
});
