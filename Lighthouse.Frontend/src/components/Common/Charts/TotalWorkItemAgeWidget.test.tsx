import { createTheme, ThemeProvider } from "@mui/material/styles";
import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { BaseMetricsService } from "../../../services/Api/MetricsService";
import TotalWorkItemAgeWidget from "./TotalWorkItemAgeWidget";

const theme = createTheme();

const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
	<ThemeProvider theme={theme}>{children}</ThemeProvider>
);

describe("TotalWorkItemAgeWidget", () => {
	afterEach(() => {
		vi.restoreAllMocks();
	});

	it("renders the total age handed to it, synchronously (Bug #5571 AC1/AC2)", () => {
		// No `await waitFor` on purpose. A widget that fetched its own data would paint the
		// loading branch on first render, so a synchronous match is the proof that no async
		// data path of its own is left in the component.
		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget totalAge={250} />
			</TestWrapper>,
		);

		expect(screen.getByText("250")).toBeInTheDocument();
		expect(screen.getByText("days")).toBeInTheDocument();
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
	});

	it("renders zero as a value, not as missing data", () => {
		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget totalAge={0} />
			</TestWrapper>,
		);

		expect(screen.getByText("0")).toBeInTheDocument();
		expect(screen.getByText("days")).toBeInTheDocument();
	});

	it("renders the loading branch while totalAge is null (Bug #5571 AC3)", () => {
		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget totalAge={null} />
			</TestWrapper>,
		);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(screen.queryByText("days")).not.toBeInTheDocument();
	});

	it("makes no metrics-service call of its own (Bug #5571 AC2)", () => {
		const getTotalWorkItemAge = vi
			.spyOn(BaseMetricsService.prototype, "getTotalWorkItemAge")
			.mockResolvedValue(0);

		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget totalAge={120} />
			</TestWrapper>,
		);

		expect(getTotalWorkItemAge).not.toHaveBeenCalled();
	});

	it("renders the title with work item age terminology", () => {
		render(
			<TestWrapper>
				<TotalWorkItemAgeWidget totalAge={100} />
			</TestWrapper>,
		);

		expect(
			screen.getByRole("heading", { name: /Total.*Work Item Age/i }),
		).toBeInTheDocument();
	});
});
