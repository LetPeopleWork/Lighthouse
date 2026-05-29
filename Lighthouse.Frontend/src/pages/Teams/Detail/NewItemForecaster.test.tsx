import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { fireEvent, render, screen } from "@testing-library/react";
import dayjs from "dayjs";
import { useState } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { HowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import NewItemForecaster from "./NewItemForecaster";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			const terms: Record<string, string> = {
				[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
			};
			return terms[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../components/Common/Forecasts/ForecastInfoList", () => ({
	default: ({ title, forecasts }: { title: string; forecasts: unknown[] }) => (
		<div data-testid="forecast-info-list">
			<h3>{title}</h3>
			<div data-testid="forecasts-count">{forecasts.length}</div>
		</div>
	),
}));

vi.mock("../../../components/Common/ItemListManager/ItemListManager", () => ({
	default: ({
		items,
		onAddItem,
		onRemoveItem,
		suggestions,
	}: {
		items: string[];
		onAddItem: (item: string) => void;
		onRemoveItem: (item: string) => void;
		suggestions: string[];
	}) => (
		<div data-testid="item-list-manager">
			<div data-testid="selected-items">
				{items.map((item) => (
					<button
						key={item}
						type="button"
						onClick={() => onRemoveItem(item)}
						data-testid={`remove-${item}`}
					>
						{item}
					</button>
				))}
			</div>
			{suggestions.map((suggestion) => (
				<button
					key={suggestion}
					type="button"
					onClick={() => onAddItem(suggestion)}
					data-testid={`add-${suggestion}`}
				>
					Add {suggestion}
				</button>
			))}
		</div>
	),
}));

const createMockForecastResult = (): ManualForecast =>
	new ManualForecast(
		0,
		new Date(),
		[],
		[
			new HowManyForecast(0.5, 5),
			new HowManyForecast(0.85, 10),
			new HowManyForecast(0.95, 15),
		],
		0.85,
	);

interface HarnessProps {
	onInputChange: (complete: boolean) => void;
	newItemForecastResult?: ManualForecast | null;
	initialWorkItemTypes?: string[];
	workItemTypes?: string[];
}

const ControlledHarness = ({
	onInputChange,
	newItemForecastResult = null,
	initialWorkItemTypes = [],
	workItemTypes = ["User Story", "Bug", "Task"],
}: HarnessProps) => {
	const [startDate, setStartDate] = useState<dayjs.Dayjs | null>(
		dayjs().subtract(30, "day"),
	);
	const [endDate, setEndDate] = useState<dayjs.Dayjs | null>(dayjs());
	const [targetDate, setTargetDate] = useState<dayjs.Dayjs | null>(
		dayjs().add(30, "day"),
	);
	const [selectedWorkItemTypes, setSelectedWorkItemTypes] =
		useState<string[]>(initialWorkItemTypes);

	return (
		<LocalizationProvider dateAdapter={AdapterDayjs}>
			<NewItemForecaster
				newItemForecastResult={newItemForecastResult}
				startDate={startDate}
				endDate={endDate}
				targetDate={targetDate}
				selectedWorkItemTypes={selectedWorkItemTypes}
				onStartDateChange={setStartDate}
				onEndDateChange={setEndDate}
				onTargetDateChange={setTargetDate}
				onWorkItemTypesChange={setSelectedWorkItemTypes}
				onInputChange={onInputChange}
				workItemTypes={workItemTypes}
			/>
		</LocalizationProvider>
	);
};

describe("NewItemForecaster", () => {
	const onInputChange = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders the forecast input sections without a manual run button", () => {
		render(<ControlledHarness onInputChange={onInputChange} />);

		expect(
			screen.getByRole("heading", { name: "Historical Data" }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("heading", { name: "Target Date" }),
		).toBeInTheDocument();
		expect(
			screen.getByRole("heading", { name: "Work Item Types" }),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "Forecast" }),
		).not.toBeInTheDocument();
	});

	it("reports a complete input change once a work item type is added", () => {
		render(<ControlledHarness onInputChange={onInputChange} />);

		fireEvent.click(screen.getByTestId("add-User Story"));

		expect(onInputChange).toHaveBeenLastCalledWith(true);
	});

	it("reports inputs becoming incomplete when the last work item type is removed", () => {
		render(
			<ControlledHarness
				onInputChange={onInputChange}
				initialWorkItemTypes={["User Story"]}
			/>,
		);

		fireEvent.click(screen.getByTestId("remove-User Story"));

		expect(onInputChange).toHaveBeenLastCalledWith(false);
	});

	it("does not select the same work item type twice", () => {
		render(<ControlledHarness onInputChange={onInputChange} />);

		fireEvent.click(screen.getByTestId("add-User Story"));
		fireEvent.click(screen.getByTestId("add-User Story"));

		expect(screen.getAllByTestId("remove-User Story")).toHaveLength(1);
	});

	it("shows the forecast result when a result and work item types are present", () => {
		render(
			<ControlledHarness
				onInputChange={onInputChange}
				newItemForecastResult={createMockForecastResult()}
				initialWorkItemTypes={["User Story", "Bug"]}
			/>,
		);

		expect(screen.getByTestId("forecast-info-list")).toBeInTheDocument();
		expect(screen.getByTestId("forecasts-count")).toHaveTextContent("3");
		expect(screen.getByRole("heading", { level: 3 })).toHaveTextContent(
			/How many User Story, Bug Work Items will you add until/,
		);
	});

	it("hides the forecast result when no work item type is selected", () => {
		render(
			<ControlledHarness
				onInputChange={onInputChange}
				newItemForecastResult={createMockForecastResult()}
			/>,
		);

		expect(screen.queryByTestId("forecast-info-list")).not.toBeInTheDocument();
	});
});
