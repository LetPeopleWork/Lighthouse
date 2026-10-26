import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import SaveStateIndicator from "../../../components/Common/ValidationActions/SaveStateIndicator";
import type { LicenseCheckStatus } from "../../../hooks/useLicenseRestrictions";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import EditTeamPage from "./EditTeam";

const COULD_NOT_CHECK =
	"Couldn't check your license. Reload the page to try again.";

const NOT_SAVED = `Not saved: ${COULD_NOT_CHECK}`;

const ATLAS = 7;

const { theCheck, theRoute, theForm } = vi.hoisted(() => ({
	/** What the licence check answers in a scenario; the hook's own tests cover how it gets there. */
	theCheck: {
		current: {
			status: "ready" as LicenseCheckStatus,
			canCreateTeam: true,
			canUpdateTeamData: true,
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
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
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

vi.mock("../../../components/Common/Team/ModifyTeamSettings", () => ({
	default: (props: { disableSave?: boolean; notSavedReason?: string }) => {
		theForm.saves = !props.disableSave;
		return (
			<div data-testid="modify-team-settings">
				ModifyTeamSettings
				<SaveStateIndicator
					saveState="idle"
					canSave={!props.disableSave}
					notSavedReason={props.notSavedReason}
				/>
			</div>
		);
	},
}));

const givenTheLicenceCheck = (
	status: LicenseCheckStatus,
	createAllowed: boolean,
) => {
	theCheck.current = {
		status,
		canCreateTeam: createAllowed,
		canUpdateTeamData: true,
	};
};

/** A new Team made as a copy of Atlas: the one way a new Team opens on the settings page rather than the wizard. */
const anAdminCopiesATeam = () => {
	theRoute.id = undefined;
	globalThis.history.pushState({}, "", `/teams/new?cloneFrom=${ATLAS}`);
	return openTheSettingsPage();
};

const anAdminEditsAtlas = () => {
	theRoute.id = String(ATLAS);
	globalThis.history.pushState({}, "", `/teams/${ATLAS}/settings`);
	return openTheSettingsPage();
};

const openTheSettingsPage = async () => {
	render(
		<MemoryRouter>
			<ApiServiceContext.Provider value={{} as IApiServiceContext}>
				<EditTeamPage />
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);
	await screen.findByTestId("modify-team-settings");
};

beforeEach(() => {
	theForm.saves = undefined;
});

afterEach(() => {
	globalThis.history.pushState({}, "", "/");
});

describe("Story 6406 (slice 02): saving a new Team follows the licence check", () => {
	it("while the licence is checked, a copied Team is not saved and the page says nothing about the licence", async () => {
		givenTheLicenceCheck("loading", false);

		await anAdminCopiesATeam();

		expect(theForm.saves).toBe(false);
		expect(screen.queryByText(NOT_SAVED)).not.toBeInTheDocument();
	});

	it("when the licence cannot be checked, a copied Team is not saved and the page says why", async () => {
		givenTheLicenceCheck("failed", false);

		await anAdminCopiesATeam();

		expect(theForm.saves).toBe(false);
		expect(await screen.findByText(NOT_SAVED)).toBeInTheDocument();
	});

	it("once the licence is checked and there is room, a copied Team saves, as today", async () => {
		givenTheLicenceCheck("ready", true);

		await anAdminCopiesATeam();

		expect(theForm.saves).toBe(true);
	});

	it("when the licence cannot be checked, editing an existing Team still saves and says nothing about the licence, as today", async () => {
		givenTheLicenceCheck("failed", false);

		await anAdminEditsAtlas();

		expect(theForm.saves).toBe(true);
		expect(screen.queryByText(NOT_SAVED)).not.toBeInTheDocument();
	});
});
