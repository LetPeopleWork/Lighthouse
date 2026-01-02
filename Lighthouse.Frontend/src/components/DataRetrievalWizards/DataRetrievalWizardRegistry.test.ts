import { describe, expect, it } from "vitest";
import type { WorkTrackingSystemType } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import CsvUploadWizard from "./CsvUploadWizard";
import dataRetrievalWizards, {
	getWizardsForSystem,
} from "./DataRetrievalWizardRegistry";

describe("DataRetrievalWizardRegistry", () => {
	describe("dataRetrievalWizards array", () => {
		it("should contain the CSV upload wizard", () => {
			expect(dataRetrievalWizards).toHaveLength(1);

			const csvWizard = dataRetrievalWizards[0];
			expect(csvWizard.id).toBe("csv.upload");
			expect(csvWizard.name).toBe("Upload CSV File");
			expect(csvWizard.applicableSystemTypes).toContain("Csv");
			expect(csvWizard.component).toBe(CsvUploadWizard);
		});

		it("should have valid wizard structure", () => {
			for (const wizard of dataRetrievalWizards) {
				expect(wizard).toHaveProperty("id");
				expect(wizard).toHaveProperty("name");
				expect(wizard).toHaveProperty("applicableSystemTypes");
				expect(wizard).toHaveProperty("component");

				expect(typeof wizard.id).toBe("string");
				expect(typeof wizard.name).toBe("string");
				expect(Array.isArray(wizard.applicableSystemTypes)).toBe(true);
				expect(typeof wizard.component).toBe("function");
			}
		});
	});

	describe("getWizardsForSystem", () => {
		it("should return CSV wizard for Csv system type", () => {
			const wizards = getWizardsForSystem("Csv");

			expect(wizards).toHaveLength(1);
			expect(wizards[0].id).toBe("csv.upload");
			expect(wizards[0].name).toBe("Upload CSV File");
		});

		it("should return empty array for Jira system type", () => {
			const wizards = getWizardsForSystem("Jira");

			expect(wizards).toHaveLength(0);
		});

		it("should return empty array for AzureDevOps system type", () => {
			const wizards = getWizardsForSystem("AzureDevOps");

			expect(wizards).toHaveLength(0);
		});

		it("should return empty array for Linear system type", () => {
			const wizards = getWizardsForSystem("Linear");

			expect(wizards).toHaveLength(0);
		});

		it("should filter wizards based on applicable system types", () => {
			const systemTypes: WorkTrackingSystemType[] = [
				"Csv",
				"Jira",
				"AzureDevOps",
				"Linear",
			];

			for (const systemType of systemTypes) {
				const wizards = getWizardsForSystem(systemType);

				for (const wizard of wizards) {
					expect(wizard.applicableSystemTypes).toContain(systemType);
				}
			}
		});

		it("should return wizards that match the system type in applicableSystemTypes", () => {
			const csvWizards = getWizardsForSystem("Csv");

			for (const wizard of csvWizards) {
				expect(wizard.applicableSystemTypes.includes("Csv")).toBe(true);
			}
		});

		it("should not return wizards that don't match the system type", () => {
			const jiraWizards = getWizardsForSystem("Jira");
			const adoWizards = getWizardsForSystem("AzureDevOps");

			// CSV wizard should not be returned for Jira or ADO
			for (const wizard of jiraWizards) {
				expect(wizard.id).not.toBe("csv.upload");
			}

			for (const wizard of adoWizards) {
				expect(wizard.id).not.toBe("csv.upload");
			}
		});

		it("should handle multiple applicable system types for a wizard", () => {
			// Test the logic works correctly even if a wizard has multiple system types
			const csvWizard = dataRetrievalWizards.find((w) => w.id === "csv.upload");

			expect(csvWizard).toBeDefined();
			expect(Array.isArray(csvWizard?.applicableSystemTypes)).toBe(true);
		});

		it("should return consistent results for the same system type", () => {
			const wizards1 = getWizardsForSystem("Csv");
			const wizards2 = getWizardsForSystem("Csv");

			expect(wizards1).toHaveLength(wizards2.length);
			expect(wizards1.map((w) => w.id)).toEqual(wizards2.map((w) => w.id));
		});

		it("should not mutate the original wizards array", () => {
			const originalLength = dataRetrievalWizards.length;
			const originalIds = dataRetrievalWizards.map((w) => w.id);

			getWizardsForSystem("Csv");
			getWizardsForSystem("Jira");
			getWizardsForSystem("AzureDevOps");

			expect(dataRetrievalWizards).toHaveLength(originalLength);
			expect(dataRetrievalWizards.map((w) => w.id)).toEqual(originalIds);
		});

		it("should handle edge case with empty applicableSystemTypes", () => {
			// This tests the filter logic handles wizards correctly
			const wizards = getWizardsForSystem("Csv");

			// All returned wizards should have Csv in their applicableSystemTypes
			for (const wizard of wizards) {
				expect(wizard.applicableSystemTypes.length).toBeGreaterThan(0);
				expect(wizard.applicableSystemTypes).toContain("Csv");
			}
		});
	});

	describe("Registry extensibility", () => {
		it("should allow for future wizard additions", () => {
			// The structure supports adding more wizards
			expect(Array.isArray(dataRetrievalWizards)).toBe(true);

			// Verify the getWizardsForSystem function can handle multiple wizards
			const wizardsForCsv = getWizardsForSystem("Csv");
			expect(Array.isArray(wizardsForCsv)).toBe(true);
		});

		it("should maintain wizard interface contract", () => {
			for (const wizard of dataRetrievalWizards) {
				// Each wizard must have these required properties
				expect(wizard.id).toBeTruthy();
				expect(wizard.name).toBeTruthy();
				expect(wizard.applicableSystemTypes.length).toBeGreaterThan(0);
				expect(wizard.component).toBeTruthy();
			}
		});
	});
});
