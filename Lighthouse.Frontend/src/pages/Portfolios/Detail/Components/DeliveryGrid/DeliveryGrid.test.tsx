import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { Delivery } from "../../../../../models/Delivery";
import { DeliveryGrid } from "./DeliveryGrid";

// Mock components
vi.mock("../../../../../components/Common/DataGrid/DataGridBase", () => ({
	default: vi.fn(({ rows, loading, emptyStateMessage }) => (
		<div data-testid="delivery-grid">
			{loading && <div data-testid="loading">Loading...</div>}
			{!loading && rows.length === 0 && (
				<div>{emptyStateMessage || "No rows to display"}</div>
			)}
			{!loading && rows.length > 0 && (
				<div>
					{rows.map((row: { id: number; name: string }) => (
						<div key={row.id} data-testid={`delivery-${row.id}`}>
							{row.name}
							<button
								type="button"
								data-testid={`delete-${row.id}`}
								onClick={() => {
									// This would be handled by the actual grid column
								}}
							>
								Delete
							</button>
						</div>
					))}
				</div>
			)}
		</div>
	)),
}));

const createMockDelivery = (overrides: Partial<Delivery> = {}): Delivery => {
	const futureDate = new Date();
	futureDate.setDate(futureDate.getDate() + 7);

	return {
		id: 1,
		name: "Test Delivery",
		date: futureDate.toISOString(),
		portfolioId: 1,
		features: [],
		likelihoodPercentage: 75,
		getFeatureCount: () => 0,
		getLikelihoodLevel: () => "likely" as const,
		getFormattedDate: () => futureDate.toLocaleDateString(),
		...overrides,
	} as Delivery;
};

describe("DeliveryGrid", () => {
	const mockOnDelete = vi.fn();

	const renderComponent = (props = {}) => {
		const defaultProps = {
			deliveries: [],
			isLoading: false,
			onDelete: mockOnDelete,
			...props,
		};

		return render(<DeliveryGrid {...defaultProps} />);
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should show loading state", () => {
		renderComponent({ isLoading: true });

		expect(screen.getByTestId("loading")).toBeInTheDocument();
	});

	it("should display no upcoming deliveries message when filtered list is empty", () => {
		renderComponent({ deliveries: [] });

		expect(screen.getByText("No upcoming deliveries")).toBeInTheDocument();
	});

	it("should display future deliveries only", () => {
		const pastDate = new Date();
		pastDate.setDate(pastDate.getDate() - 7);

		const futureDate = new Date();
		futureDate.setDate(futureDate.getDate() + 7);

		const deliveries = [
			createMockDelivery({
				id: 1,
				name: "Past Delivery",
				date: pastDate.toISOString(),
			}),
			createMockDelivery({
				id: 2,
				name: "Future Delivery",
				date: futureDate.toISOString(),
			}),
		];

		renderComponent({ deliveries });

		expect(screen.getByTestId("delivery-2")).toBeInTheDocument();
		expect(screen.queryByTestId("delivery-1")).not.toBeInTheDocument();
	});

	it("should handle empty deliveries list", () => {
		renderComponent({ deliveries: [] });

		expect(screen.getByTestId("delivery-grid")).toBeInTheDocument();
		expect(screen.getByText("No upcoming deliveries")).toBeInTheDocument();
	});
});
