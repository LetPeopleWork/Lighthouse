import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import ForecastSettingsComponent from "./ForecastSettingsComponent";

vi.mock("../../../components/Common/InputGroup/InputGroup", () => ({
	__esModule: true,
	default: ({
		title,
		children,
	}: {
		title: string;
		children: React.ReactNode;
	}) => (
		<div>
			<h2>{title}</h2>
			{children}
		</div>
	),
}));

const mockIsTeamAdmin = vi.fn(() => true);
const mockCanUsePremiumFeatures = vi.fn(() => true);

vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => ({
		isLoading: false,
		isRbacEnabled: true,
		isSystemAdmin: false,
		canCreateTeam: true,
		canCreatePortfolio: true,
		isTeamAdmin: mockIsTeamAdmin,
		isPortfolioAdmin: () => true,
		summary: {
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: true,
			adminTeamIds: [],
			adminPortfolioIds: [],
		},
	}),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: mockCanUsePremiumFeatures() },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

const getApiServiceContextWithSchema = (): IApiServiceContext => {
	const teamService = createMockTeamService();
	teamService.getForecastFilterSchema = vi.fn().mockResolvedValue({
		fields: [
			{ fieldKey: "workitem.type", displayName: "Type", isMultiValue: false },
			{ fieldKey: "workitem.state", displayName: "State", isMultiValue: false },
		],
		operators: ["equals", "notequals", "contains"],
		maxRules: 20,
		maxValueLength: 500,
	});
	return createMockApiServiceContext({ teamService });
};

const renderWithContext = (ui: React.ReactElement) => {
	const ctx = getApiServiceContextWithSchema();
	return render(
		<ApiServiceContext.Provider value={ctx}>{ui}</ApiServiceContext.Provider>,
	);
};

describe("ForecastSettingsComponent", () => {
	const onTeamSettingsChange = vi.fn();

	const teamSettings = createMockTeamSettings();

	it("renders correctly with provided teamSettings", () => {
		render(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		// Check if the TextFields display the correct values
		expect(screen.getByLabelText("Throughput History")).toHaveValue(30);
	});

	it("calls onTeamSettingsChange with correct parameters when Throughput History TextField value changes", () => {
		render(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Throughput History"), {
			target: { value: "15" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith("throughputHistory", 15);
	});

	it("renders date inputs when useFixedDatesForThroughput is true", () => {
		const settingsWithFixedDates = {
			...teamSettings,
			useFixedDatesForThroughput: true,
		};

		render(
			<ForecastSettingsComponent
				teamSettings={settingsWithFixedDates}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		expect(screen.getByLabelText("Start Date")).toBeInTheDocument();
		expect(screen.getByLabelText("End Date")).toBeInTheDocument();
		expect(
			screen.queryByLabelText("Throughput History"),
		).not.toBeInTheDocument();
	});

	it("renders throughput history input when useFixedDatesForThroughput is false", () => {
		render(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		expect(screen.queryByLabelText("Start Date")).not.toBeInTheDocument();
		expect(screen.queryByLabelText("End Date")).not.toBeInTheDocument();
		expect(screen.getByLabelText("Throughput History")).toBeInTheDocument();
	});

	it("calls onTeamSettingsChange when useFixedDatesForThroughput switch is toggled", () => {
		render(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		const switchElement = screen.getByRole("switch", {
			name: "Use Fixed Dates for Throughput",
		});
		fireEvent.click(switchElement);

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"useFixedDatesForThroughput",
			true,
		);
	});

	it("calls onTeamSettingsChange when date inputs are changed", () => {
		const settingsWithFixedDates = {
			...teamSettings,
			useFixedDatesForThroughput: true,
		};

		render(
			<ForecastSettingsComponent
				teamSettings={settingsWithFixedDates}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Start Date"), {
			target: { value: "2023-01-01" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"throughputHistoryStartDate",
			expect.any(Date),
		);

		fireEvent.change(screen.getByLabelText("End Date"), {
			target: { value: "2023-01-15" },
		});

		expect(onTeamSettingsChange).toHaveBeenCalledWith(
			"throughputHistoryEndDate",
			expect.any(Date),
		);
	});

	it("does not render fixed dates switch when isDefaultSettings is true", () => {
		render(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={true}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		expect(
			screen.queryByRole("switch", {
				name: "Use Fixed Dates for Throughput",
			}),
		).not.toBeInTheDocument();
	});
});

describe("ForecastSettingsComponent — Forecast Filter sub-section", () => {
	const teamSettings = createMockTeamSettings();
	const onTeamSettingsChange = vi.fn();

	it("renders the ForecastFilterEditor inside the existing Forecast Configuration InputGroup on premium tenants", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);

		renderWithContext(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		const forecastConfigHeading = screen.getByRole("heading", {
			name: "Forecast Configuration",
		});
		const inputGroup = forecastConfigHeading.parentElement as HTMLElement;

		await waitFor(() => {
			const editor = inputGroup.querySelector(
				"[data-testid='delivery-rule-builder']",
			);
			expect(editor).not.toBeNull();
		});

		expect(
			screen.getByRole("heading", { name: "Forecast Filter (Premium)" }),
		).toBeInTheDocument();
	});

	it("renders the upgrade teaser instead of the editor on non-premium tenants", () => {
		mockCanUsePremiumFeatures.mockReturnValue(false);

		renderWithContext(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		expect(
			screen.getByText(/Forecast Filter is a Premium feature/i),
		).toBeInTheDocument();
		const learnMoreLink = screen.getByRole("link", { name: /learn more/i });
		expect(learnMoreLink).toHaveAttribute(
			"href",
			"/docs/premium-features#forecast-filter",
		);
		expect(
			screen.queryByTestId("delivery-rule-builder"),
		).not.toBeInTheDocument();
	});

	it("does not render the Forecast Filter sub-section at all when the team page is in default-settings mode", () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);

		renderWithContext(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={true}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		expect(
			screen.queryByRole("heading", { name: "Forecast Filter (Premium)" }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("delivery-rule-builder"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByText(/Forecast Filter is a Premium feature/i),
		).not.toBeInTheDocument();
	});

	it("preserves today's throughputHistory and fixed-dates fields above the new sub-section", () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);

		renderWithContext(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		const throughputField = screen.getByLabelText("Throughput History");
		const fixedDatesSwitch = screen.getByRole("switch", {
			name: "Use Fixed Dates for Throughput",
		});
		const subHeading = screen.getByRole("heading", {
			name: "Forecast Filter (Premium)",
		});

		expect(throughputField).toBeInTheDocument();
		expect(fixedDatesSwitch).toBeInTheDocument();

		const subHeadingPosition =
			subHeading.compareDocumentPosition(throughputField);
		expect(subHeadingPosition & Node.DOCUMENT_POSITION_PRECEDING).toBeTruthy();
	});
});

describe("ForecastSettingsComponent — forecastFilterRuleSetJson round-trip", () => {
	const teamSettings = createMockTeamSettings();

	it("propagates editor rule changes through onTeamSettingsChange as serialised JSON", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		const onTeamSettingsChange = vi.fn();

		renderWithContext(
			<ForecastSettingsComponent
				teamSettings={teamSettings}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		const addRuleButton = await screen.findByTestId("add-rule-button");
		fireEvent.click(addRuleButton);

		await waitFor(() => {
			expect(onTeamSettingsChange).toHaveBeenCalledWith(
				"forecastFilterRuleSetJson",
				expect.any(String),
			);
		});

		const calls = onTeamSettingsChange.mock.calls;
		const lastCall = calls[calls.length - 1];
		expect(lastCall).toBeDefined();
		const payload = JSON.parse(lastCall?.[1] as string) as {
			version: number;
			conditions: { fieldKey: string; operator: string; value: string }[];
		};
		expect(payload.version).toBe(1);
		expect(payload.conditions).toHaveLength(1);
		expect(payload.conditions[0].fieldKey).toBe("workitem.type");
	});

	it("hydrates the editor from teamSettings.forecastFilterRuleSetJson on render", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		const settingsWithRules = {
			...teamSettings,
			forecastFilterRuleSetJson: JSON.stringify({
				version: 1,
				conditions: [
					{
						fieldKey: "workitem.type",
						operator: "equals",
						value: "Bug",
					},
				],
			}),
		};

		renderWithContext(
			<ForecastSettingsComponent
				teamSettings={settingsWithRules}
				isDefaultSettings={false}
				onTeamSettingsChange={vi.fn()}
			/>,
		);

		await waitFor(() => {
			expect(screen.getByDisplayValue("Bug")).toBeInTheDocument();
		});
	});

	it("propagates null to onTeamSettingsChange when all rules are deleted", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		const onTeamSettingsChange = vi.fn();
		const settingsWithOneRule = {
			...teamSettings,
			forecastFilterRuleSetJson: JSON.stringify({
				version: 1,
				conditions: [
					{
						fieldKey: "workitem.type",
						operator: "equals",
						value: "Bug",
					},
				],
			}),
		};

		renderWithContext(
			<ForecastSettingsComponent
				teamSettings={settingsWithOneRule}
				isDefaultSettings={false}
				onTeamSettingsChange={onTeamSettingsChange}
			/>,
		);

		const removeButton = await screen.findByLabelText("Remove rule");
		fireEvent.click(removeButton);

		await waitFor(() => {
			expect(onTeamSettingsChange).toHaveBeenCalledWith(
				"forecastFilterRuleSetJson",
				null,
			);
		});
	});
});
