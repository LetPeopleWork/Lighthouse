import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import AdditionalFieldsEditor from "./AdditionalFieldsEditor";
import WriteBackMappingsEditor from "./WriteBackMappingsEditor";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: vi.fn(),
}));

const { useLicenseRestrictions } = await import(
	"../../../hooks/useLicenseRestrictions"
);

const premiumLicense: ILicenseStatus = {
	hasLicense: true,
	isValid: true,
	canUsePremiumFeatures: true,
};

// ServiceNow ships no predefined additional fields, its connector cannot fill custom ones yet, and
// Lighthouse never writes back to ServiceNow, so leaving either control enabled would ship a button
// that does nothing. Easy to miss because both gates are written as "not Linear, not Csv".
describe("Connection settings a ServiceNow administrator should not be offered", () => {
	beforeEach(() => {
		vi.mocked(useLicenseRestrictions).mockReturnValue({
			status: "ready",
			canCreateTeam: true,
			canUpdateTeamData: true,
			canCreatePortfolio: true,
			canUpdatePortfolioData: true,
			licenseStatus: premiumLicense,
			maxTeamsWithoutPremium: 3,
			maxPortfoliosWithoutPremium: 1,
		});
	});

	it("does not let them add an additional field ServiceNow cannot fill", () => {
		render(
			<AdditionalFieldsEditor
				workTrackingSystemType="ServiceNow"
				fields={[]}
				onChange={vi.fn()}
			/>,
		);

		expect(screen.getByRole("button", { name: /add field/i })).toBeDisabled();
	});

	it("does not let them map a value back into ServiceNow", () => {
		// A field has to exist for the editor to offer mapping at all, so the ServiceNow gate is
		// the only thing that can disable the button here.
		render(
			<WriteBackMappingsEditor
				workTrackingSystemType="ServiceNow"
				additionalFields={[
					{ id: 1, displayName: "Forecast", reference: "u_forecast" },
				]}
				mappings={[]}
				onChange={vi.fn()}
			/>,
		);

		expect(
			screen.getByRole("button", { name: /add sync mapping/i }),
		).toBeDisabled();
	});
});
