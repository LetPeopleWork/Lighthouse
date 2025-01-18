import { expect, test } from "../fixutres/LighthouseFixture";

test("clicking page links should new pages", async ({ overviewPage }) => {
	await overviewPage.lightHousePage.goToTeams();
	await overviewPage.lighthousePage.goToProjects();
	await overviewPage.lighthousePage.goToSettings();
});

test("clicking contributors icon should open the contributors page", async ({
	overviewPage,
}) => {
	const contributorsPage = await overviewPage.lightHousePage.goToContributors();

	const pageTitle = await contributorsPage.title();
	expect(pageTitle).toContain("CONTRIBUTORS.md");
});

test("clicking defects icon should open the issues page", async ({
	overviewPage,
}) => {
	const reportIssuePage = await overviewPage.lightHousePage.goToReportIssue();

	const pageTitle = await reportIssuePage.title();
	expect(pageTitle).toContain("Issues");
});

test("clicking documentation icon should open the documentation", async ({
	overviewPage,
}) => {
	const documentationPage = await overviewPage.lightHousePage.goToDocumentation();

	expect(documentationPage.url()).toBe("https://docs.lighthouse.letpeople.work/");
});

test("clicking let people work logo should open let people work page", async ({
	overviewPage,
}) => {
	const letPeopleWorkPage =
		await overviewPage.lightHousePage.goToLetPeopleWork();

	expect(letPeopleWorkPage.url()).toBe("https://www.letpeople.work/");
});

test("clicking version number should open the release page", async ({
	overviewPage,
}) => {
	const releasePage = await overviewPage.lightHousePage.goToRelease();

	expect(releasePage.url()).toContain(
		"https://github.com/LetPeopleWork/Lighthouse/releases/tag/v",
	);
});

test("clicking email icon should open the email client", async ({
	overviewPage,
}) => {
	const emailAddress = await overviewPage.lightHousePage.getEmailContact();

	expect(emailAddress).toContain("mailto:contact@letpeople.work");
});

test("clicking the call button should open calendly", async ({
	overviewPage,
}) => {
	const calendlyPage = await overviewPage.lightHousePage.contactViaCall();

	const pageTitle = await calendlyPage.title();
	expect(pageTitle).toContain("Calendly");
	expect(calendlyPage.url()).toContain("https://calendly.com/letpeoplework");
});

test("clicking the LinkedIn button should open the LPW LinkedIn Page", async ({
	overviewPage,
}) => {
	const linkedInPage = await overviewPage.lightHousePage.contactViaLinkedIn();

	const pageTitle = await linkedInPage.title();
	expect(pageTitle).toContain("Let People Work");
	expect(linkedInPage.url()).toContain(
		"https://www.linkedin.com/company/let-people-work",
	);
});

test("clicking the GitHub Issue button should open the GitHub Lighthouse Issue Page", async ({
	overviewPage,
}) => {
	const gitHubIssuePage =
		await overviewPage.lightHousePage.contactViaGitHubIssue();
		
	expect(gitHubIssuePage.url()).toBe(
		"https://github.com/LetPeopleWork/Lighthouse/issues",
	);
});
