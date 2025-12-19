import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { DeliveryHeader } from "./DeliveryHeader";

// Mock the terminology context
const mockGetTerm = vi.fn((key: string) => {
	const terminologyMap: Record<string, string> = {
		[TERMINOLOGY_KEYS.DELIVERY]: "Delivery",
		[TERMINOLOGY_KEYS.DELIVERIES]: "Deliveries",
	};
	return terminologyMap[key] || key;
});

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: mockGetTerm,
	}),
}));

describe("DeliveryHeader", () => {
	const mockOnAddDelivery = vi.fn();

	const renderComponent = (props = {}) => {
		const defaultProps = {
			onAddDelivery: mockOnAddDelivery,
			...props,
		};

		return render(<DeliveryHeader {...defaultProps} />);
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should display the deliveries title using terminology", () => {
		renderComponent();

		expect(
			screen.getByRole("heading", { name: "Deliveries" }),
		).toBeInTheDocument();
	});

	it("should render Add Delivery button with correct terminology", () => {
		renderComponent();

		expect(
			screen.getByRole("button", { name: "Add Delivery" }),
		).toBeInTheDocument();
	});

	it("should call onAddDelivery when Add button is clicked", async () => {
		const user = userEvent.setup();
		renderComponent();

		const addButton = screen.getByRole("button", { name: "Add Delivery" });
		await user.click(addButton);

		expect(mockOnAddDelivery).toHaveBeenCalledTimes(1);
	});

	it("should use correct terminology keys", () => {
		renderComponent();

		expect(mockGetTerm).toHaveBeenCalledWith(TERMINOLOGY_KEYS.DELIVERIES);
		expect(mockGetTerm).toHaveBeenCalledWith(TERMINOLOGY_KEYS.DELIVERY);
	});

	it("should display Add icon in the button", () => {
		renderComponent();

		const addButton = screen.getByRole("button", { name: "Add Delivery" });
		const icon = addButton.querySelector("svg");

		expect(icon).toBeInTheDocument();
		expect(icon).toHaveAttribute("data-testid", "AddIcon");
	});
});
