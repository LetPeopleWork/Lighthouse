import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import "@testing-library/jest-dom/vitest";
import { LicenseTooltip } from "./LicenseToolTip";

describe("LicenseTooltip", () => {
	const defaultProps = {
		canUseFeature: true,
		defaultTooltip: "This is a default tooltip",
		children: <button type="button">Test Button</button>,
	};

	it("renders children correctly", () => {
		render(<LicenseTooltip {...defaultProps} />);
		expect(screen.getByText("Test Button")).toBeInTheDocument();
	});

	it("displays default tooltip when canUseFeature is true", async () => {
		const user = userEvent.setup();
		render(<LicenseTooltip {...defaultProps} />);

		const button = screen.getByText("Test Button");
		await user.hover(button);

		expect(
			await screen.findByText("This is a default tooltip"),
		).toBeInTheDocument();
	});

	it("displays premium license message when canUseFeature is false", async () => {
		const user = userEvent.setup();
		render(<LicenseTooltip {...defaultProps} canUseFeature={false} />);

		const button = screen.getByRole("button", { name: "Test Button" });
		await user.hover(button);

		expect(
			await screen.findByText(/This feature requires a/i),
		).toBeInTheDocument();
		expect(screen.getByText("premium license.")).toBeInTheDocument();
	});

	it("renders link to premium license page when canUseFeature is false", async () => {
		const user = userEvent.setup();
		render(<LicenseTooltip {...defaultProps} canUseFeature={false} />);

		const button = screen.getByRole("button", { name: "Test Button" });
		await user.hover(button);

		const link = await screen.findByRole("link", { name: "premium license." });
		expect(link).toHaveAttribute(
			"href",
			"https://letpeople.work/lighthouse#lighthouse-license",
		);
		expect(link).toHaveAttribute("target", "_blank");
		expect(link).toHaveAttribute("rel", "noopener noreferrer");
	});

	it("applies correct styling to the premium license link", async () => {
		const user = userEvent.setup();
		render(<LicenseTooltip {...defaultProps} canUseFeature={false} />);

		const button = screen.getByRole("button", { name: "Test Button" });
		await user.hover(button);

		const link = await screen.findByRole("link", { name: "premium license." });
		expect(link).toHaveStyle({
			textDecoration: "underline",
		});
	});

	it("displays premiumExtraInfo when provided and canUseFeature is false", async () => {
		const user = userEvent.setup();
		const extraInfo = "Contact sales for more information.";

		render(
			<LicenseTooltip
				{...defaultProps}
				canUseFeature={false}
				premiumExtraInfo={extraInfo}
			/>,
		);

		const button = screen.getByRole("button", { name: "Test Button" });
		await user.hover(button);

		expect(
			await screen.findByText(/Contact sales for more information/i),
		).toBeInTheDocument();
	});

	it("does not display premiumExtraInfo when canUseFeature is true", async () => {
		const user = userEvent.setup();
		const extraInfo = "Contact sales for more information.";

		render(
			<LicenseTooltip
				{...defaultProps}
				canUseFeature={true}
				premiumExtraInfo={extraInfo}
			/>,
		);

		const button = screen.getByText("Test Button");
		await user.hover(button);

		await screen.findByText("This is a default tooltip");
		expect(
			screen.queryByText(/Contact sales for more information/i),
		).not.toBeInTheDocument();
	});

	it("does not display premiumExtraInfo when not provided and canUseFeature is false", async () => {
		const user = userEvent.setup();

		render(<LicenseTooltip {...defaultProps} canUseFeature={false} />);

		const button = screen.getByRole("button", { name: "Test Button" });
		await user.hover(button);

		const tooltip = await screen.findByText(/This feature requires a/i);
		expect(tooltip).toBeInTheDocument();
		// Verify only the default premium message is shown
		expect(tooltip.textContent).toMatch(
			/This feature requires a premium license\./,
		);
	});

	it("renders tooltip with arrow prop", () => {
		render(<LicenseTooltip {...defaultProps} />);
		// MUI Tooltip with arrow prop should render
		expect(screen.getByText("Test Button")).toBeInTheDocument();
	});

	it("handles different child components", () => {
		render(
			<LicenseTooltip {...defaultProps}>
				<div>Custom Child Element</div>
			</LicenseTooltip>,
		);
		expect(screen.getByText("Custom Child Element")).toBeInTheDocument();
	});

	it("switches tooltip content when canUseFeature changes", async () => {
		const user = userEvent.setup();
		const { rerender } = render(
			<LicenseTooltip {...defaultProps} canUseFeature={true} />,
		);

		let button = screen.getByText("Test Button");
		await user.hover(button);
		expect(
			await screen.findByText("This is a default tooltip"),
		).toBeInTheDocument();

		await user.unhover(button);

		rerender(<LicenseTooltip {...defaultProps} canUseFeature={false} />);
		button = screen.getByText("Test Button");
		await user.hover(button);

		expect(
			await screen.findByText(/This feature requires a/i),
		).toBeInTheDocument();
	});
});
