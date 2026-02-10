import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import AdditionalFieldsEditor from "./AdditionalFieldsEditor";

// Mock the useLicenseRestrictions hook
vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: vi.fn(),
}));

const { useLicenseRestrictions } = await import(
	"../../../hooks/useLicenseRestrictions"
);

describe("AdditionalFieldsEditor", () => {
	const mockOnChange = vi.fn();
	const mockOnFieldsChanged = vi.fn();

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

	beforeEach(() => {
		mockOnChange.mockClear();
		mockOnFieldsChanged.mockClear();

		// Default to premium license for existing tests
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
		it("should render with no fields", () => {
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			expect(
				screen.getByText("No additional fields configured."),
			).toBeInTheDocument();
			expect(screen.getByText("Additional Fields")).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: /add field/i }),
			).toBeInTheDocument();
		});

		it("should render with existing fields", () => {
			const fields: IAdditionalFieldDefinition[] = [
				{
					id: 1,
					displayName: "Iteration Path",
					reference: "System.IterationPath",
				},
				{
					id: 2,
					displayName: "Story Points",
					reference: "Microsoft.VSTS.Scheduling.StoryPoints",
				},
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			expect(screen.getByText("Iteration Path")).toBeInTheDocument();
			expect(screen.getByText("System.IterationPath")).toBeInTheDocument();
			expect(screen.getByText("Story Points")).toBeInTheDocument();
			expect(
				screen.getByText("Microsoft.VSTS.Scheduling.StoryPoints"),
			).toBeInTheDocument();
		});

		it("should render edit and delete buttons for each field", () => {
			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Test Field", reference: "test.field" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const editButtons = screen.getAllByLabelText("edit");
			const deleteButtons = screen.getAllByLabelText("delete");

			expect(editButtons).toHaveLength(1);
			expect(deleteButtons).toHaveLength(1);
		});
	});

	describe("Adding Fields", () => {
		it("should open dialog when Add Field button is clicked", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			expect(screen.getByRole("dialog")).toBeInTheDocument();
			// Dialog title now has icon, so check for label text
			expect(screen.getByLabelText("Display Name")).toBeInTheDocument();
			expect(screen.getByLabelText("Field Reference")).toBeInTheDocument();
		});

		it("should add a new field when save is clicked", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// Open dialog
			await user.click(screen.getByRole("button", { name: /add field/i }));

			// Fill in field details
			const displayNameInput = screen.getByLabelText("Display Name");
			const referenceInput = screen.getByLabelText("Field Reference");

			await user.type(displayNameInput, "Custom Field");
			await user.type(referenceInput, "customfield_10001");

			// Save
			await user.click(screen.getByRole("button", { name: /save/i }));

			// Verify onChange was called with the new field
			expect(mockOnChange).toHaveBeenCalledTimes(1);
			const newFields = mockOnChange.mock.calls[0][0];
			expect(newFields).toHaveLength(1);
			expect(newFields[0].displayName).toBe("Custom Field");
			expect(newFields[0].reference).toBe("customfield_10001");
			expect(newFields[0].id).toBeLessThan(0); // Temporary ID
		});

		it("should trim whitespace from field values", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			await user.type(screen.getByLabelText("Display Name"), "  Test Field  ");
			await user.type(
				screen.getByLabelText("Field Reference"),
				"  test.reference  ",
			);

			await user.click(screen.getByRole("button", { name: /save/i }));

			const newFields = mockOnChange.mock.calls[0][0];
			expect(newFields[0].displayName).toBe("Test Field");
			expect(newFields[0].reference).toBe("test.reference");
		});

		it("should disable save button when fields are empty", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			const saveButton = screen.getByRole("button", { name: /save/i });
			expect(saveButton).toBeDisabled();
		});

		it("should disable save button when display name is empty", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			await user.type(
				screen.getByLabelText("Field Reference"),
				"test.reference",
			);

			const saveButton = screen.getByRole("button", { name: /save/i });
			expect(saveButton).toBeDisabled();
		});

		it("should disable save button when reference is empty", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			await user.type(screen.getByLabelText("Display Name"), "Test Field");

			const saveButton = screen.getByRole("button", { name: /save/i });
			expect(saveButton).toBeDisabled();
		});

		it("should close dialog when cancel is clicked", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));
			expect(screen.getByRole("dialog")).toBeInTheDocument();

			await user.click(screen.getByRole("button", { name: /cancel/i }));

			await waitFor(() => {
				expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			});
			expect(mockOnChange).not.toHaveBeenCalled();
		});
	});

	describe("Editing Fields", () => {
		it("should open dialog with field data when edit button is clicked", async () => {
			const user = userEvent.setup();
			const fields: IAdditionalFieldDefinition[] = [
				{
					id: 1,
					displayName: "Original Name",
					reference: "original.reference",
				},
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const editButton = screen.getByLabelText("edit");
			await user.click(editButton);

			expect(screen.getByRole("dialog")).toBeInTheDocument();
			expect(screen.getByText("Edit Field")).toBeInTheDocument();
			expect(screen.getByDisplayValue("Original Name")).toBeInTheDocument();
			expect(
				screen.getByDisplayValue("original.reference"),
			).toBeInTheDocument();
		});

		it("should update field when save is clicked after editing", async () => {
			const user = userEvent.setup();
			const fields: IAdditionalFieldDefinition[] = [
				{
					id: 1,
					displayName: "Original Name",
					reference: "original.reference",
				},
				{ id: 2, displayName: "Other Field", reference: "other.reference" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// Click edit on first field
			const editButtons = screen.getAllByLabelText("edit");
			await user.click(editButtons[0]);

			// Update fields
			const displayNameInput = screen.getByLabelText("Display Name");
			const referenceInput = screen.getByLabelText("Field Reference");

			await user.clear(displayNameInput);
			await user.type(displayNameInput, "Updated Name");

			await user.clear(referenceInput);
			await user.type(referenceInput, "updated.reference");

			await user.click(screen.getByRole("button", { name: /save/i }));

			// Verify onChange was called with updated field
			expect(mockOnChange).toHaveBeenCalledTimes(1);
			const updatedFields = mockOnChange.mock.calls[0][0];
			expect(updatedFields).toHaveLength(2);
			expect(updatedFields[0].id).toBe(1);
			expect(updatedFields[0].displayName).toBe("Updated Name");
			expect(updatedFields[0].reference).toBe("updated.reference");
			// Second field should remain unchanged
			expect(updatedFields[1]).toEqual(fields[1]);
		});
	});

	describe("Deleting Fields", () => {
		it("should delete field when delete button is clicked", async () => {
			const user = userEvent.setup();
			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Field 1", reference: "field.1" },
				{ id: 2, displayName: "Field 2", reference: "field.2" },
				{ id: 3, displayName: "Field 3", reference: "field.3" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// Delete second field
			const deleteButtons = screen.getAllByLabelText("delete");
			await user.click(deleteButtons[1]);

			// Verify onChange was called with remaining fields
			expect(mockOnChange).toHaveBeenCalledTimes(1);
			const remainingFields = mockOnChange.mock.calls[0][0];
			expect(remainingFields).toHaveLength(2);
			expect(remainingFields[0].id).toBe(1);
			expect(remainingFields[1].id).toBe(3);
		});
	});

	describe("Multiple Field Operations", () => {
		it("should handle adding multiple fields", async () => {
			const user = userEvent.setup();
			const { rerender } = render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// Add first field
			await user.click(screen.getByRole("button", { name: /add field/i }));
			await user.type(screen.getByLabelText("Display Name"), "Field 1");
			await user.type(screen.getByLabelText("Field Reference"), "field.1");
			await user.click(screen.getByRole("button", { name: /save/i }));

			const firstCall = mockOnChange.mock.calls[0][0];
			expect(firstCall).toHaveLength(1);

			// Simulate re-render with updated fields
			rerender(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={firstCall}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// Wait for dialog to close
			await waitFor(() => {
				expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			});

			// Add second field
			await user.click(screen.getByRole("button", { name: /add field/i }));
			await user.type(screen.getByLabelText("Display Name"), "Field 2");
			await user.type(screen.getByLabelText("Field Reference"), "field.2");
			await user.click(screen.getByRole("button", { name: /save/i }));

			const secondCall = mockOnChange.mock.calls[1][0];
			expect(secondCall).toHaveLength(2);
		});

		it("should assign unique temporary IDs to new fields", async () => {
			const user = userEvent.setup();
			const { rerender } = render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// Add first field
			await user.click(screen.getByRole("button", { name: /add field/i }));
			await user.type(screen.getByLabelText("Display Name"), "Field 1");
			await user.type(screen.getByLabelText("Field Reference"), "field.1");
			await user.click(screen.getByRole("button", { name: /save/i }));

			const firstId = mockOnChange.mock.calls[0][0][0].id;

			rerender(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={mockOnChange.mock.calls[0][0]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// Wait for dialog to close
			await waitFor(() => {
				expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			});

			// Add second field
			await user.click(screen.getByRole("button", { name: /add field/i }));
			await user.type(screen.getByLabelText("Display Name"), "Field 2");
			await user.type(screen.getByLabelText("Field Reference"), "field.2");
			await user.click(screen.getByRole("button", { name: /save/i }));

			const secondId = mockOnChange.mock.calls[1][0][1].id;

			// Both should be negative (temporary IDs)
			expect(firstId).toBeLessThan(0);
			expect(secondId).toBeLessThan(0);
			// And they should be different
			expect(firstId).not.toBe(secondId);
		});
	});

	describe("Dialog Behavior", () => {
		it("should reset dialog fields when opening for a new field", async () => {
			const user = userEvent.setup();
			const fields: IAdditionalFieldDefinition[] = [
				{
					id: 1,
					displayName: "Existing Field",
					reference: "existing.reference",
				},
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// Open edit dialog
			await user.click(screen.getByLabelText("edit"));
			expect(screen.getByDisplayValue("Existing Field")).toBeInTheDocument();

			// Close dialog
			await user.click(screen.getByRole("button", { name: /cancel/i }));

			// Wait for dialog to close
			await waitFor(() => {
				expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
			});

			// Open add new field dialog
			await user.click(screen.getByRole("button", { name: /add field/i }));

			// Fields should be empty
			const displayNameInput =
				screen.getByLabelText<HTMLInputElement>("Display Name");
			const referenceInput =
				screen.getByLabelText<HTMLInputElement>("Field Reference");
			expect(displayNameInput.value).toBe("");
			expect(referenceInput.value).toBe("");
		});

		it("should show helper text for both input fields", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			expect(
				screen.getByText("A user-friendly name for this field"),
			).toBeInTheDocument();
			expect(
				screen.getByText(
					"Azure DevOps field reference (e.g., 'System.IterationPath', 'Custom.MyField') or name (e.g. 'Iteration Path' or 'My Field')",
				),
			).toBeInTheDocument();
		});
	});

	describe("Edge Cases", () => {
		it("should handle whitespace-only input as invalid", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			await user.type(screen.getByLabelText("Display Name"), "   ");
			await user.type(screen.getByLabelText("Field Reference"), "   ");

			const saveButton = screen.getByRole("button", { name: /save/i });
			expect(saveButton).toBeDisabled();
		});

		it("should handle fields with special characters", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			await user.type(screen.getByLabelText("Display Name"), "Field-Name_123");
			await user.type(
				screen.getByLabelText("Field Reference"),
				"custom.field{[}123{]}",
			);

			await user.click(screen.getByRole("button", { name: /save/i }));

			const newFields = mockOnChange.mock.calls[0][0];
			expect(newFields[0].displayName).toBe("Field-Name_123");
			expect(newFields[0].reference).toBe("custom.field[123]");
		});

		it("should handle empty fields array", () => {
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			expect(
				screen.getByText(/no additional fields configured/i),
			).toBeInTheDocument();
		});
	});

	describe("Disabled State for Unsupported Systems", () => {
		it("should disable Add Field button when system type is null", () => {
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType={null}
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const addButton = screen.getByRole("button", { name: /add field/i });
			expect(addButton).toBeDisabled();
		});

		it("should disable Add Field button when system type is Linear", () => {
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="Linear"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const addButton = screen.getByRole("button", { name: /add field/i });
			expect(addButton).toBeDisabled();
		});

		it("should enable Add Field button for AzureDevOps", () => {
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const addButton = screen.getByRole("button", { name: /add field/i });
			expect(addButton).toBeEnabled();
		});

		it("should enable Add Field button for Jira", () => {
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="Jira"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const addButton = screen.getByRole("button", { name: /add field/i });
			expect(addButton).toBeEnabled();
		});

		it("should disable edit and delete buttons when system type is null", () => {
			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Test Field", reference: "test.field" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType={null}
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const editButton = screen.getByLabelText("edit");
			const deleteButton = screen.getByLabelText("delete");

			expect(editButton).toBeDisabled();
			expect(deleteButton).toBeDisabled();
		});

		it("should disable edit and delete buttons when system type is Linear", () => {
			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Test Field", reference: "test.field" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="Linear"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const editButton = screen.getByLabelText("edit");
			const deleteButton = screen.getByLabelText("delete");

			expect(editButton).toBeDisabled();
			expect(deleteButton).toBeDisabled();
		});

		it("should enable edit and delete buttons for supported systems", () => {
			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Test Field", reference: "test.field" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const editButton = screen.getByLabelText("edit");
			const deleteButton = screen.getByLabelText("delete");

			expect(editButton).toBeEnabled();
			expect(deleteButton).toBeEnabled();
		});
	});

	describe("System-Specific Helper Text", () => {
		it("should show Azure DevOps specific helper text", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			expect(
				screen.getByText(
					"Azure DevOps field reference (e.g., 'System.IterationPath', 'Custom.MyField') or name (e.g. 'Iteration Path' or 'My Field')",
				),
			).toBeInTheDocument();
		});

		it("should show Jira specific helper text", async () => {
			const user = userEvent.setup();
			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="Jira"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			await user.click(screen.getByRole("button", { name: /add field/i }));

			expect(
				screen.getByText(
					"'Name' or 'Key' of Jira Field. Can be a custom field or a built-in field (e.g., 'Flagged', 'Our Custom Field', 'customfield_10011')",
				),
			).toBeInTheDocument();
		});

		describe("Info Icon and Documentation Links", () => {
			it("should render info icon in dialog title", async () => {
				const user = userEvent.setup();
				render(
					<AdditionalFieldsEditor
						workTrackingSystemType="AzureDevOps"
						fields={[]}
						onChange={mockOnChange}
						onFieldsChanged={mockOnFieldsChanged}
					/>,
				);

				await user.click(screen.getByRole("button", { name: /add field/i }));

				const infoLink = screen.getByLabelText("View documentation");
				expect(infoLink).toBeInTheDocument();
			});

			it("should have Azure DevOps documentation link", async () => {
				const user = userEvent.setup();
				render(
					<AdditionalFieldsEditor
						workTrackingSystemType="AzureDevOps"
						fields={[]}
						onChange={mockOnChange}
						onFieldsChanged={mockOnFieldsChanged}
					/>,
				);

				await user.click(screen.getByRole("button", { name: /add field/i }));

				const infoLink = screen.getByLabelText("View documentation");
				expect(infoLink).toHaveAttribute(
					"href",
					"https://learn.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field?view=azure-devops",
				);
				expect(infoLink).toHaveAttribute("target", "_blank");
				expect(infoLink).toHaveAttribute("rel", "noopener noreferrer");
			});

			it("should have Jira documentation link", async () => {
				const user = userEvent.setup();
				render(
					<AdditionalFieldsEditor
						workTrackingSystemType="Jira"
						fields={[]}
						onChange={mockOnChange}
						onFieldsChanged={mockOnFieldsChanged}
					/>,
				);

				await user.click(screen.getByRole("button", { name: /add field/i }));

				const infoLink = screen.getByLabelText("View documentation");
				expect(infoLink).toHaveAttribute(
					"href",
					"https://confluence.atlassian.com/jirakb/find-my-custom-field-id-number-in-jira-744522503.html",
				);
				expect(infoLink).toHaveAttribute("target", "_blank");
				expect(infoLink).toHaveAttribute("rel", "noopener noreferrer");
			});
		});
	});

	describe("License Restrictions", () => {
		it("should disable Add Field button when free user has 1 field already", () => {
			vi.mocked(useLicenseRestrictions).mockReturnValue({
				canCreateTeam: true,
				canUpdateTeamData: true,
				canCreatePortfolio: true,
				canUpdatePortfolioData: true,
				licenseStatus: mockFreeLicense,
				maxTeamsWithoutPremium: 3,
				maxPortfoliosWithoutPremium: 1,
			});

			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Custom Field", reference: "custom.field" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const addButton = screen.getByRole("button", { name: /add field/i });
			expect(addButton).toBeDisabled();
		});

		it("should show license tooltip on disabled Add Field button for free users at limit", async () => {
			const user = userEvent.setup();
			vi.mocked(useLicenseRestrictions).mockReturnValue({
				canCreateTeam: true,
				canUpdateTeamData: true,
				canCreatePortfolio: true,
				canUpdatePortfolioData: true,
				licenseStatus: mockFreeLicense,
				maxTeamsWithoutPremium: 3,
				maxPortfoliosWithoutPremium: 1,
			});

			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Custom Field", reference: "custom.field" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const addButton = screen.getByRole("button", { name: /add field/i });
			const spanWrapper = addButton.parentElement;

			if (spanWrapper) {
				await user.hover(spanWrapper);

				await waitFor(() => {
					expect(
						screen.getByText(/This feature requires a/i),
					).toBeInTheDocument();
				});
			}
		});

		it("should show alert message when free user is at limit", () => {
			vi.mocked(useLicenseRestrictions).mockReturnValue({
				canCreateTeam: true,
				canUpdateTeamData: true,
				canCreatePortfolio: true,
				canUpdatePortfolioData: true,
				licenseStatus: mockFreeLicense,
				maxTeamsWithoutPremium: 3,
				maxPortfoliosWithoutPremium: 1,
			});

			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Custom Field", reference: "custom.field" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			expect(
				screen.getByText(/reached the limit of 1 additional field/i),
			).toBeInTheDocument();
		});

		it("should allow premium users to add unlimited fields", () => {
			vi.mocked(useLicenseRestrictions).mockReturnValue({
				canCreateTeam: true,
				canUpdateTeamData: true,
				canCreatePortfolio: true,
				canUpdatePortfolioData: true,
				licenseStatus: mockPremiumLicense,
				maxTeamsWithoutPremium: 3,
				maxPortfoliosWithoutPremium: 1,
			});

			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Field 1", reference: "field.1" },
				{ id: 2, displayName: "Field 2", reference: "field.2" },
				{ id: 3, displayName: "Field 3", reference: "field.3" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const addButton = screen.getByRole("button", { name: /add field/i });
			expect(addButton).not.toBeDisabled();
		});

		it("should allow free users to add 1 field when they have 0 fields", () => {
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
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			const addButton = screen.getByRole("button", { name: /add field/i });
			expect(addButton).not.toBeDisabled();
		});

		it("should not show alert when free user has 0 fields", () => {
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
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={[]}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			expect(
				screen.queryByText(/reached the limit of 1 additional field/i),
			).not.toBeInTheDocument();
		});

		it("should display all existing fields even when over limit for free users", () => {
			vi.mocked(useLicenseRestrictions).mockReturnValue({
				canCreateTeam: true,
				canUpdateTeamData: true,
				canCreatePortfolio: true,
				canUpdatePortfolioData: true,
				licenseStatus: mockFreeLicense,
				maxTeamsWithoutPremium: 3,
				maxPortfoliosWithoutPremium: 1,
			});

			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Field 1", reference: "field.1" },
				{ id: 2, displayName: "Field 2", reference: "field.2" },
				{ id: 3, displayName: "Field 3", reference: "field.3" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// All 3 fields should be visible
			expect(screen.getByText("Field 1")).toBeInTheDocument();
			expect(screen.getByText("Field 2")).toBeInTheDocument();
			expect(screen.getByText("Field 3")).toBeInTheDocument();

			// But add button should be disabled
			const addButton = screen.getByRole("button", { name: /add field/i });
			expect(addButton).toBeDisabled();
		});

		it("should not show alert when premium user has many fields", () => {
			vi.mocked(useLicenseRestrictions).mockReturnValue({
				canCreateTeam: true,
				canUpdateTeamData: true,
				canCreatePortfolio: true,
				canUpdatePortfolioData: true,
				licenseStatus: mockPremiumLicense,
				maxTeamsWithoutPremium: 3,
				maxPortfoliosWithoutPremium: 1,
			});

			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Field 1", reference: "field.1" },
				{ id: 2, displayName: "Field 2", reference: "field.2" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			expect(
				screen.queryByText(/reached the limit of 1 additional field/i),
			).not.toBeInTheDocument();
		});

		it("should handle null license status gracefully", () => {
			vi.mocked(useLicenseRestrictions).mockReturnValue({
				canCreateTeam: true,
				canUpdateTeamData: true,
				canCreatePortfolio: true,
				canUpdatePortfolioData: true,
				licenseStatus: null,
				maxTeamsWithoutPremium: 3,
				maxPortfoliosWithoutPremium: 1,
			});

			const fields: IAdditionalFieldDefinition[] = [
				{ id: 1, displayName: "Field 1", reference: "field.1" },
			];

			render(
				<AdditionalFieldsEditor
					workTrackingSystemType="AzureDevOps"
					fields={fields}
					onChange={mockOnChange}
					onFieldsChanged={mockOnFieldsChanged}
				/>,
			);

			// Should disable when license is null and has 1+ fields (safe default)
			const addButton = screen.getByRole("button", { name: /add field/i });
			expect(addButton).toBeDisabled();
		});
	});
});
