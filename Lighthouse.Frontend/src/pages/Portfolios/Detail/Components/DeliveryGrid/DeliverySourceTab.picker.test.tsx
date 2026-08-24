import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDeliverySourceOption } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { createMockDeliveryService } from "../../../../../tests/MockApiServiceProvider";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";
import {
	allOptions,
	datedInJustATest,
	openSourceList,
	RELEASE_44_IN_PROJECT_TEST,
	RELEASE_44_IN_PROJECT_X,
	renderTab,
	THE_DATELESS_RELEASE,
} from "./DeliverySourceTabFixture";

const licence = vi.hoisted(() => ({ isPremium: true }));

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		// Every term here is renamed away from the seeded default on purpose. A tenant who calls a
		// Portfolio a Value Stream must never be shown the word "Portfolio", and a mock that answers
		// with the default lets a hardcoded one through unnoticed.
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.DELIVERY]: "Launch",
				[TERMINOLOGY_KEYS.FEATURES]: "Deliverables",
				[TERMINOLOGY_KEYS.FEATURE]: "Deliverable",
				[TERMINOLOGY_KEYS.PORTFOLIO]: "Value Stream",
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: {
			hasLicense: licence.isPremium,
			isValid: licence.isPremium,
			canUsePremiumFeatures: licence.isPremium,
		},
		canCreateTeam: true,
		canUpdateTeamData: true,
		canUpdateTeamSettings: true,
		canUpdatePortfolioData: true,
		canUpdateAllTeamsAndPortfolios: licence.isPremium,
		createTeamTooltip: "",
		updateTeamDataTooltip: "",
		updateTeamSettingsTooltip: "",
		updatePortfolioDataTooltip: "",
		updateAllTeamsAndPortfoliosTooltip: "",
	}),
}));

vi.mock("../../../../../components/Common/FeatureGrid", () => ({
	FeatureGrid: ({ features }: { features: IFeature[] }) => (
		<div data-testid="feature-grid-mock">
			{features.map((f) => (
				<div key={f.id}>{f.name}</div>
			))}
		</div>
	),
}));

beforeEach(() => {
	vi.clearAllMocks();
	licence.isPremium = true;
	clearDeliverySourceOptionsCache();
});

describe("DeliverySourceTab picker", () => {
	it("says it is loading while the list fills", async () => {
		const deliveryService = createMockDeliveryService();
		let resolveOptions: (options: IDeliverySourceOption[]) => void = () => {};
		deliveryService.getDeliverySourceOptions = vi.fn().mockReturnValue(
			new Promise<IDeliverySourceOption[]>((resolve) => {
				resolveOptions = resolve;
			}),
		);

		renderTab(deliveryService);

		expect(await screen.findByRole("progressbar")).toBeInTheDocument();

		resolveOptions(allOptions);

		await waitFor(() => {
			expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
		});
	});

	it("names the project on every row so equally named Releases stay apart", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);

		// One row per Release and nothing else. The project used to be a heading over a group of rows,
		// which is one more kind of thing to read past for someone typing to find a name.
		expect(screen.getAllByRole("option")).toHaveLength(allOptions.length);
		expect(screen.queryByText("Project (Test) (JUSTATEST)")).toBeNull();
		expect(screen.queryByText("Project X (PROJ)")).toBeNull();

		expect(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		).toBeInTheDocument();
	});

	it("leaves the date off the rows, so the only date on screen is the one being previewed", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);

		expect(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		).not.toHaveTextContent(
			new Date("2026-09-30T00:00:00Z").toLocaleDateString(),
		);
	});

	it("narrows the list to what the reader types, matching the Release or the project", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);
		const picker = screen.getByRole("combobox", { name: "Jira Release" });

		await user.type(picker, "45");

		expect(screen.getAllByRole("option")).toHaveLength(1);
		expect(
			screen.getByRole("option", { name: THE_DATELESS_RELEASE }),
		).toBeInTheDocument();

		// The project half of the row is searchable too, and it is the only way to reach one of two
		// Releases that share a name.
		await user.clear(picker);
		await user.type(picker, "project x");

		expect(screen.getAllByRole("option")).toHaveLength(2);
		expect(
			screen.queryByRole("option", { name: RELEASE_44_IN_PROJECT_TEST }),
		).toBeNull();
	});

	it("lists a dateless Release but refuses to let it be picked, saying what is missing", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(allOptions);

		renderTab(deliveryService);
		await openSourceList(user);

		const blocked = screen.getByRole("option", { name: THE_DATELESS_RELEASE });
		expect(blocked).toHaveAttribute("aria-disabled", "true");
		expect(blocked).toHaveTextContent("No Release Date Set");

		fireEvent.click(blocked);

		expect(deliveryService.previewDeliverySource).not.toHaveBeenCalled();
	});

	it("renders selectability from the server even when the dates disagree with it", async () => {
		const user = userEvent.setup();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi.fn().mockResolvedValue([
			{
				...datedInJustATest,
				isSelectable: false,
				blockedBecause: "NoDateSet",
			},
		]);

		renderTab(deliveryService);
		await openSourceList(user);

		expect(screen.getByRole("option", { name: /Release 44/ })).toHaveAttribute(
			"aria-disabled",
			"true",
		);
	});

	it("shows an error rather than an empty list when the options cannot be fetched", async () => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockRejectedValue(new Error("boom"));

		renderTab(deliveryService);

		const error = await screen.findByRole("alert");
		expect(error).toHaveTextContent(/could not be loaded/i);
		expect(error).not.toHaveTextContent(/no releases/i);
		expect(error).toHaveTextContent(/Value Stream/);
		expect(error).not.toHaveTextContent(/Portfolio/);
	});
});
