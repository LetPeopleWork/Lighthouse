import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDeliverySourceOption } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
	createMockFeatureService,
} from "../../../../../tests/MockApiServiceProvider";
import { DeliveryCreateModal } from "./DeliveryCreateModal";
import { clearDeliverySourceOptionsCache } from "./DeliverySourceTab";
import {
	allOptions,
	createFeature,
	datedInProject,
	datedOnASingleDigitDay,
	JIRA_RELEASE_SOURCE,
	mockPortfolio,
	openSourceList,
	RELEASE_44_IN_PROJECT_X,
	renderModal,
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

describe("DeliveryCreateModal and the one thing it asks for next", () => {
	// The date field holds a day on this browser's calendar, not a UTC instant, so the string typed
	// into it has to be built the same way or a test run either side of midnight names a different day.
	const dayOnThisBrowsersCalendar = (daysFromToday: number): string => {
		const day = new Date();
		day.setDate(day.getDate() + daysFromToday);
		const month = `${day.getMonth() + 1}`.padStart(2, "0");
		const dayOfMonth = `${day.getDate()}`.padStart(2, "0");

		return `${day.getFullYear()}-${month}-${dayOfMonth}`;
	};

	const blockingMessage = () =>
		within(screen.getByRole("dialog")).getByRole("alert").textContent;

	const typeTheDate = async (
		user: ReturnType<typeof userEvent.setup>,
		value: string,
	) => {
		await user.clear(screen.getByLabelText("Launch Date"));
		await user.type(screen.getByLabelText("Launch Date"), value);
	};

	const modalOfferingNoSource = () => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi.fn().mockResolvedValue([]);
		deliveryService.getRuleSchema = vi.fn().mockResolvedValue({
			fields: [
				{
					fieldKey: "fixVersion",
					displayName: "Fix Version",
					isMultiValue: false,
				},
			],
			operators: ["equals"],
			maxRules: 5,
			maxValueLength: 100,
		});

		renderModal(deliveryService);
		return deliveryService;
	};

	const modalShowingOneRelease = (options: IDeliverySourceOption[]) => {
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi
			.fn()
			.mockResolvedValue(options);
		deliveryService.previewDeliverySource = vi.fn().mockResolvedValue({
			name: "Release 44",
			date: new Date("2026-10-15T12:00:00Z"),
			features: [createFeature(1, "Widget rewrite")],
			emptyBecause: "None",
		});

		renderModal(deliveryService);
		return deliveryService;
	};

	it("names one missing thing at a time, in the order the reader can fix them", async () => {
		const user = userEvent.setup();
		modalOfferingNoSource();

		await waitFor(() => {
			expect(screen.getByLabelText("Launch Name")).toBeInTheDocument();
		});
		expect(blockingMessage()).toBe("Launch name is required");

		await user.type(screen.getByLabelText("Launch Name"), "Autumn launch");
		expect(blockingMessage()).toBe("Launch date is required");

		await typeTheDate(user, dayOnThisBrowsersCalendar(-7));
		expect(blockingMessage()).toBe("Launch date must be in the future");

		await typeTheDate(user, dayOnThisBrowsersCalendar(7));
		expect(blockingMessage()).toBe("At least one deliverable must be selected");
	});

	it("does not take a name of nothing but spaces for a name", async () => {
		const user = userEvent.setup();
		modalOfferingNoSource();

		await waitFor(() => {
			expect(screen.getByLabelText("Launch Name")).toBeInTheDocument();
		});
		await user.type(screen.getByLabelText("Launch Name"), "   ");

		expect(blockingMessage()).toBe("Launch name is required");
	});

	it("does not treat rules as matched just because the tab was opened", async () => {
		const user = userEvent.setup();
		modalOfferingNoSource();

		await waitFor(() => {
			expect(screen.getByLabelText("Launch Name")).toBeInTheDocument();
		});
		await user.type(screen.getByLabelText("Launch Name"), "Autumn launch");
		await typeTheDate(user, dayOnThisBrowsersCalendar(7));

		await user.click(screen.getByRole("button", { name: "Rule-Based" }));

		expect(
			await screen.findByText("Rules must be validated before saving"),
		).toBeInTheDocument();
		expect(screen.queryByText("No features match the rules")).toBeNull();
	});

	it("refuses today, because a date to launch on has to be a day still to come", async () => {
		const user = userEvent.setup();
		modalOfferingNoSource();

		await waitFor(() => {
			expect(screen.getByLabelText("Launch Name")).toBeInTheDocument();
		});
		await user.type(screen.getByLabelText("Launch Name"), "Autumn launch");
		await typeTheDate(user, dayOnThisBrowsersCalendar(0));

		expect(blockingMessage()).toBe("Launch date must be in the future");
	});

	it("writes a single-digit month and day into the date field with a leading zero", async () => {
		const user = userEvent.setup();
		modalShowingOneRelease([datedOnASingleDigitDay]);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await openSourceList(user);
		await user.click(screen.getByRole("option", { name: /Release 5/ }));

		expect(screen.getByLabelText("Launch Date")).toHaveValue("2027-01-05");
	});

	it("leaves the picked Release alone when its own tab button is clicked again", async () => {
		const user = userEvent.setup();
		modalShowingOneRelease(allOptions);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);
		await screen.findByTestId("delivery-source-preview");

		await user.click(screen.getByRole("button", { name: "Jira Release" }));

		expect(screen.getByRole("combobox", { name: "Jira Release" })).toHaveValue(
			RELEASE_44_IN_PROJECT_X,
		);
		expect(screen.getByRole("button", { name: "Save" })).toBeEnabled();
	});

	// The picker fills the date field in, and it has to fill it in with the day the tracker holds. The
	// runner is pinned east of UTC, so an entry dated late in the UTC day is the case where reading it
	// as this browser's day names the day AFTER the board does — the mirror of what a reader west of
	// UTC gets, and the same defect seen from the other side.
	it("writes the day the tracker holds into the date field, not the day this browser is having", async () => {
		const user = userEvent.setup();
		modalShowingOneRelease([
			{
				...datedInProject,
				date: new Date("2026-10-15T23:30:00Z"),
			},
		]);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);

		expect(screen.getByLabelText("Launch Date")).toHaveValue("2026-10-15");
	});

	// The server takes whatever day the tracker holds, including one already gone. The browser's own
	// future-date guard is the one thing that could still refuse it, and refusing it here would put a
	// Release that shipped last quarter out of reach with no way for anyone to argue.
	it("lets a Release dated in the past be bound, which no hand-typed date may be", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn();
		const deliveryService = createMockDeliveryService();
		deliveryService.getDeliverySources = vi
			.fn()
			.mockResolvedValue([JIRA_RELEASE_SOURCE]);
		deliveryService.getDeliverySourceOptions = vi.fn().mockResolvedValue([
			{
				...datedInProject,
				date: new Date("2025-03-04T00:00:00Z"),
			},
		]);

		render(
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({
					deliveryService,
					featureService: createMockFeatureService(),
				})}
			>
				<DeliveryCreateModal
					open={true}
					portfolio={mockPortfolio}
					onClose={vi.fn()}
					onSave={onSave}
				/>
			</ApiServiceContext.Provider>,
		);

		await user.click(
			await screen.findByRole("button", { name: "Jira Release" }),
		);
		await openSourceList(user);
		await user.click(
			screen.getByRole("option", { name: RELEASE_44_IN_PROJECT_X }),
		);

		fireEvent.click(screen.getByRole("button", { name: "Save" }));

		expect(onSave).toHaveBeenCalledWith({
			name: "Release 44",
			date: "2025-03-04",
			featureIds: [],
			selectionMode: DeliverySelectionMode.SourceBound,
			sourceKey: JIRA_RELEASE_SOURCE.key,
			sourceReference: datedInProject.id,
		});
		expect(screen.queryByText(/must be in the future/i)).toBeNull();
	});
});
