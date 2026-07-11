import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import AdditionalFieldsEditor from "./AdditionalFieldsEditor";

// DISTILL acceptance spec (Epic 5074) — Slice 05: the predefined (system-owned) additional field is
// surfaced read-only. The editable list filters `!isPredefined`; predefined fields are neither editable
// nor deletable and do not consume a user field slot. The rule field-key picker (separately) DOES include
// predefined fields — covered by the backend rule-key scenario (Slice05, offered-as-rule-key).
//
// The DTO's `isPredefined` flag does not exist on today's IAdditionalFieldDefinition, so these tests type
// the fixtures with a local extension — the spec COMPILES (tsc) against today's model while pinning the
// not-yet-implemented split. The pending block is `describe.skip` (RED-ready): un-skip in DELIVER, one at a
// time, once the editor filters predefined fields out of the editable list.

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: vi.fn(),
}));

const { useLicenseRestrictions } = await import(
	"../../../hooks/useLicenseRestrictions"
);

type PredefinedAdditionalField = IAdditionalFieldDefinition & {
	isPredefined?: boolean;
};

const premiumLicense: ILicenseStatus = {
	hasLicense: true,
	isValid: true,
	canUsePremiumFeatures: true,
};

const freeLicense: ILicenseStatus = {
	hasLicense: false,
	isValid: false,
	canUsePremiumFeatures: false,
};

const withLicense = (licenseStatus: ILicenseStatus) => {
	vi.mocked(useLicenseRestrictions).mockReturnValue({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus,
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	});
};

const renderEditor = (fields: PredefinedAdditionalField[]) =>
	render(
		<AdditionalFieldsEditor
			workTrackingSystemType="Jira"
			fields={fields}
			onChange={vi.fn()}
			onFieldsChanged={vi.fn()}
		/>,
	);

const userField: PredefinedAdditionalField = {
	id: 1,
	displayName: "Story Points",
	reference: "customfield_10050",
};

const predefinedFlaggedField: PredefinedAdditionalField = {
	id: 2,
	displayName: "Flagged",
	reference: "customfield_10001",
	isPredefined: true,
};

describe("AdditionalFieldsEditor — user fields (control)", () => {
	beforeEach(() => {
		withLicense(premiumLicense);
	});

	it("lists a user-configured field as editable and deletable", () => {
		renderEditor([userField]);

		expect(screen.getByText("Story Points")).toBeInTheDocument();
		expect(screen.getByLabelText("edit")).toBeInTheDocument();
		expect(screen.getByLabelText("delete")).toBeInTheDocument();
	});
});

describe.skip("AdditionalFieldsEditor — predefined field split (Slice 05, pending)", () => {
	beforeEach(() => {
		withLicense(premiumLicense);
	});

	it("does not list a predefined field among the editable fields", () => {
		renderEditor([userField, predefinedFlaggedField]);

		expect(screen.getByText("Story Points")).toBeInTheDocument();
		// The predefined "Flagged" field is system-owned: it must NOT appear in the user-editable list.
		expect(screen.queryByText("Flagged")).not.toBeInTheDocument();
	});

	it("does not offer edit or delete controls for a predefined field", () => {
		renderEditor([predefinedFlaggedField]);

		// With only a predefined field present, the editable list is empty — no edit/delete affordances.
		expect(screen.queryByLabelText("edit")).not.toBeInTheDocument();
		expect(screen.queryByLabelText("delete")).not.toBeInTheDocument();
	});

	it("does not count a predefined field against the free-plan field slot", () => {
		withLicense(freeLicense);

		// A single predefined field is present but consumes no user slot: a free-plan admin can still add
		// their one allowed field.
		renderEditor([predefinedFlaggedField]);

		expect(screen.getByRole("button", { name: /add field/i })).toBeEnabled();
	});
});
