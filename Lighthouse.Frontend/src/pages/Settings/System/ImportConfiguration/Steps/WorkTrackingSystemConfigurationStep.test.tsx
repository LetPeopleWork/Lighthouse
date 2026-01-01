import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IWorkTrackingSystemConnection } from "../../../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemService } from "../../../../../services/Api/WorkTrackingSystemService";
import WorkTrackingSystemConfigurationStep from "./WorkTrackingSystemConfigurationStep";

// Create mock work tracking system service
const createMockWorkTrackingSystemService = () => {
	return {
		validateWorkTrackingSystemConnection: vi.fn().mockResolvedValue(true),
		getWorkTrackingSystems: vi.fn(),
		getWorkTrackingSystem: vi.fn(),
		createWorkTrackingSystem: vi.fn(),
		updateWorkTrackingSystem: vi.fn(),
		deleteWorkTrackingSystem: vi.fn(),
	} as unknown as IWorkTrackingSystemService;
};

describe("WorkTrackingSystemConfigurationStep", () => {
	const mockOnNext = vi.fn();
	const mockOnCancel = vi.fn();
	const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();

	// Sample work tracking systems for testing
	const mockWorkTrackingSystems: IWorkTrackingSystemConnection[] = [
		{
			id: 1,
			name: "Azure DevOps",
			dataSourceType: "Query",
			workTrackingSystem: "AzureDevOps",
			authenticationMethodKey: "ado.pat",
			options: [
				{
					key: "url",
					value: "https://dev.azure.com/organization",
					isSecret: false,
					isOptional: false,
				},
				{
					key: "token",
					value: "",
					isSecret: true,
					isOptional: false,
				},
			],
		},
	];

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders with no work tracking systems", () => {
		render(
			<WorkTrackingSystemConfigurationStep
				newWorkTrackingSystems={[]}
				workTrackingSystemService={mockWorkTrackingSystemService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		expect(
			screen.getByText("Work Tracking System Configuration"),
		).toBeInTheDocument();
		expect(
			screen.getByText("No new work tracking Systems."),
		).toBeInTheDocument();

		// Buttons should be rendered
		expect(screen.getByText("Validate")).toBeInTheDocument();
		expect(screen.getByText("Next")).toBeInTheDocument();
		expect(screen.getByText("Cancel")).toBeInTheDocument();

		// Next button should be enabled as there are no systems to validate
		expect(screen.getByText("Next")).not.toBeDisabled();
	});
	it("renders with work tracking systems and shows secret fields", () => {
		render(
			<WorkTrackingSystemConfigurationStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				workTrackingSystemService={mockWorkTrackingSystemService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// System name should be displayed
		expect(screen.getByText("Azure DevOps")).toBeInTheDocument();

		// Secret field should be displayed by finding password inputs
		// Password inputs don't have the 'textbox' role
		const passwordInput = document.querySelector('input[type="password"]');
		expect(passwordInput).not.toBeNull();

		// Next button should be disabled initially until validation
		expect(screen.getByText("Next")).toBeDisabled();
	});

	it("handles validation success", async () => {
		mockWorkTrackingSystemService.validateWorkTrackingSystemConnection = vi
			.fn()
			.mockResolvedValue(true);

		render(
			<WorkTrackingSystemConfigurationStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				workTrackingSystemService={mockWorkTrackingSystemService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click validate button
		fireEvent.click(screen.getByText("Validate"));

		// Wait for validation to complete
		await waitFor(() => {
			expect(
				mockWorkTrackingSystemService.validateWorkTrackingSystemConnection,
			).toHaveBeenCalledWith(expect.objectContaining({ name: "Azure DevOps" }));
			expect(
				screen.getByText(
					"Validation successful! You can proceed to the next step.",
				),
			).toBeInTheDocument();
			expect(screen.getByText("Next")).not.toBeDisabled();
		});
	});

	it("handles validation failure", async () => {
		mockWorkTrackingSystemService.validateWorkTrackingSystemConnection = vi
			.fn()
			.mockResolvedValue(false);

		render(
			<WorkTrackingSystemConfigurationStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				workTrackingSystemService={mockWorkTrackingSystemService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click validate button
		fireEvent.click(screen.getByText("Validate"));

		// Wait for validation to complete
		await waitFor(() => {
			expect(
				mockWorkTrackingSystemService.validateWorkTrackingSystemConnection,
			).toHaveBeenCalledWith(expect.objectContaining({ name: "Azure DevOps" }));
			expect(screen.getByRole("alert")).toBeInTheDocument();
			expect(
				screen.getByText(/Connection validation failed for Azure DevOps/),
			).toBeInTheDocument();
			expect(screen.getByText("Next")).toBeDisabled();
		});
	});

	it("handles validation error with exception", async () => {
		mockWorkTrackingSystemService.validateWorkTrackingSystemConnection = vi
			.fn()
			.mockRejectedValue(new Error("API Error"));

		render(
			<WorkTrackingSystemConfigurationStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				workTrackingSystemService={mockWorkTrackingSystemService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click validate button
		fireEvent.click(screen.getByText("Validate"));

		// Wait for validation to complete
		await waitFor(() => {
			expect(
				mockWorkTrackingSystemService.validateWorkTrackingSystemConnection,
			).toHaveBeenCalled();
			expect(screen.getByRole("alert")).toBeInTheDocument();
			expect(screen.getByText("API Error")).toBeInTheDocument();
			expect(screen.getByText("Next")).toBeDisabled();
		});
	});
	it("handles field updates", async () => {
		render(
			<WorkTrackingSystemConfigurationStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				workTrackingSystemService={mockWorkTrackingSystemService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Get the token field and update it using document.querySelector since password inputs don't have standard roles
		const passwordInput = document.querySelector(
			'input[type="password"]',
		) as HTMLInputElement;
		expect(passwordInput).not.toBeNull();

		fireEvent.change(passwordInput, { target: { value: "new-secret-token" } });

		// Value should be updated in the component
		expect(passwordInput.value).toBe("new-secret-token");
	});
	it("calls onNext when Next button is clicked after validation", async () => {
		mockWorkTrackingSystemService.validateWorkTrackingSystemConnection = vi
			.fn()
			.mockResolvedValue(true);

		render(
			<WorkTrackingSystemConfigurationStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				workTrackingSystemService={mockWorkTrackingSystemService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Update token field
		const passwordInput = document.querySelector(
			'input[type="password"]',
		) as HTMLInputElement;
		expect(passwordInput).not.toBeNull();
		fireEvent.change(passwordInput, { target: { value: "new-secret-token" } });

		// Click validate button
		fireEvent.click(screen.getByText("Validate"));

		// Wait for validation to complete
		await waitFor(() => {
			expect(
				screen.getByText(
					"Validation successful! You can proceed to the next step.",
				),
			).toBeInTheDocument();
		});

		// Click next button
		fireEvent.click(screen.getByText("Next"));

		// Check that onNext was called with updated systems
		expect(mockOnNext).toHaveBeenCalledWith(
			expect.arrayContaining([
				expect.objectContaining({
					name: "Azure DevOps",
					options: expect.arrayContaining([
						expect.objectContaining({
							key: "token",
							value: "new-secret-token",
						}),
					]),
				}),
			]),
		);
	});

	it("calls onCancel when Cancel button is clicked", () => {
		render(
			<WorkTrackingSystemConfigurationStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				workTrackingSystemService={mockWorkTrackingSystemService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click cancel button
		fireEvent.click(screen.getByText("Cancel"));

		// Check that onCancel was called
		expect(mockOnCancel).toHaveBeenCalled();
	});

	it("disables buttons during validation", async () => {
		// Add delay to the mock to ensure we can check the disabled state
		mockWorkTrackingSystemService.validateWorkTrackingSystemConnection = vi
			.fn()
			.mockImplementation(
				() => new Promise((resolve) => setTimeout(() => resolve(true), 100)),
			);

		render(
			<WorkTrackingSystemConfigurationStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				workTrackingSystemService={mockWorkTrackingSystemService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click validate button
		fireEvent.click(screen.getByText("Validate"));

		// Check that buttons are disabled and progress indicator is shown
		expect(screen.getByText("Validating...")).toBeInTheDocument();
		expect(screen.getByText("Next")).toBeDisabled();
		expect(screen.getByText("Cancel")).toBeDisabled();

		// Wait for validation to complete
		await waitFor(() => {
			expect(screen.getByText("Validate")).toBeInTheDocument();
			expect(screen.getByText("Next")).not.toBeDisabled();
			expect(screen.getByText("Cancel")).not.toBeDisabled();
		});
	});
});
