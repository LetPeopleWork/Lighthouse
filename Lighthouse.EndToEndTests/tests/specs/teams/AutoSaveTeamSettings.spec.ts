import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";

const DEMO_SCENARIO_ID = 0;
const TEAM_NAME = "Team Zenith";

test.fixme("@walking_skeleton @real-io @US-01 a team-admin's valid settings edit persists with All changes saved and no Save button", async ({
	page,
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);
	await page.goto("/");

	const teamDetail = await overviewPage.goToTeam(TEAM_NAME);
	const teamEdit = await teamDetail.editTeam();

	await teamEdit.setThroughputHistory(60);
	await teamEdit.waitForChangesSaved();
	expect(await teamEdit.hasSaveButton()).toBe(false);

	await page.reload();
	expect(await teamEdit.getThroughputHistory()).toBe(60);
});
