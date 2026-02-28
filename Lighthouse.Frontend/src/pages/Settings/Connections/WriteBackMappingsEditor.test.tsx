import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import {
	type IWriteBackMappingDefinition,
	WriteBackAppliesTo,
	WriteBackTargetValueType,
	WriteBackValueSource,
} from "../../../models/WorkTracking/WriteBackMappingDefinition";
import WriteBackMappingsEditor from "./WriteBackMappingsEditor";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: vi.fn(),
}));

const { useLicenseRestrictions } = await import(
	"../../../hooks/useLicenseRestrictions"
);

describe("WriteBackMappingsEditor", () => {
	const mockOnChange = vi.fn();

	const mockPremiumLicense: ILicenseStatus = {
		hasLicense: true,
		isValid: true,
		canUsePremiumFeatures: true,
	};

	const mockFreeLicense: ILicenseStatus = {
		hasLicense: false,
		isValid: false,
		canUsePremiumFeatures: false,
	};

	const sampleAdditionalFields: IAdditionalFieldDefinition[] = [
		{ id: 1, displayName: "Forecast Date", reference: "Custom.ForecastDate" },
		{
			id: 2,
			displayName: "Work Item Age Field",
			reference: "Custom.WorkItemAge",
		},
	];

	const sampleMapping: IWriteBackMappingDefinition = {
		id: 1,
		valueSource: WriteBackValueSource.WorkItemAgeCycleTime,
		appliesTo: WriteBackAppliesTo.Team,
		targetFieldReference: "Custom.WorkItemAge",
		targetValueType: WriteBackTargetValueType.Date,
		dateFormat: null,
	};

	const forecastMapping: IWriteBackMappingDefinition = {
		id: 2,
		valueSource: WriteBackValueSource.ForecastPercentile85,
		appliesTo: WriteBackAppliesTo.Portfolio,
		targetFieldReference: "Custom.ForecastDate",
		targetValueType: WriteBackTargetValueType.FormattedText,
		dateFormat: "yyyy-MM-dd",
	};

	beforeEach(() => {
		mockOnChange.mockClear();
		vi.mocked(useLicenseRestrictions).mockReturnValue({
			canCreateTeam: true,
			canUpdateTeamData: true,
			canCreatePortfolio: true,
			canUpdatePortfolioData: true,
			licenseStatus: mockPremiumLicense,
			maxTeamsWithoutPremium: 3,
			maxPortfoliosWithoutPremium: 1,
		});
	});

	describe("Rendering", () => {
		it("should show info message when no additional fields are defined", () => {
			render(
				<WriteBackMappingsEditor
					additionalFields={[]}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			expect(
				screen.getByText(/define additional fields first/i),
			).toBeInTheDocument();
		});

		it("should show empty state when additional fields exist but no mappings", () => {
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			expect(
				screen.getByText(/no sync mappings configured/i),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: /add sync mapping/i }),
			).toBeInTheDocument();
		});

		it("should render existing mappings", () => {
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[sampleMapping]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			expect(screen.getByText("Work Item Age Field")).toBeInTheDocument();
			expect(screen.getByText("Work Item Age/Cycle Time")).toBeInTheDocument();
			expect(screen.getByText("Team")).toBeInTheDocument();
		});

		it("should render section title as Sync with Source", () => {
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			expect(screen.getByText("Sync with Source")).toBeInTheDocument();
		});
	});

	describe("Adding Mappings", () => {
		it("should open dialog when Add Sync Mapping button is clicked", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			await user.click(
				screen.getByRole("button", { name: /add sync mapping/i }),
			);

			expect(screen.getByRole("dialog")).toBeInTheDocument();
		});

		it("should add a new mapping when dialog is saved", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			await user.click(
				screen.getByRole("button", { name: /add sync mapping/i }),
			);

			// Select target field
			const targetFieldSelect = screen.getByLabelText("Target Field");
			await user.click(targetFieldSelect);
			const targetOption = await screen.findByRole("option", {
				name: "Forecast Date",
			});
			await user.click(targetOption);

			// Select value source
			const valueSourceSelect = screen.getByLabelText("Sync Value");
			await user.click(valueSourceSelect);
			const valueOption = await screen.findByRole("option", {
				name: "Work Item Age/Cycle Time",
			});
			await user.click(valueOption);

			// Save
			await user.click(screen.getByRole("button", { name: /save/i }));

			expect(mockOnChange).toHaveBeenCalledTimes(1);
			const newMappings = mockOnChange.mock.calls[0][0];
			expect(newMappings).toHaveLength(1);
			expect(newMappings[0].targetFieldReference).toBe("Custom.ForecastDate");
			expect(newMappings[0].valueSource).toBe(
				WriteBackValueSource.WorkItemAgeCycleTime,
			);
			expect(newMappings[0].id).toBeLessThan(0);
		});

		it("should close dialog when cancel is clicked", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			await user.click(
				screen.getByRole("button", { name: /add sync mapping/i }),
			);
			expect(screen.getByRole("dialog")).toBeInTheDocument();

			await user.click(screen.getByRole("button", { name: /cancel/i }));

			await waitFor(() => {
				expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			});
			expect(mockOnChange).not.toHaveBeenCalled();
		});
	});

	describe("Deleting Mappings", () => {
		it("should remove a mapping when delete button is clicked", async () => {
			const user = userEvent.setup();
			const mappings = [sampleMapping, forecastMapping];

			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={mappings}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			const deleteButtons = screen.getAllByLabelText("delete");
			await user.click(deleteButtons[0]);

			expect(mockOnChange).toHaveBeenCalledTimes(1);
			const remaining = mockOnChange.mock.calls[0][0];
			expect(remaining).toHaveLength(1);
			expect(remaining[0].id).toBe(forecastMapping.id);
		});
	});

	describe("Applies To filtering", () => {
		it("should hide portfolio-only sources when AppliesTo is Team", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			await user.click(
				screen.getByRole("button", { name: /add sync mapping/i }),
			);

			// Default AppliesTo should be Team
			// Open value source dropdown
			const valueSourceSelect = screen.getByLabelText("Sync Value");
			await user.click(valueSourceSelect);

			// Should see non-portfolio sources
			expect(
				screen.getByRole("option", { name: "Work Item Age/Cycle Time" }),
			).toBeInTheDocument();

			// Should NOT see portfolio-only sources
			expect(
				screen.queryByRole("option", { name: "Feature Size" }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByRole("option", {
					name: "Forecast (85th Percentile)",
				}),
			).not.toBeInTheDocument();
		});

		it("should show all sources when AppliesTo is Portfolio", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			await user.click(
				screen.getByRole("button", { name: /add sync mapping/i }),
			);

			// Change AppliesTo to Portfolio
			const appliesToSelect = screen.getByLabelText("Applies To");
			await user.click(appliesToSelect);
			const portfolioOption = await screen.findByRole("option", {
				name: "Portfolio",
			});
			await user.click(portfolioOption);

			// Open value source dropdown
			const valueSourceSelect = screen.getByLabelText("Sync Value");
			await user.click(valueSourceSelect);

			// Should see all sources
			expect(
				screen.getByRole("option", { name: "Work Item Age/Cycle Time" }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("option", { name: "Feature Size" }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("option", {
					name: "Forecast (85th Percentile)",
				}),
			).toBeInTheDocument();
		});
	});

	describe("Forecast date format", () => {
		it("should show date format options for forecast sources with FormattedText type", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			await user.click(
				screen.getByRole("button", { name: /add sync mapping/i }),
			);

			// Change to Portfolio
			const appliesToSelect = screen.getByLabelText("Applies To");
			await user.click(appliesToSelect);
			await user.click(
				await screen.findByRole("option", { name: "Portfolio" }),
			);

			// Select target field
			const targetFieldSelect = screen.getByLabelText("Target Field");
			await user.click(targetFieldSelect);
			await user.click(
				await screen.findByRole("option", { name: "Forecast Date" }),
			);

			// Select a forecast source
			const valueSourceSelect = screen.getByLabelText("Sync Value");
			await user.click(valueSourceSelect);
			await user.click(
				await screen.findByRole("option", {
					name: "Forecast (85th Percentile)",
				}),
			);

			// Change to FormattedText
			const valueTypeSelect = screen.getByLabelText("Value Type");
			await user.click(valueTypeSelect);
			await user.click(await screen.findByRole("option", { name: "Text" }));

			// Date format field should appear
			expect(screen.getByLabelText("Date Format")).toBeInTheDocument();
		});

		it("should not show date format for non-forecast sources", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			await user.click(
				screen.getByRole("button", { name: /add sync mapping/i }),
			);

			// Select a non-forecast source
			const valueSourceSelect = screen.getByLabelText("Sync Value");
			await user.click(valueSourceSelect);
			await user.click(
				await screen.findByRole("option", { name: "Work Item Age/Cycle Time" }),
			);

			// Value type should not appear for non-forecast sources
			expect(screen.queryByLabelText("Value Type")).not.toBeInTheDocument();
			expect(screen.queryByLabelText("Date Format")).not.toBeInTheDocument();
		});

		it("should not show date format for forecast sources with Date type", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			await user.click(
				screen.getByRole("button", { name: /add sync mapping/i }),
			);

			// Change to Portfolio
			const appliesToSelect = screen.getByLabelText("Applies To");
			await user.click(appliesToSelect);
			await user.click(
				await screen.findByRole("option", { name: "Portfolio" }),
			);

			// Select a forecast source
			const valueSourceSelect = screen.getByLabelText("Sync Value");
			await user.click(valueSourceSelect);
			await user.click(
				await screen.findByRole("option", {
					name: "Forecast (85th Percentile)",
				}),
			);

			// Value type should appear, but default is Date so no date format field
			expect(screen.getByLabelText("Value Type")).toBeInTheDocument();
			expect(screen.queryByLabelText("Date Format")).not.toBeInTheDocument();
		});
	});

	describe("Disabled systems", () => {
		it("should disable Add button when system type is Linear", () => {
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="Linear"
				/>,
			);

			const addButton = screen.getByRole("button", {
				name: /add sync mapping/i,
			});
			expect(addButton).toBeDisabled();
		});

		it("should disable Add button when system type is Csv", () => {
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="Csv"
				/>,
			);

			const addButton = screen.getByRole("button", {
				name: /add sync mapping/i,
			});
			expect(addButton).toBeDisabled();
		});

		it("should enable Add button for Jira", () => {
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="Jira"
				/>,
			);

			const addButton = screen.getByRole("button", {
				name: /add sync mapping/i,
			});
			expect(addButton).toBeEnabled();
		});
	});

	describe("License Restrictions", () => {
		it("should disable Add button when user does not have premium", () => {
			vi.mocked(useLicenseRestrictions).mockReturnValue({
				canCreateTeam: true,
				canUpdateTeamData: true,
				canCreatePortfolio: true,
				canUpdatePortfolioData: true,
				licenseStatus: mockFreeLicense,
				maxTeamsWithoutPremium: 3,
				maxPortfoliosWithoutPremium: 1,
			});

			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			const addButton = screen.getByRole("button", {
				name: /add sync mapping/i,
			});
			expect(addButton).toBeDisabled();
		});

		it("should allow adding mappings for premium users", () => {
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			const addButton = screen.getByRole("button", {
				name: /add sync mapping/i,
			});
			expect(addButton).toBeEnabled();
		});
	});

	describe("Editing Mappings", () => {
		it("should open dialog with existing mapping data when edit is clicked", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[sampleMapping]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			const editButton = screen.getByLabelText("edit");
			await user.click(editButton);

			expect(screen.getByRole("dialog")).toBeInTheDocument();
		});

		it("should update mapping when dialog is saved after editing", async () => {
			const user = userEvent.setup();
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[sampleMapping]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			const editButton = screen.getByLabelText("edit");
			await user.click(editButton);

			// Change applies to Portfolio
			const appliesToSelect = screen.getByLabelText("Applies To");
			await user.click(appliesToSelect);
			await user.click(
				await screen.findByRole("option", { name: "Portfolio" }),
			);

			await user.click(screen.getByRole("button", { name: /save/i }));

			expect(mockOnChange).toHaveBeenCalledTimes(1);
			const updated = mockOnChange.mock.calls[0][0];
			expect(updated).toHaveLength(1);
			expect(updated[0].appliesTo).toBe(WriteBackAppliesTo.Portfolio);
		});
	});

	describe("Mapping display", () => {
		it("should show forecast date format in list for forecast FormattedText mappings", () => {
			render(
				<WriteBackMappingsEditor
					additionalFields={sampleAdditionalFields}
					mappings={[forecastMapping]}
					onChange={mockOnChange}
					workTrackingSystemType="AzureDevOps"
				/>,
			);

			expect(screen.getByText("Forecast Date")).toBeInTheDocument();
			expect(
				screen.getByText("Forecast (85th Percentile)"),
			).toBeInTheDocument();
			expect(screen.getByText("Portfolio")).toBeInTheDocument();
			expect(screen.getByText(/yyyy-MM-dd/)).toBeInTheDocument();
		});
	});
});
