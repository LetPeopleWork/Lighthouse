import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IHowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import type { IForecast } from "../../../models/Forecasts/IForecast";
import type { IWhenForecast } from "../../../models/Forecasts/WhenForecast";
import ForecastInfo from "./ForecastInfo";

vi.mock("./ForecastLevel", () => ({
	ForecastLevel: vi.fn(() => ({
		level: "High",
		probability: 85,
		IconComponent: () => <span data-testid="icon" />,
		color: "green",
	})),
}));

vi.mock("../LocalDateTimeDisplay/LocalDateTimeDisplay", () => ({
	default: ({ utcDate }: { utcDate: Date }) => (
		<span data-testid="local-date-time-display">{utcDate.toString()}</span>
	),
}));

describe("ForecastInfo component", () => {
	const whenForecast: IWhenForecast = {
		expectedDate: new Date("2025-08-04"),
		probability: 85,
	};

	const howManyForecast: IHowManyForecast = {
		value: 50,
		probability: 70,
	};

	it("should render WhenForecast correctly", () => {
		render(<ForecastInfo forecast={whenForecast} />);

		expect(screen.getByTestId("icon")).toBeInTheDocument();
		expect(screen.getByTestId("local-date-time-display")).toBeInTheDocument();
	});

	it("should render HowManyForecast correctly", () => {
		render(<ForecastInfo forecast={howManyForecast} />);

		expect(screen.getByTestId("icon")).toBeInTheDocument();
		expect(screen.getByText("50 Items")).toBeInTheDocument();
	});

	it("should show unsupported forecast type message", () => {
		const unsupportedForecast = {
			probability: 85,
		};

		render(<ForecastInfo forecast={unsupportedForecast as IForecast} />);

		expect(screen.getByText("Forecast Type not Supported")).toBeInTheDocument();
	});
});
