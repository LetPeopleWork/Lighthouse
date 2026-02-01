import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useUpdateAll } from "../../../hooks/useUpdateAll";
import UpdateAllButton from "./UpdateAllButton";

// Mock the hooks
vi.mock("../../../hooks/useLicenseRestrictions");
vi.mock("../../../hooks/useUpdateAll");

const mockUseLicenseRestrictions = vi.mocked(useLicenseRestrictions);
const mockUseUpdateAll = vi.mocked(useUpdateAll);

describe("UpdateAllButton", () => {
	const mockHandleUpdateAll = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();

		mockUseLicenseRestrictions.mockReturnValue({
			canUpdateAllTeamsAndPortfolios: true,
		} as ReturnType<typeof useLicenseRestrictions>);

		mockUseUpdateAll.mockReturnValue({
			handleUpdateAll: mockHandleUpdateAll,
			globalUpdateStatus: {
				hasActiveUpdates: false,
				activeCount: 0,
			},
			hasError: false,
		});
	});

	it("should render update button with correct icon", () => {
		render(<UpdateAllButton />);

		const button = screen.getByTestId("update-all-button");
		expect(button).toBeInTheDocument();
		expect(button).toHaveAttribute(
			"aria-label",
			"Update All Teams and Portfolios",
		);
	});

	it("should show circular progress when updates are active", () => {
		mockUseUpdateAll.mockReturnValue({
			handleUpdateAll: mockHandleUpdateAll,
			globalUpdateStatus: {
				hasActiveUpdates: true,
				activeCount: 3,
			},

			hasError: false,
		});

		render(<UpdateAllButton />);

		// CircularProgress should be rendered
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});

	it("should show badge with active count when there are updates pending", () => {
		mockUseUpdateAll.mockReturnValue({
			handleUpdateAll: mockHandleUpdateAll,
			globalUpdateStatus: {
				hasActiveUpdates: false,
				activeCount: 5,
			},
			hasError: false,
		});

		render(<UpdateAllButton />);

		// Badge should be rendered with count
		expect(screen.getByText("5")).toBeInTheDocument();
	});

	it("should be disabled when user cannot update all teams and portfolios", () => {
		mockUseLicenseRestrictions.mockReturnValue({
			canUpdateAllTeamsAndPortfolios: false,
		} as ReturnType<typeof useLicenseRestrictions>);

		render(<UpdateAllButton />);

		const button = screen.getByTestId("update-all-button");
		expect(button).toBeDisabled();
	});

	it("should be disabled when updates are in progress", () => {
		mockUseUpdateAll.mockReturnValue({
			handleUpdateAll: mockHandleUpdateAll,
			globalUpdateStatus: {
				hasActiveUpdates: true,
				activeCount: 2,
			},
			hasError: false,
		});

		render(<UpdateAllButton />);

		const button = screen.getByTestId("update-all-button");
		expect(button).toBeDisabled();
	});

	it("should call handleUpdateAll when clicked", async () => {
		const user = userEvent.setup();
		render(<UpdateAllButton />);

		const button = screen.getByTestId("update-all-button");
		await user.click(button);

		expect(mockHandleUpdateAll).toHaveBeenCalledOnce();
	});

	it("should not call handleUpdateAll when disabled", async () => {
		mockUseLicenseRestrictions.mockReturnValue({
			canUpdateAllTeamsAndPortfolios: false,
		} as ReturnType<typeof useLicenseRestrictions>);

		render(<UpdateAllButton />);

		const button = screen.getByTestId("update-all-button");
		expect(button).toBeDisabled();
		expect(mockHandleUpdateAll).not.toHaveBeenCalled();
	});
});
