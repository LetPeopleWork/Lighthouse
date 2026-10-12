import { fireEvent, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { renderWithQueryClient } from "../../../tests/renderWithQueryClient";
import {
	aWorkItem,
	cellText,
	day,
} from "../../../tests/WorkItemsDialogTestKit";
import LineRunChart from "./LineRunChart";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

vi.mock("./BlackoutOverlay", () => ({ default: () => null }));

vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		LineChart: ({
			onLineClick,
			xAxis,
		}: {
			onLineClick: (event: unknown, params: { dataIndex: number }) => void;
			xAxis: { data: string[] }[];
		}) => (
			<div>
				{xAxis[0].data.map((label, index) => (
					<button
						type="button"
						key={label}
						data-testid={`line-point-${index}`}
						onClick={(event) => onLineClick(event, { dataIndex: index })}
					>
						{label}
					</button>
				))}
			</div>
		),
	};
});

const RENDER_HEAVY = 20000;

describe("LineRunChart's dialog", () => {
	it(
		"shows the age an item had on the clicked day, even when it has finished since",
		() => {
			const finishedSince = aWorkItem({
				referenceId: "ST-1",
				startedDate: day("2026-09-01"),
				closedDate: day("2026-09-20"),
				cycleTime: 20,
			});
			const tenthOfSeptember = 9;
			const days = 12;
			const itemsPerDay = Object.fromEntries(
				Array.from({ length: days }, (_, index) => [
					index,
					index === tenthOfSeptember ? [finishedSince] : [],
				]),
			);
			const chartData = new RunChartData(itemsPerDay, days, 1);

			renderWithQueryClient(
				<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
					<LineRunChart
						chartData={chartData}
						startDate={day("2026-09-01")}
						dialogContext="inProgress"
						ownerKind="team"
					/>
				</ApiServiceContext.Provider>,
			);

			fireEvent.click(screen.getByTestId(`line-point-${tenthOfSeptember}`));

			expect(cellText("workItemAge", "ST-1")).toBe("10");
		},
		RENDER_HEAVY,
	);
});
