import { expect, test } from "../../fixutres/LighthouseFixture";
import {
	loadDemoScenario,
	waitForBackgroundUpdates,
} from "../../helpers/api/demo";

const DEMO_SCENARIO_ID = 0;
const DEMO_CONNECTION = "Demo Data CSV Connector";
const A_TEAM_WITHOUT_AN_ESTIMATION = "Team Voyager";

// Until the Estimation default exists, the connection page has no defaults section, so this
// walking skeleton waits for it.
test.fixme("@walking_skeleton @US-11 an admin sets the Teams' Estimation once on the connection and a Team without one shows it", async ({
	request,
	overviewPage,
}) => {
	await loadDemoScenario(request, DEMO_SCENARIO_ID);
	await waitForBackgroundUpdates(request);

	const overview = await overviewPage.lighthousePage.open();
	const connection = await overview.editConnection(DEMO_CONNECTION);
	await expect(connection.defaultsSection).toBeVisible();
	await connection.setTheTeamsEstimationDefault("Story Points", "Points");
	const afterTheSave = await connection.saveAnswering(
		"Change the Estimation default?",
	);

	const team = await afterTheSave.goToTeam(A_TEAM_WITHOUT_AN_ESTIMATION);
	const settings = await team.editTeam();

	await expect
		.poll(() => settings.getSelectedEstimation())
		.toBe("Connection default (Story Points, Points)");
});
