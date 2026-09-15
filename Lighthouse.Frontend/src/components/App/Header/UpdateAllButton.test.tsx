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
			licenseStatus: { canUsePremiumFeatures: true },
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

	it("should be disabled when user cannot update all teams and portfolios", () => {
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: false },
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

	// No license status is not an edge case: nothing is known about the license on the first paint of
	// every page, and nothing is known for the life of the page if the licensing call fails. The button
	// has to survive that and refuse the action, because offering somebody an action nobody has
	// established they are allowed is how they find out by being told no halfway through it.
	it("should render and be disabled when no license status is known", () => {
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: null,
		} as ReturnType<typeof useLicenseRestrictions>);

		render(<UpdateAllButton />);

		const button = screen.getByTestId("update-all-button");
		expect(button).toBeInTheDocument();
		expect(button).toBeDisabled();
	});

	// A button that refuses and says nothing is a dead end. The component already treats an unknown
	// license as permission it does not have, and the reason it offers has to agree with that - the
	// ordinary tooltip on a button that cannot be pressed explains nothing at all.
	it("should say why it is refusing when no license status is known", async () => {
		const user = userEvent.setup();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: null,
		} as ReturnType<typeof useLicenseRestrictions>);

		render(<UpdateAllButton />);

		// A disabled button takes no pointer events, which is exactly why it is wrapped - the wrapper is
		// what the tooltip listens on.
		const button = screen.getByTestId("update-all-button");
		await user.hover(button.parentElement ?? button);

		expect(
			await screen.findByText(/This feature requires a/i),
		).toBeInTheDocument();
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
			licenseStatus: { canUsePremiumFeatures: false },
		} as ReturnType<typeof useLicenseRestrictions>);

		render(<UpdateAllButton />);

		const button = screen.getByTestId("update-all-button");
		expect(button).toBeDisabled();
		expect(mockHandleUpdateAll).not.toHaveBeenCalled();
	});
});
