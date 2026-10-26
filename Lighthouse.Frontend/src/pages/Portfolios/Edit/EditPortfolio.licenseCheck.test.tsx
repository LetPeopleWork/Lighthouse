import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import SaveStateIndicator from "../../../components/Common/ValidationActions/SaveStateIndicator";
import type { LicenseCheckStatus } from "../../../hooks/useLicenseRestrictions";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import EditPortfolioPage from "./EditPortfolio";

const COULD_NOT_CHECK =
	"Couldn't check your license. Reload the page to try again.";

const NOT_SAVED = `Not saved: ${COULD_NOT_CHECK}`;

const HARBOR = 7;

const { theCheck, theRoute, theForm } = vi.hoisted(() => ({
	/** What the licence check answers in a scenario; the hook's own tests cover how it gets there. */
	theCheck: {
		current: {
			status: "ready" as LicenseCheckStatus,
			canCreatePortfolio: true,
			canUpdatePortfolioData: true,
		},
	},
	theRoute: { id: undefined as string | undefined },
	/** Whether the page lets its settings form save, as the form is told. */
	theForm: { saves: undefined as boolean | undefined },
}));

vi.mock("react-router", async (importOriginal) => ({
	...(await importOriginal<typeof import("react-router")>()),
	useNavigate: () => vi.fn(),
	useParams: () => theRoute,
}));

vi.mock("../../../hooks/useRbacGate", () => ({
	useRbacGate: () => ({ allowed: true, isLoading: false }),
}));

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		...theCheck.current,
		canCreateTeam: true,
		canUpdateTeamData: true,
		licenseStatus: null,
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => vi.fn(),
	}),
);

vi.mock(
	"../../../components/Common/ProjectSettings/ModifyProjectSettings",
	() => ({
		default: (props: { disableSave?: boolean; notSavedReason?: string }) => {
			theForm.saves = !props.disableSave;
			return (
				<div data-testid="modify-project-settings">
					ModifyProjectSettings
					<SaveStateIndicator
						saveState="idle"
						canSave={!props.disableSave}
						notSavedReason={props.notSavedReason}
					/>
				</div>
			);
		},
	}),
);

const givenTheLicenceCheck = (
	status: LicenseCheckStatus,
	createAllowed: boolean,
) => {
	theCheck.current = {
		status,
		canCreatePortfolio: createAllowed,
		canUpdatePortfolioData: true,
	};
};

/** A new Portfolio made as a copy of Harbor: the one way a new Portfolio opens on the settings page rather than the wizard. */
const anAdminCopiesAPortfolio = () => {
	theRoute.id = undefined;
	globalThis.history.pushState({}, "", `/portfolios/new?cloneFrom=${HARBOR}`);
	return openTheSettingsPage();
};

const anAdminEditsHarbor = () => {
	theRoute.id = String(HARBOR);
	globalThis.history.pushState({}, "", `/portfolios/${HARBOR}/settings`);
	return openTheSettingsPage();
};

const openTheSettingsPage = async () => {
	render(
		<MemoryRouter>
			<ApiServiceContext.Provider value={{} as IApiServiceContext}>
				<EditPortfolioPage />
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);
	await screen.findByTestId("modify-project-settings");
};

beforeEach(() => {
	theForm.saves = undefined;
});

afterEach(() => {
	globalThis.history.pushState({}, "", "/");
});

describe("Story 6406 (slice 02): saving a new Portfolio follows the licence check", () => {
	it("while the licence is checked, a copied Portfolio is not saved and the page says nothing about the licence", async () => {
		givenTheLicenceCheck("loading", false);

		await anAdminCopiesAPortfolio();

		expect(theForm.saves).toBe(false);
		expect(screen.queryByText(NOT_SAVED)).not.toBeInTheDocument();
	});

	it("when the licence cannot be checked, a copied Portfolio is not saved and the page says why", async () => {
		givenTheLicenceCheck("failed", false);

		await anAdminCopiesAPortfolio();

		expect(theForm.saves).toBe(false);
		expect(await screen.findByText(NOT_SAVED)).toBeInTheDocument();
	});

	it("once the licence is checked and there is room, a copied Portfolio saves, as today", async () => {
		givenTheLicenceCheck("ready", true);

		await anAdminCopiesAPortfolio();

		expect(theForm.saves).toBe(true);
	});

	it("when the licence cannot be checked, editing an existing Portfolio still saves and says nothing about the licence, as today", async () => {
		givenTheLicenceCheck("failed", false);

		await anAdminEditsHarbor();

		expect(theForm.saves).toBe(true);
		expect(screen.queryByText(NOT_SAVED)).not.toBeInTheDocument();
	});
});
