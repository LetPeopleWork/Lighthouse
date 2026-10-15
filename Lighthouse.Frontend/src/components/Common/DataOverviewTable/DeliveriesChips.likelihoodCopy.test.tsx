import { screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../models/Delivery";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
} from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../tests/renderWithQueryClient";
import { DeliveriesChips } from "./DeliveriesChips";

/**
 * The Overview's Delivery chip shows one number for all of a Delivery's Features together. A bare
 * "Likelihood: NN%" reads as if it were any single Feature's chance, so the chip says whose chance it is.
 *
 * The Features word comes from the instance's terminology and is changed per test: a mock that always
 * said "Features" could not catch a hard-coded word surviving a rename. The word is shown exactly as
 * configured, never lower-cased, because lower-casing mangles acronyms ("PIs" would become "pis").
 *
 * The joint number is never above any single Feature's, but it can equal one, so the chip must not
 * promise it is lower. The second block holds the states the joint wording must leave alone.
 */

const { terminology } = vi.hoisted(() => ({
	terminology: { current: {} as Record<string, string> },
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terminology.current[key] ?? key,
	}),
}));

const getByPortfolio = vi.fn();

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
	renderWithQueryClient(
		<MemoryRouter>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					deliveryService: {
						...createMockDeliveryService(),
						getByPortfolio,
					},
				})}
			>
				<DeliveriesChips portfolioId={PortfolioId} />
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);

	await screen.findByText(/Autumn Release/);
}

beforeEach(() => {
	terminology.current = { features: "Features", deliveries: "Deliveries" };
	getByPortfolio.mockResolvedValue({
		active: [deliveryWith(81)],
		archived: [],
	});
});

describe("DeliveriesChips says the number covers all Features together", () => {
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

describe("DeliveriesChips states the joint wording must leave alone", () => {
	// The joint number can equal a single Feature's, so the chip must never claim it is lower.
	it("never promises the number is below every feature", async () => {
		await renderChips();

		const chip = screen.getByText(/Autumn Release/);

		expect(chip.textContent).not.toMatch(/lower|below|worst|least likely/i);
	});

	it("still reports that an unforecastable delivery cannot be forecast", async () => {
		const delivery = deliveryWith(null);
		delivery.teamsWithoutForecast = ["Beta"];
		getByPortfolio.mockResolvedValue({ active: [delivery], archived: [] });

		await renderChips();

		expect(screen.getByText(/Autumn Release/).textContent).toContain(
			"Cannot forecast",
		);
	});

	it("still reports insufficient data ahead of any likelihood framing", async () => {
		const delivery = deliveryWith(81);
		delivery.hasSufficientData = false;
		getByPortfolio.mockResolvedValue({ active: [delivery], archived: [] });

		await renderChips();

		const chip = screen.getByText(/Autumn Release/);
		expect(chip.textContent).toContain("Not enough data");
		expect(chip.textContent).not.toContain("81");
	});
});
