import { fireEvent, render, screen } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import ForecastSettingsComponent from "./ForecastSettingsComponent";

// Mock InputGroup component
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

// SCAFFOLD: true — DISTILL RED scaffolds for filter-forecast-throughput (Epic 4896).
describe("ForecastSettingsComponent — Forecast Filter sub-section (RED scaffold)", () => {
	it("renders the ForecastFilterEditor inside the existing Forecast Configuration InputGroup on premium tenants", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-01, feature-delta row 359, slice-01 line 21). DELIVER wave: the editor must render INSIDE the existing 'Forecast Configuration' InputGroup (below throughputHistory / fixed-dates fields), NOT in a new sibling section.",
		);
	});

	it("renders the upgrade teaser instead of the editor on non-premium tenants", () => {
		throw new Error(
			"Not yet implemented — RED scaffold (US-07 AC). DELIVER wave: useLicenseRestrictions().isPremium==false renders a one-line teaser with a docs link.",
		);
	});

	it("does not render the Forecast Filter sub-section at all when the team page is in default-settings mode", () => {
		throw new Error(
			"Not yet implemented — RED scaffold. DELIVER wave: the filter editor is team-scoped — default-team-settings page hides it.",
		);
	});

	it("preserves today's throughputHistory and fixed-dates fields above the new sub-section", () => {
		throw new Error(
			"Not yet implemented — RED scaffold. DELIVER wave: regression-protect the existing fields' presence and order inside the InputGroup.",
		);
	});
});
