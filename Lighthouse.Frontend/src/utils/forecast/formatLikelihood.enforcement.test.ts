import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));

const renderSites = {
	DeliverySection: resolve(
		here,
		"../../pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx",
	),
	DeliveriesChips: resolve(
		here,
		"../../components/Common/DataOverviewTable/DeliveriesChips.tsx",
	),
	ForecastLikelihood: resolve(
		here,
		"../../components/Common/Forecasts/ForecastLikelihood.tsx",
	),
} as const;

const formatLikelihoodCall = /formatLikelihood\s*\(/;
const inlineRoundedLikelihood =
	/Math\.round\([^)]*likelihood[^)]*\)\s*\+\s*["'`]%/i;
const inlineFixedLikelihood = /likelihood\w*\.toFixed\(\s*2\s*\)/i;

describe("forecast-likelihood render sites route through formatLikelihood", () => {
	it.each(
		Object.entries(renderSites),
	)("%s calls formatLikelihood and holds no inline likelihood format", (_name, path) => {
		const source = readFileSync(path, "utf8");

		expect(formatLikelihoodCall.test(source)).toBe(true);
		expect(inlineRoundedLikelihood.test(source)).toBe(false);
		expect(inlineFixedLikelihood.test(source)).toBe(false);
	});
});
