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

// Story #5574, DoD 5 / KPI 3 — no silent no-ops. ServiceNow brings no predefined additional
// fields in slice 01 and write-back to ServiceNow is permanently out of scope (D8), so leaving
// either control enabled would ship a button that does nothing. Found by inspection during
// DESIGN; easy to miss because both gates are written as "not Linear, not Csv".
describe("Connection settings a ServiceNow administrator should not be offered", () => {
	beforeEach(() => {
		vi.mocked(useLicenseRestrictions).mockReturnValue({
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
				onFieldsChanged={vi.fn()}
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
