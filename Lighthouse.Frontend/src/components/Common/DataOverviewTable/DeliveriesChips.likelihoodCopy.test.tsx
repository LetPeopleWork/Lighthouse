import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../models/Delivery";
import { DeliveriesChips } from "./DeliveriesChips";

/**
 * Story #5587 (ADR-113), slice-03 — the portfolio OVERVIEW chip, ruled in scope by the maintainer
 * 2026-07-29.
 *
 * D1 scopes the relabel to the delivery detail header and the breakdown column. The overview chip
 * renders the same joint number as `Likelihood: NN%`, so a reader there has no cue that it means
 * "all of these together" — the exact misreading D1 exists to prevent, one surface over. The ruling
 * extends slice-03 to cover it.
 *
 * Constraint A (terminology): the noun comes from `getTerm(TERMINOLOGY_KEYS.FEATURES)`, and the
 * mock below is parameterised per test — a mock that hardcodes "Features" cannot fail on the defect
 * it is meant to catch, which is a literal surviving a rename. Ruled 2026-07-29: the term renders
 * VERBATIM, never lower-cased, because lower-casing mangles acronym terms ("PIs" -> "pis").
 *
 * Constraint B (no false promise): the delivery number is <= every row but MAY EQUAL one (D5).
 *
 * The second block holds the states the relabel must leave alone.
 */

const { terminology } = vi.hoisted(() => ({
	terminology: { current: {} as Record<string, string> },
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terminology.current[key] ?? key,
	}),
}));

const { getByPortfolio } = vi.hoisted(() => ({
	getByPortfolio: vi.fn(),
}));

vi.mock("../../../services/Api/ApiServiceContext", () => ({
	ApiServiceContext: {
		_currentValue: {
			deliveryService: { getByPortfolio },
		},
	},
}));

const PortfolioId = 7;

function deliveryWith(likelihood: number | null): Delivery {
	const delivery = new Delivery();
	delivery.id = 1;
	delivery.portfolioId = PortfolioId;
	delivery.name = "Autumn Release";
	delivery.likelihoodPercentage = likelihood;
	delivery.remainingWork = 12;
	delivery.hasSufficientData = true;
	delivery.teamsWithoutForecast = [];
	delivery.features = [1, 2, 3];
	return delivery;
}

async function renderChips(): Promise<void> {
	render(
		<MemoryRouter>
			<DeliveriesChips portfolioId={PortfolioId} />
		</MemoryRouter>,
	);

	await screen.findByText(/Autumn Release/);
}

beforeEach(() => {
	terminology.current = { features: "Features", deliveries: "Deliveries" };
	getByPortfolio.mockResolvedValue([deliveryWith(81)]);
});

describe("DeliveriesChips joint framing (#5587 slice-03)", () => {
	it("says the number covers all of them, not a bare likelihood", async () => {
		await renderChips();

		const chip = screen.getByText(/Autumn Release/);

		expect(chip.textContent).toContain("All Features");
		expect(chip.textContent).not.toMatch(/(^|\|\s*)Likelihood:/);
	});

	it("renders the configured term verbatim rather than lower-cased", async () => {
		terminology.current = { features: "PIs", deliveries: "Deliveries" };

		await renderChips();

		const chip = screen.getByText(/Autumn Release/);

		expect(chip.textContent).toContain("All PIs");
		expect(chip.textContent).not.toContain("All pis");
	});
});

describe("DeliveriesChips states the relabel must leave alone", () => {
	// Green today and must STAY green: today's copy makes no such claim, and the new copy must not
	// introduce one either. Constraint B - equality with a row is legitimate (D5). Kept out of the
	// skipped block deliberately: it passes before the change, so as a RED it could never fail.
	it("never promises the number is below every feature", async () => {
		await renderChips();

		const chip = screen.getByText(/Autumn Release/);

		expect(chip.textContent).not.toMatch(/lower|below|worst|least likely/i);
	});
	it("still reports that an unforecastable delivery cannot be forecast", async () => {
		const delivery = deliveryWith(null);
		delivery.teamsWithoutForecast = ["Beta"];
		getByPortfolio.mockResolvedValue([delivery]);

		await renderChips();

		expect(screen.getByText(/Autumn Release/).textContent).toContain(
			"Cannot forecast",
		);
	});

	it("still reports insufficient data ahead of any likelihood framing", async () => {
		const delivery = deliveryWith(81);
		delivery.hasSufficientData = false;
		getByPortfolio.mockResolvedValue([delivery]);

		await renderChips();

		expect(screen.getByText(/Autumn Release/).textContent).not.toContain("81");
	});
});
