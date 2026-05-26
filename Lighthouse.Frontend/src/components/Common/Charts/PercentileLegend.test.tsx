import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import PercentileLegend from "./PercentileLegend";

describe("PercentileLegend", () => {
	const createPercentile = (
		percentile: number,
		value: number,
	): IPercentileValue => ({
		percentile,
		value,
	});

	const withTheme = (component: React.ReactElement) => {
		const theme = createTheme({
			palette: { mode: "light" },
		});
		const extendedTheme = {
			...theme,
			opacity: {
				subtle: 0.04,
				medium: 0.08,
				high: 0.12,
				opaque: 1,
			},
		};
		return <ThemeProvider theme={extendedTheme}>{component}</ThemeProvider>;
	};

	const renderWithTheme = (component: React.ReactElement) =>
		render(withTheme(component));

	describe("Percentile Chips", () => {
		it("should render a chip for each percentile", () => {
			const percentiles = [
				createPercentile(50, 10),
				createPercentile(85, 20),
				createPercentile(95, 30),
			];

			renderWithTheme(
				<PercentileLegend
					percentiles={percentiles}
					visiblePercentiles={{ 50: true, 85: true, 95: true }}
					onTogglePercentile={vi.fn()}
				/>,
			);

			expect(screen.getByText("50%")).toBeInTheDocument();
			expect(screen.getByText("85%")).toBeInTheDocument();
			expect(screen.getByText("95%")).toBeInTheDocument();
		});

		it("should call onTogglePercentile with correct percentile when chip is clicked", async () => {
			const user = userEvent.setup();
			const onTogglePercentile = vi.fn();
			const percentiles = [createPercentile(85, 20)];

			renderWithTheme(
				<PercentileLegend
					percentiles={percentiles}
					visiblePercentiles={{ 85: true }}
					onTogglePercentile={onTogglePercentile}
				/>,
			);

			const chip = screen.getByText("85%");
			await user.click(chip);

			expect(onTogglePercentile).toHaveBeenCalledWith(85);
			expect(onTogglePercentile).toHaveBeenCalledTimes(1);
		});

		it("should render visible chip with filled variant", () => {
			const percentiles = [createPercentile(50, 10)];

			renderWithTheme(
				<PercentileLegend
					percentiles={percentiles}
					visiblePercentiles={{ 50: true }}
					onTogglePercentile={vi.fn()}
				/>,
			);

			const chip = screen.getByText("50%").closest(".MuiChip-root");
			expect(chip).toHaveClass("MuiChip-filled");
		});

		it("should render hidden chip with outlined variant", () => {
			const percentiles = [createPercentile(50, 10)];

			renderWithTheme(
				<PercentileLegend
					percentiles={percentiles}
					visiblePercentiles={{ 50: false }}
					onTogglePercentile={vi.fn()}
				/>,
			);

			const chip = screen.getByText("50%").closest(".MuiChip-root");
			expect(chip).toHaveClass("MuiChip-outlined");
		});

		it("should handle multiple percentile clicks independently", async () => {
			const user = userEvent.setup();
			const onTogglePercentile = vi.fn();
			const percentiles = [createPercentile(50, 10), createPercentile(85, 20)];

			renderWithTheme(
				<PercentileLegend
					percentiles={percentiles}
					visiblePercentiles={{ 50: true, 85: false }}
					onTogglePercentile={onTogglePercentile}
				/>,
			);

			await user.click(screen.getByText("50%"));
			await user.click(screen.getByText("85%"));

			expect(onTogglePercentile).toHaveBeenCalledWith(50);
			expect(onTogglePercentile).toHaveBeenCalledWith(85);
			expect(onTogglePercentile).toHaveBeenCalledTimes(2);
		});

		it("should render empty when no percentiles provided", () => {
			const { container } = renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
				/>,
			);

			const chips = container.querySelectorAll(".MuiChip-root");
			expect(chips).toHaveLength(0);
		});
	});

	describe("SLE Chip", () => {
		it("should render SLE chip when serviceLevelExpectation and onToggleSle are provided", () => {
			const sle = createPercentile(90, 14);

			renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					serviceLevelExpectation={sle}
					onToggleSle={vi.fn()}
				/>,
			);

			expect(screen.getByText("SLE")).toBeInTheDocument();
		});

		it("should not render SLE chip when serviceLevelExpectation is missing", () => {
			renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					onToggleSle={vi.fn()}
				/>,
			);

			expect(screen.queryByText("SLE")).not.toBeInTheDocument();
		});

		it("should not render SLE chip when onToggleSle is missing", () => {
			const sle = createPercentile(90, 14);

			renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					serviceLevelExpectation={sle}
				/>,
			);

			expect(screen.queryByText("SLE")).not.toBeInTheDocument();
		});

		it("should use custom serviceLevelExpectationLabel when provided", () => {
			const sle = createPercentile(90, 14);

			renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					serviceLevelExpectation={sle}
					serviceLevelExpectationLabel="Custom SLE Label"
					onToggleSle={vi.fn()}
				/>,
			);

			expect(screen.getByText("Custom SLE Label")).toBeInTheDocument();
			expect(screen.queryByText("SLE")).not.toBeInTheDocument();
		});

		it("should call onToggleSle when SLE chip is clicked", async () => {
			const user = userEvent.setup();
			const onToggleSle = vi.fn();
			const sle = createPercentile(90, 14);

			renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					serviceLevelExpectation={sle}
					onToggleSle={onToggleSle}
				/>,
			);

			await user.click(screen.getByText("SLE"));

			expect(onToggleSle).toHaveBeenCalledTimes(1);
		});

		it("should render SLE chip as filled when sleVisible is true", () => {
			const sle = createPercentile(90, 14);

			renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					serviceLevelExpectation={sle}
					sleVisible={true}
					onToggleSle={vi.fn()}
				/>,
			);

			const chip = screen.getByText("SLE").closest(".MuiChip-root");
			expect(chip).toHaveClass("MuiChip-filled");
		});

		it("should render SLE chip as outlined when sleVisible is false", () => {
			const sle = createPercentile(90, 14);

			renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					serviceLevelExpectation={sle}
					sleVisible={false}
					onToggleSle={vi.fn()}
				/>,
			);

			const chip = screen.getByText("SLE").closest(".MuiChip-root");
			expect(chip).toHaveClass("MuiChip-outlined");
		});
	});

	describe("Combined Rendering", () => {
		it("should render both percentile chips and SLE chip together", () => {
			const percentiles = [createPercentile(50, 10), createPercentile(85, 20)];
			const sle = createPercentile(90, 14);

			renderWithTheme(
				<PercentileLegend
					percentiles={percentiles}
					visiblePercentiles={{ 50: true, 85: false }}
					onTogglePercentile={vi.fn()}
					serviceLevelExpectation={sle}
					onToggleSle={vi.fn()}
				/>,
			);

			expect(screen.getByText("50%")).toBeInTheDocument();
			expect(screen.getByText("85%")).toBeInTheDocument();
			expect(screen.getByText("SLE")).toBeInTheDocument();
		});
	});

	describe("Pace Percentiles Chip", () => {
		it("should render an off-by-default Pace percentiles chip when band data is available", () => {
			renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					paceBandsAvailable={true}
					showPaceBands={false}
					onTogglePaceBands={vi.fn()}
				/>,
			);

			const chip = screen
				.getByText("Pace percentiles")
				.closest(".MuiChip-root");
			expect(chip).toBeInTheDocument();
			expect(chip).toHaveClass("MuiChip-outlined");
		});

		it("should not render the Pace percentiles chip when no band data is available", () => {
			renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					paceBandsAvailable={false}
					showPaceBands={false}
					onTogglePaceBands={vi.fn()}
				/>,
			);

			expect(screen.queryByText("Pace percentiles")).not.toBeInTheDocument();
		});

		it("should toggle the overlay on then off across successive clicks", async () => {
			const user = userEvent.setup();
			const onTogglePaceBands = vi.fn();

			const { rerender } = renderWithTheme(
				<PercentileLegend
					percentiles={[]}
					visiblePercentiles={{}}
					onTogglePercentile={vi.fn()}
					paceBandsAvailable={true}
					showPaceBands={false}
					onTogglePaceBands={onTogglePaceBands}
				/>,
			);

			await user.click(screen.getByText("Pace percentiles"));
			expect(onTogglePaceBands).toHaveBeenCalledTimes(1);

			rerender(
				withTheme(
					<PercentileLegend
						percentiles={[]}
						visiblePercentiles={{}}
						onTogglePercentile={vi.fn()}
						paceBandsAvailable={true}
						showPaceBands={true}
						onTogglePaceBands={onTogglePaceBands}
					/>,
				),
			);
			expect(
				screen.getByText("Pace percentiles").closest(".MuiChip-root"),
			).toHaveClass("MuiChip-filled");

			await user.click(screen.getByText("Pace percentiles"));
			expect(onTogglePaceBands).toHaveBeenCalledTimes(2);
		});

		it("should leave the percentile and SLE chips unaffected by the Pace percentiles chip", async () => {
			const user = userEvent.setup();
			const onTogglePercentile = vi.fn();
			const onToggleSle = vi.fn();
			const onTogglePaceBands = vi.fn();
			const percentiles = [createPercentile(50, 10), createPercentile(85, 20)];
			const sle = createPercentile(90, 14);

			renderWithTheme(
				<PercentileLegend
					percentiles={percentiles}
					visiblePercentiles={{ 50: true, 85: false }}
					onTogglePercentile={onTogglePercentile}
					serviceLevelExpectation={sle}
					onToggleSle={onToggleSle}
					paceBandsAvailable={true}
					showPaceBands={false}
					onTogglePaceBands={onTogglePaceBands}
				/>,
			);

			expect(screen.getByText("50%")).toBeInTheDocument();
			expect(screen.getByText("85%")).toBeInTheDocument();
			expect(screen.getByText("SLE")).toBeInTheDocument();

			await user.click(screen.getByText("Pace percentiles"));

			expect(onTogglePaceBands).toHaveBeenCalledTimes(1);
			expect(onTogglePercentile).not.toHaveBeenCalled();
			expect(onToggleSle).not.toHaveBeenCalled();
		});
	});
});
