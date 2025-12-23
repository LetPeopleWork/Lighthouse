import { Typography } from "@mui/material";
import { render, screen } from "@testing-library/react";
import DetailHeader from "./DetailHeader";

describe("DetailHeader", () => {
	it("renders left, center, and right content correctly", () => {
		// Arrange
		const leftContent = (
			<Typography data-testid="left-content">Left Content</Typography>
		);
		const centerContent = (
			<Typography data-testid="center-content">Center Content</Typography>
		);
		const rightContent = (
			<Typography data-testid="right-content">Right Content</Typography>
		);

		// Act
		render(
			<DetailHeader
				leftContent={leftContent}
				centerContent={centerContent}
				rightContent={rightContent}
			/>,
		);

		// Assert
		expect(screen.getByTestId("left-content")).toBeInTheDocument();
		expect(screen.getByTestId("center-content")).toBeInTheDocument();
		expect(screen.getByTestId("right-content")).toBeInTheDocument();
	});

	it("renders with only left content", () => {
		// Arrange
		const leftContent = (
			<Typography data-testid="left-content">Left Content</Typography>
		);

		// Act
		render(<DetailHeader leftContent={leftContent} />);

		// Assert
		expect(screen.getByTestId("left-content")).toBeInTheDocument();
	});

	it("renders quickSettingsContent between leftContent and centerContent", () => {
		// Arrange
		const leftContent = (
			<Typography data-testid="left-content">Left Content</Typography>
		);
		const quickSettingsContent = (
			<Typography data-testid="quick-settings-content">
				Quick Settings
			</Typography>
		);
		const centerContent = (
			<Typography data-testid="center-content">Center Content</Typography>
		);

		// Act
		render(
			<DetailHeader
				leftContent={leftContent}
				quickSettingsContent={quickSettingsContent}
				centerContent={centerContent}
			/>,
		);

		// Assert
		expect(screen.getByTestId("left-content")).toBeInTheDocument();
		expect(screen.getByTestId("quick-settings-content")).toBeInTheDocument();
		expect(screen.getByTestId("center-content")).toBeInTheDocument();
	});
});
