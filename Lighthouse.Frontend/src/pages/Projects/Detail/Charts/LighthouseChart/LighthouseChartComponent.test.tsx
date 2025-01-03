import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import dayjs, { type Dayjs } from "dayjs";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
	BurndownEntry,
	LighthouseChartData,
	LighthouseChartFeatureData,
} from "../../../../../models/Charts/LighthouseChartData";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import type { IChartService } from "../../../../../services/Api/ChartService";
import {
	createMockApiServiceContext,
	createMockChartService,
} from "../../../../../tests/MockApiServiceProvider";
import LighthouseChartComponent from "./LighthouseChartComponent";

// Mocking the dependent components
vi.mock(
	"../../../../../components/Common/DatePicker/DatePickerComponent",
	() => ({
		default: ({
			label,
			value,
			onChange,
		}: {
			label: string;
			value: Dayjs;
			onChange: (newValue: Dayjs | null) => void;
		}) => (
			<input
				aria-label={label}
				value={value.format("YYYY-MM-DD")}
				onChange={(e) => onChange(dayjs(e.target.value))}
			/>
		),
	}),
);

vi.mock("../../SampleFrequencySelector", () => ({
	default: ({
		sampleEveryNthDay,
		onSampleEveryNthDayChange,
	}: {
		sampleEveryNthDay: number;
		onSampleEveryNthDayChange: (value: number) => void;
	}) => (
		<select
			value={sampleEveryNthDay}
			onChange={(e) => onSampleEveryNthDayChange(Number(e.target.value))}
		>
			<option value={1}>Daily</option>
			<option value={7}>Weekly</option>
			<option value={30}>Monthly</option>
		</select>
	),
}));

vi.mock(
	"../../../../../components/Common/LoadingAnimation/LoadingAnimation",
	() => ({
		default: ({
			hasError,
			isLoading,
			children,
		}: {
			hasError: boolean;
			isLoading: boolean;
			children: React.ReactNode;
		}) => (
			<>
				{isLoading && <div>Loading...</div>}
				{hasError && <div>Error loading data</div>}
				{!isLoading && !hasError && children}
			</>
		),
	}),
);

// Creating mock chart service
const mockChartService: IChartService = createMockChartService();

const mockGetLighthouseChartData = vi.fn();
mockChartService.getLighthouseChartData = mockGetLighthouseChartData;

const MockApiServiceProvider = ({
	children,
}: { children: React.ReactNode }) => {
	const mockContext = createMockApiServiceContext({
		chartService: mockChartService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

const renderWithMockApiProvider = () => {
	render(
		<MockApiServiceProvider>
			<LighthouseChartComponent projectId={1} />
		</MockApiServiceProvider>,
	);
};

const lighthouseChartData = new LighthouseChartData(
	[
		new LighthouseChartFeatureData(
			"Feature 1",
			[new Date(), new Date(), new Date(), new Date()],
			[new BurndownEntry(new Date(), 10)],
		),
	],
	[],
);

describe("LighthouseChartComponent", () => {
	beforeEach(() => {
		mockGetLighthouseChartData.mockResolvedValue(lighthouseChartData);
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it("renders without crashing", async () => {
		renderWithMockApiProvider();

		await waitFor(() => {
			screen.getByLabelText("Burndown Start Date");
		});

		expect(screen.getByLabelText("Burndown Start Date")).toBeInTheDocument();
		expect(screen.getByRole("combobox")).toBeInTheDocument();
	});

	it("initially fetches lighthouse data", async () => {
		renderWithMockApiProvider();

		expect(mockGetLighthouseChartData).toHaveBeenCalledWith(
			1,
			expect.any(Date),
			1,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);
	});

	it("handles data fetching error", async () => {
		mockGetLighthouseChartData.mockRejectedValueOnce(new Error("Fetch error"));
		renderWithMockApiProvider();

		await waitFor(() =>
			expect(screen.getByText("Error loading data")).toBeInTheDocument(),
		);
	});

	it("updates start date and fetches new data", async () => {
		renderWithMockApiProvider();

		await waitFor(() => {
			screen.getByLabelText("Burndown Start Date");
		});

		const startDateInput = screen.getByLabelText("Burndown Start Date");
		fireEvent.change(startDateInput, {
			target: { value: dayjs().format("YYYY-MM-DD") },
		});

		expect(mockGetLighthouseChartData).toHaveBeenCalledWith(
			1,
			expect.any(Date),
			1,
		);
	});
});
