import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../services/TerminologyContext";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { TerminologyConfiguration } from "./TerminologyConfiguration";

// Mock the terminology service
const mockTerminologyService = {
	getAllTerminology: vi.fn(),
	updateTerminology: vi.fn(),
};

// Mock licensing-related services
const mockLicensingService = {
	getLicenseStatus: vi.fn(),
	importLicense: vi.fn(),
	clearLicense: vi.fn(),
};

const mockTeamService = {
	getTeams: vi.fn(),
	getTeam: vi.fn(),
	deleteTeam: vi.fn(),
	getTeamSettings: vi.fn(),
	validateTeamSettings: vi.fn(),
	updateTeam: vi.fn(),
	createTeam: vi.fn(),
	updateTeamData: vi.fn(),
	updateAllTeamData: vi.fn(),
	updateForecast: vi.fn(),
	updateForecastsForTeamPortfolios: vi.fn(),
};

const mockPortfolioService = {
	getPortfolios: vi.fn(),
	getPortfolio: vi.fn(),
	deletePortfolio: vi.fn(),
	getPortfolioSettings: vi.fn(),
	validatePortfolioSettings: vi.fn(),
	updatePortfolio: vi.fn(),
	createPortfolio: vi.fn(),
	updatePortfolioData: vi.fn(),
	refreshForecastsForPortfolio: vi.fn(),
	refreshFeaturesForAllPortfolios: vi.fn(),
	refreshFeaturesForPortfolio: vi.fn(),
};

// Mock the API service context
const mockApiServiceContext = createMockApiServiceContext({
	terminologyService: mockTerminologyService,
	licensingService: mockLicensingService,
	teamService: mockTeamService,
	portfolioService: mockPortfolioService,
});

const renderWithProviders = (component: React.ReactElement) => {
	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	return render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<TerminologyProvider>{component}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

describe("TerminologyConfiguration", () => {
	const mockTerminology = [
		{
			id: 1,
			key: "workItem",
			defaultValue: "Task",
			description: "Individual work item",
			value: "Task",
		},
		{
			id: 2,
			key: "workItems",
			defaultValue: "Tasks",
			description: "Multiple work items",
			value: "Tasks",
		},
	];

	beforeEach(() => {
		vi.clearAllMocks();

		// Default mocks for license status (premium user)
		const premiumLicense: ILicenseStatus = createPremiumLicense();

		mockLicensingService.getLicenseStatus.mockResolvedValue(premiumLicense);
		mockTeamService.getTeams.mockResolvedValue([]);
		mockPortfolioService.getPortfolios.mockResolvedValue([]);
	});

	it("should display loading state initially", () => {
		// Return a promise that doesn't resolve immediately
		mockTerminologyService.getAllTerminology.mockReturnValue(
			new Promise(() => {}), // Never resolves
		);

		renderWithProviders(<TerminologyConfiguration />);

		expect(
			screen.getByText(/loading terminology configuration/i),
		).toBeInTheDocument();
	});

	it("should load and display terminology from the service", async () => {
		const extendedMockTerminology = [
			...mockTerminology,
			{
				id: 3,
				key: "customTerm",
				defaultValue: "Custom Default",
				description: "Custom terminology",
				value: "Custom Value",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(
			extendedMockTerminology,
		);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Task")).toBeInTheDocument();
			expect(screen.getByLabelText("Tasks")).toBeInTheDocument();
			expect(screen.getByLabelText("Custom Default")).toBeInTheDocument();
		});

		// Check that values are displayed correctly
		expect(screen.getByDisplayValue("Task")).toBeInTheDocument();
		expect(screen.getByDisplayValue("Tasks")).toBeInTheDocument();
		expect(screen.getByDisplayValue("Custom Value")).toBeInTheDocument();

		expect(mockTerminologyService.getAllTerminology).toHaveBeenCalled();
	});

	it("should allow editing terminology values", async () => {
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const input = screen.getByLabelText("Work Item");
		fireEvent.change(input, { target: { value: "Task" } });

		expect(screen.getByDisplayValue("Task")).toBeInTheDocument();
	});

	it("should save terminology when save button is clicked", async () => {
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);
		mockTerminologyService.updateTerminology.mockResolvedValue(undefined);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const input = screen.getByLabelText("Work Item");
		fireEvent.change(input, { target: { value: "Task" } });

		const saveButton = screen.getByRole("button", {
			name: /save configuration/i,
		});
		fireEvent.click(saveButton);

		await waitFor(() => {
			expect(mockTerminologyService.updateTerminology).toHaveBeenCalledWith([
				{
					id: 1,
					key: "workItem",
					defaultValue: "Work Item",
					description: "Individual work item",
					value: "Task",
				},
			]);
		});
	});

	it("should display success message after saving", async () => {
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);
		mockTerminologyService.updateTerminology.mockResolvedValue(undefined);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const saveButton = screen.getByRole("button", {
			name: /save configuration/i,
		});
		fireEvent.click(saveButton);

		await waitFor(() => {
			expect(
				screen.getByText(/terminology configuration updated successfully/i),
			).toBeInTheDocument();
		});
	});

	it("should display error message when loading fails", async () => {
		mockTerminologyService.getAllTerminology.mockRejectedValue(
			new Error("Network error"),
		);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(
				screen.getByText(/failed to load terminology configuration/i),
			).toBeInTheDocument();
		});
	});

	it("should display error message when saving fails", async () => {
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);
		mockTerminologyService.updateTerminology.mockRejectedValue(
			new Error("Network error"),
		);

		renderWithProviders(<TerminologyConfiguration />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const saveButton = screen.getByRole("button", {
			name: /save configuration/i,
		});
		fireEvent.click(saveButton);

		await waitFor(() => {
			expect(
				screen.getByText(/failed to save terminology configuration/i),
			).toBeInTheDocument();
		});
	});

	it("should call onClose when cancel button is clicked", async () => {
		const mockOnClose = vi.fn();
		const mockTerminology = [
			{
				id: 1,
				key: "workItem",
				defaultValue: "Work Item",
				description: "Individual work item",
				value: "Work Item",
			},
		];

		mockTerminologyService.getAllTerminology.mockResolvedValue(mockTerminology);

		renderWithProviders(<TerminologyConfiguration onClose={mockOnClose} />);

		await waitFor(() => {
			expect(screen.getByLabelText("Work Item")).toBeInTheDocument();
		});

		const cancelButton = screen.getByRole("button", { name: /cancel/i });
		fireEvent.click(cancelButton);

		expect(mockOnClose).toHaveBeenCalledTimes(1);
	});

	describe("License Restrictions", () => {
		it("should disable all text fields for non-premium users", async () => {
			const freeLicense: ILicenseStatus = createFreeLicense();

			mockLicensingService.getLicenseStatus.mockResolvedValue(freeLicense);
			mockTerminologyService.getAllTerminology.mockResolvedValue(
				mockTerminology,
			);

			renderWithProviders(<TerminologyConfiguration />);

			await waitFor(() => {
				expect(screen.getByLabelText("Task")).toBeInTheDocument();
			});

			const taskInput = screen.getByLabelText("Task");
			const tasksInput = screen.getByLabelText("Tasks");

			expect(taskInput).toBeDisabled();
			expect(tasksInput).toBeDisabled();
		});

		it("should disable save button for non-premium users", async () => {
			const freeLicense: ILicenseStatus = createFreeLicense();

			mockLicensingService.getLicenseStatus.mockResolvedValue(freeLicense);
			mockTerminologyService.getAllTerminology.mockResolvedValue(
				mockTerminology,
			);

			renderWithProviders(<TerminologyConfiguration />);

			await waitFor(() => {
				expect(screen.getByLabelText("Task")).toBeInTheDocument();
			});

			const saveButton = screen.getByRole("button", {
				name: /save configuration/i,
			});

			expect(saveButton).toBeDisabled();
		});

		it("should show premium license tooltip on save button for non-premium users", async () => {
			const user = userEvent.setup();
			const freeLicense: ILicenseStatus = createFreeLicense();

			mockLicensingService.getLicenseStatus.mockResolvedValue(freeLicense);
			mockTerminologyService.getAllTerminology.mockResolvedValue(
				mockTerminology,
			);

			renderWithProviders(<TerminologyConfiguration />);

			await waitFor(() => {
				expect(screen.getByLabelText("Task")).toBeInTheDocument();
			});

			const saveButton = screen.getByRole("button", {
				name: /save configuration/i,
			});

			// Hover over the parent span element (tooltip wrapper) to show the tooltip
			const spanWrapper = saveButton.parentElement;
			if (spanWrapper) {
				await user.hover(spanWrapper);
			}

			expect(
				await screen.findByText(/This feature requires a/i),
			).toBeInTheDocument();
			expect(screen.getByText("premium license.")).toBeInTheDocument();
		});

		it("should enable all fields for premium users", async () => {
			const premiumLicense: ILicenseStatus = createPremiumLicense();

			mockLicensingService.getLicenseStatus.mockResolvedValue(premiumLicense);
			mockTerminologyService.getAllTerminology.mockResolvedValue(
				mockTerminology,
			);

			renderWithProviders(<TerminologyConfiguration />);

			await waitFor(() => {
				expect(screen.getByLabelText("Task")).toBeInTheDocument();
			});

			const taskInput = screen.getByLabelText("Task");
			const tasksInput = screen.getByLabelText("Tasks");
			const saveButton = screen.getByRole("button", {
				name: /save configuration/i,
			});

			expect(taskInput).not.toBeDisabled();
			expect(tasksInput).not.toBeDisabled();
			expect(saveButton).not.toBeDisabled();
		});

		it("should allow editing and saving for premium users", async () => {
			const premiumLicense: ILicenseStatus = createPremiumLicense();

			mockLicensingService.getLicenseStatus.mockResolvedValue(premiumLicense);
			mockTerminologyService.getAllTerminology.mockResolvedValue(
				mockTerminology,
			);
			mockTerminologyService.updateTerminology.mockResolvedValue(undefined);

			renderWithProviders(<TerminologyConfiguration />);

			await waitFor(() => {
				expect(screen.getByLabelText("Task")).toBeInTheDocument();
			});

			const taskInput = screen.getByLabelText("Task");
			fireEvent.change(taskInput, { target: { value: "Work Item" } });

			expect(screen.getByDisplayValue("Work Item")).toBeInTheDocument();

			const saveButton = screen.getByRole("button", {
				name: /save configuration/i,
			});
			fireEvent.click(saveButton);

			await waitFor(() => {
				expect(mockTerminologyService.updateTerminology).toHaveBeenCalledWith([
					{
						id: 1,
						key: "workItem",
						defaultValue: "Task",
						description: "Individual work item",
						value: "Work Item",
					},
					{
						id: 2,
						key: "workItems",
						defaultValue: "Tasks",
						description: "Multiple work items",
						value: "Tasks",
					},
				]);
			});
		});

		it("should enable all fields when license status is null (graceful degradation)", async () => {
			mockLicensingService.getLicenseStatus.mockResolvedValue(null);
			mockTerminologyService.getAllTerminology.mockResolvedValue(
				mockTerminology,
			);

			renderWithProviders(<TerminologyConfiguration />);

			await waitFor(() => {
				expect(screen.getByLabelText("Task")).toBeInTheDocument();
			});

			const taskInput = screen.getByLabelText("Task");
			const tasksInput = screen.getByLabelText("Tasks");
			const saveButton = screen.getByRole("button", {
				name: /save configuration/i,
			});

			expect(taskInput).not.toBeDisabled();
			expect(tasksInput).not.toBeDisabled();
			expect(saveButton).not.toBeDisabled();
		});

		it("should display alert about premium requirement for non-premium users", async () => {
			const freeLicense: ILicenseStatus = createFreeLicense();

			mockLicensingService.getLicenseStatus.mockResolvedValue(freeLicense);
			mockTerminologyService.getAllTerminology.mockResolvedValue(
				mockTerminology,
			);

			renderWithProviders(<TerminologyConfiguration />);

			await waitFor(() => {
				expect(screen.getByLabelText("Task")).toBeInTheDocument();
			});

			expect(
				screen.getByText(/requires a premium license/i),
			).toBeInTheDocument();
		});

		it("should not display premium alert for premium users", async () => {
			const premiumLicense: ILicenseStatus = createPremiumLicense();

			mockLicensingService.getLicenseStatus.mockResolvedValue(premiumLicense);
			mockTerminologyService.getAllTerminology.mockResolvedValue(
				mockTerminology,
			);

			renderWithProviders(<TerminologyConfiguration />);

			await waitFor(() => {
				expect(screen.getByLabelText("Task")).toBeInTheDocument();
			});

			expect(
				screen.queryByText(/requires a premium license/i),
			).not.toBeInTheDocument();
		});
	});

	const createPremiumLicense = (): ILicenseStatus => ({
		hasLicense: true,
		isValid: true,
		canUsePremiumFeatures: true,
	});

	const createFreeLicense = (): ILicenseStatus => ({
		hasLicense: false,
		isValid: false,
		canUsePremiumFeatures: false,
	});
});
