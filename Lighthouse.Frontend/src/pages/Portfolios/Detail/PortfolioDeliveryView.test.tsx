import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { Delivery } from "../../../models/Delivery";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
	createMockFeatureService,
} from "../../../tests/MockApiServiceProvider";
import PortfolioDeliveryView from "./PortfolioDeliveryView";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terminologyMap: Record<string, string> = {
				delivery: "Delivery",
				deliveries: "Deliveries",
				feature: "Feature",
				features: "Features",
				workItems: "Work Items",
			};
			return terminologyMap[key] || key;
		},
	}),
}));

const buildPortfolio = (): Portfolio => {
	const portfolio = new Portfolio();
	portfolio.id = 1;
	portfolio.name = "Test Portfolio";
	portfolio.features = [];
	portfolio.involvedTeams = [];
	return portfolio;
};

const buildDelivery = (id: number, name: string): Delivery => {
	const delivery = new Delivery();
	delivery.id = id;
	delivery.name = name;
	delivery.date = new Date("2026-06-01").toISOString();
	delivery.features = [];
	delivery.likelihoodPercentage = 80;
	delivery.progress = 50;
	delivery.remainingWork = 5;
	delivery.totalWork = 10;
	delivery.featureLikelihoods = [];
	delivery.completionDates = [];
	return delivery;
};

const renderView = (canEdit: boolean | undefined, deliveries: Delivery[]) => {
	const deliveryService = createMockDeliveryService();
	(
		deliveryService.getByPortfolio as ReturnType<typeof vi.fn>
	).mockResolvedValue(deliveries);

	const context = createMockApiServiceContext({
		deliveryService,
		featureService: createMockFeatureService(),
	});

	const portfolio = buildPortfolio();

	const props = canEdit === undefined ? { portfolio } : { portfolio, canEdit };

	return render(
		<ApiServiceContext.Provider value={context}>
			<MemoryRouter>
				<SnackbarErrorHandler>
					<PortfolioDeliveryView {...props} />
				</SnackbarErrorHandler>
			</MemoryRouter>
		</ApiServiceContext.Provider>,
	);
};

describe("PortfolioDeliveryView - RBAC action gating", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("shows Add Delivery, Edit, and Delete buttons when canEdit is true", async () => {
		const delivery = buildDelivery(7, "Phoenix Release");
		renderView(true, [delivery]);

		await waitFor(() => {
			expect(screen.getByText("Phoenix Release")).toBeInTheDocument();
		});

		expect(
			screen.getByRole("button", { name: "Add Delivery" }),
		).toBeInTheDocument();
		expect(screen.getByLabelText("edit")).toBeInTheDocument();
		expect(screen.getByLabelText("delete")).toBeInTheDocument();
	});

	it("hides Add Delivery, Edit, and Delete buttons when canEdit is false", async () => {
		const delivery = buildDelivery(7, "Phoenix Release");
		renderView(false, [delivery]);

		await waitFor(() => {
			expect(screen.getByText("Phoenix Release")).toBeInTheDocument();
		});

		expect(
			screen.queryByRole("button", { name: "Add Delivery" }),
		).not.toBeInTheDocument();
		expect(screen.queryByLabelText("edit")).not.toBeInTheDocument();
		expect(screen.queryByLabelText("delete")).not.toBeInTheDocument();
	});

	it("renders delivery list items when canEdit is false", async () => {
		const deliveries = [
			buildDelivery(7, "Phoenix Release"),
			buildDelivery(8, "Atlas Release"),
		];
		renderView(false, deliveries);

		await waitFor(() => {
			expect(screen.getByText("Phoenix Release")).toBeInTheDocument();
		});
		expect(screen.getByText("Atlas Release")).toBeInTheDocument();
	});
});
