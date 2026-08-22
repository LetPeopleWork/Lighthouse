import { render, screen, waitFor } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { Delivery } from "../../../models/Delivery";
import {
	ArchivedDelivery,
	ArchivedDeliverySchema,
} from "../../../models/Delivery/ArchivedDelivery";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
	createMockFeatureService,
	createMockLicensingService,
	createMockPortfolioService,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import { PREMIUM_UPGRADE_TOOLTIP } from "../../../utils/premiumUpgradeTooltip";
import PortfolioDeliveryView from "./PortfolioDeliveryView";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) =>
			({
				delivery: "Delivery",
				deliveries: "Deliveries",
				feature: "Feature",
				features: "Features",
				workItems: "Work Items",
			})[key] ?? key,
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
	delivery.features = [11, 12];
	delivery.likelihoodPercentage = 80;
	delivery.progress = 50;
	delivery.remainingWork = 5;
	delivery.totalWork = 10;
	delivery.featureLikelihoods = [];
	delivery.completionDates = [];
	delivery.concurrencyToken = "11111111-1111-1111-1111-111111111111";
	return delivery;
};

const buildArchived = (id: number, name: string): ArchivedDelivery =>
	ArchivedDelivery.fromParsed(
		ArchivedDeliverySchema.parse({
			id,
			name,
			date: "2026-05-01T00:00:00Z",
			portfolioId: 1,
			archivedOn: "2026-05-04T00:00:00Z",
			progress: 100,
			totalWork: 30,
			doneWork: 30,
			remainingWork: 0,
			likelihoodPercentage: 91,
			hasSufficientData: true,
			teamsWithoutForecast: [],
			selectionMode: "Manual",
			concurrencyToken: "22222222-2222-2222-2222-222222222222",
		}),
	);

const renderView = (options?: {
	active?: Delivery[];
	archived?: ArchivedDelivery[];
	canUsePremiumFeatures?: boolean;
	deliveryService?: ReturnType<typeof createMockDeliveryService>;
	featureService?: ReturnType<typeof createMockFeatureService>;
}) => {
	const deliveryService =
		options?.deliveryService ?? createMockDeliveryService();
	(
		deliveryService.getByPortfolio as ReturnType<typeof vi.fn>
	).mockResolvedValue({
		active: options?.active ?? [],
		archived: options?.archived ?? [],
	});

	const licensingService = createMockLicensingService();
	(
		licensingService.getLicenseStatus as ReturnType<typeof vi.fn>
	).mockResolvedValue({
		canUsePremiumFeatures: options?.canUsePremiumFeatures ?? true,
	});

	const featureService = options?.featureService ?? createMockFeatureService();

	// The licence gate counts Teams and Portfolios alongside the licence itself, so both have to
	// answer before it can decide anything.
	const teamService = createMockTeamService();
	(teamService.getTeams as ReturnType<typeof vi.fn>).mockResolvedValue([]);
	const portfolioService = createMockPortfolioService();
	(
		portfolioService.getPortfolios as ReturnType<typeof vi.fn>
	).mockResolvedValue([]);

	const context = createMockApiServiceContext({
		deliveryService,
		featureService,
		licensingService,
		teamService,
		portfolioService,
	});

	render(
		<ApiServiceContext.Provider value={context}>
			<MemoryRouter>
				<SnackbarErrorHandler>
					<PortfolioDeliveryView portfolio={buildPortfolio()} canEdit={true} />
				</SnackbarErrorHandler>
			</MemoryRouter>
		</ApiServiceContext.Provider>,
	);

	return { deliveryService, featureService };
};

describe("PortfolioDeliveryView - retiring a Delivery", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("keeps the retired Deliveries out of the live list and under their own heading", async () => {
		renderView({
			active: [buildDelivery(7, "Phoenix Release")],
			archived: [buildArchived(9, "Autumn Launch")],
		});

		await waitFor(() => {
			expect(screen.getByText("Phoenix Release")).toBeInTheDocument();
		});

		expect(screen.getByRole("button", { name: /Archived/ })).toHaveAttribute(
			"aria-expanded",
			"false",
		);
		expect(screen.queryByText("Autumn Launch")).not.toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: /Archived/ }));

		expect(await screen.findByText("Autumn Launch")).toBeInTheDocument();
	});

	it("never asks the server for the Features behind a retired Delivery", async () => {
		const { featureService } = renderView({
			archived: [buildArchived(9, "Autumn Launch")],
		});

		await userEvent.click(
			await screen.findByRole("button", { name: /Archived/ }),
		);
		await screen.findByText("Autumn Launch");

		expect(featureService.getFeaturesByIds).not.toHaveBeenCalled();
	});

	it("asks before archiving, and archives on the version it is looking at", async () => {
		const { deliveryService } = renderView({
			active: [buildDelivery(7, "Phoenix Release")],
		});

		await waitFor(() => {
			expect(screen.getByLabelText("archive")).not.toBeDisabled();
		});

		await userEvent.click(screen.getByLabelText("archive"));

		expect(await screen.findByText(/bring it back/i)).toBeInTheDocument();
		expect(deliveryService.archive).not.toHaveBeenCalled();

		await userEvent.click(screen.getByRole("button", { name: "Archive" }));

		await waitFor(() => {
			expect(deliveryService.archive).toHaveBeenCalledWith(
				7,
				"11111111-1111-1111-1111-111111111111",
			);
		});
	});

	it("leaves the Delivery where it was when the confirmation is declined", async () => {
		const { deliveryService } = renderView({
			active: [buildDelivery(7, "Phoenix Release")],
		});

		await waitFor(() => {
			expect(screen.getByLabelText("archive")).not.toBeDisabled();
		});

		await userEvent.click(screen.getByLabelText("archive"));
		await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

		expect(deliveryService.archive).not.toHaveBeenCalled();
		expect(screen.getByText("Phoenix Release")).toBeInTheDocument();
	});

	it("shows Archive without a licence but will not run it", async () => {
		renderView({
			active: [buildDelivery(7, "Phoenix Release")],
			canUsePremiumFeatures: false,
		});

		const archiveButton = await screen.findByLabelText("archive");

		await waitFor(() => {
			expect(archiveButton).toBeDisabled();
		});
		expect(archiveButton.parentElement).toHaveAttribute(
			"aria-label",
			PREMIUM_UPGRADE_TOOLTIP,
		);
	});

	it("says so when the Delivery moved on while the confirmation was open", async () => {
		const deliveryService = createMockDeliveryService();
		(deliveryService.archive as ReturnType<typeof vi.fn>).mockRejectedValue(
			new ApiError(409, "Request failed with status code 409"),
		);

		renderView({
			active: [buildDelivery(7, "Phoenix Release")],
			deliveryService,
		});

		await waitFor(() => {
			expect(screen.getByLabelText("archive")).not.toBeDisabled();
		});

		await userEvent.click(screen.getByLabelText("archive"));
		await userEvent.click(screen.getByRole("button", { name: "Archive" }));

		expect(
			await screen.findByText(/changed by someone else/i),
		).toBeInTheDocument();
	});
});
