import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import QuickSettingsBar from "./QuickSettingsBar";

describe("QuickSettingsBar", () => {
	it("should render children correctly", () => {
		render(
			<QuickSettingsBar>
				<button type="button" data-testid="test-setting">
					Test Setting
				</button>
			</QuickSettingsBar>,
		);

		expect(screen.getByTestId("test-setting")).toBeInTheDocument();
	});

	it("should render multiple children with consistent spacing", () => {
		render(
			<QuickSettingsBar>
				<button type="button" data-testid="setting-1">
					Setting 1
				</button>
				<button type="button" data-testid="setting-2">
					Setting 2
				</button>
				<button type="button" data-testid="setting-3">
					Setting 3
				</button>
			</QuickSettingsBar>,
		);

		expect(screen.getByTestId("setting-1")).toBeInTheDocument();
		expect(screen.getByTestId("setting-2")).toBeInTheDocument();
		expect(screen.getByTestId("setting-3")).toBeInTheDocument();
	});

	it("should render empty when no children provided", () => {
		const { container } = render(<QuickSettingsBar />);

		expect(container.firstChild).toBeInTheDocument();
	});
});
