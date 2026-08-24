import { expect, test } from "../../fixutres/LighthouseFixture";
import { waitForBackgroundUpdates } from "../../helpers/api/demo";
import { createPortfolio } from "../../helpers/api/portfolios";
import { createTeam } from "../../helpers/api/teams";
import { createJiraConnection } from "../../helpers/api/workTrackingSystemConnections";
import { generateRandomName } from "../../helpers/names";

const jiraStates = {
	toDo: ["To Do"],
	doing: ["In Progress"],
	done: ["Done"],
};

// Four real Epics on the letpeoplework demo board, all four tagged with the "Elixir Project"
// Release. Naming them one by one, instead of taking the whole project, is what keeps the number on
// screen a fact rather than a reading of today's board - the nightly demo job adds Epics to this
// project and none of them carry the Release, so tomorrow's board cannot move this count.
const THE_FOUR_TAGGED_EPICS =
	"project = LGHTHSDMO AND key IN (LGHTHSDMO-7, LGHTHSDMO-8, LGHTHSDMO-9, LGHTHSDMO-10)";

const THE_DATED_RELEASE = "Elixir Project";
const ONE_RELEASE_WITHOUT_A_DATE = "Oberon Initiative";
const THE_OTHER_RELEASE_WITHOUT_A_DATE = "Dart Release";

const TAGGED_EPICS = [
	"Spotlight Finder",
	"SnapShare Hub",
	"BlinkList Directory",
	"TrendSpotter Insights",
];

// The picker asks Jira for the Releases of every project the credential can see, one call each, so
// the list takes a couple of seconds to fill on this instance.
const RELEASES_ARRIVE_FROM_JIRA = 60_000;

// Saving reads the Release's Features out of Jira again before the new row can be drawn.
const THE_SAVED_DELIVERY_COMES_BACK = 60_000;

// The date field holds a plain year-month-day; the preview prints the same day the way this browser
// writes dates. Converting one into the other is what lets the two be compared without writing the
// date down here, which would go stale the day somebody moves it in Jira.
const asShownToTheReader = (yearMonthDay: string): string =>
	new Date(`${yearMonthDay}T00:00:00`).toLocaleDateString();

// The walking skeleton for the whole slice: Releases a person made in Jira, the dates they did and
// did not give them, a forecaster reading off what binding one would mean, and then the Delivery
// that binding leaves behind.
//
// Everything else sits a layer down and is covered there:
//   - which Releases are offered, and why an archived or released one is not -> the backend
//     delivery-source scenarios
//   - the tab's absence on a connection with nothing to offer, the loading state and the empty
//     preview -> DeliverySourceTab.test.tsx
//   - refusing an edit that fights the binding, and letting go of one -> the backend scenarios and
//     DeliverySection.provenance.test.tsx
test("@walking_skeleton a dated Jira Release picked in the form becomes a Delivery carrying its date, its Features and the mark that it follows that Release", async ({
	request,
	overviewPage,
}) => {
	const connection = await createJiraConnection(request, generateRandomName());

	// Lighthouse refuses to create a Portfolio while the instance has no Team at all, so there has to
	// be one. It is never refreshed: nothing this test reads comes from below the Epics.
	await createTeam(
		request,
		generateRandomName(),
		connection.id,
		'project = LGHTHSDMO AND labels = "Lagunitas"',
		["Story"],
		jiraStates,
	);

	const portfolio = await createPortfolio(
		request,
		generateRandomName(),
		connection.id,
		THE_FOUR_TAGGED_EPICS,
		["Epic"],
		jiraStates,
	);

	const lighthousePage = overviewPage.lighthousePage;
	const portfolioPage = await (
		await lighthousePage.goToOverview()
	).goToPortfolio(portfolio.name);
	await portfolioPage.refreshFeatures();
	await waitForBackgroundUpdates(request);

	const deliveries = await portfolioPage.goToDeliveries();
	const dialog = await deliveries.addDelivery();

	await expect(dialog.sourceTab("Jira Release")).toBeVisible();
	const jiraReleases = await dialog.switchToSource("Jira Release");
	await expect(jiraReleases.picker).toBeVisible({
		timeout: RELEASES_ARRIVE_FROM_JIRA,
	});

	await jiraReleases.openList();

	// A Release nobody dated is still worth showing - the reader can go and date it in Jira and come
	// straight back - but there is no date to take from it yet, so it cannot be picked.
	await expect(jiraReleases.option(ONE_RELEASE_WITHOUT_A_DATE)).toBeVisible();
	expect(await jiraReleases.isSelectable(ONE_RELEASE_WITHOUT_A_DATE)).toBe(
		false,
	);
	expect(
		await jiraReleases.isSelectable(THE_OTHER_RELEASE_WITHOUT_A_DATE),
	).toBe(false);

	// The name and the date belong to the Release while this tab is showing, so neither is the
	// reader's to type over.
	await expect(dialog.deliveryNameInput).toBeDisabled();
	await expect(dialog.deliveryDateInput).toBeDisabled();

	await jiraReleases.pick(THE_DATED_RELEASE);

	// Picking fills the form from the Release itself. Reading the date back out of the field, rather
	// than naming it here, is what keeps the check a fact about Jira and not about the day this was
	// written.
	await expect(dialog.deliveryNameInput).toHaveValue(THE_DATED_RELEASE);
	const dateJiraHolds = await dialog.deliveryDateInput.inputValue();
	expect(dateJiraHolds).not.toBe("");

	await expect(jiraReleases.previewSummary).toHaveText(
		`${THE_DATED_RELEASE} would set the date to ${asShownToTheReader(dateJiraHolds)}`,
	);

	await expect(jiraReleases.previewGrid).toBeVisible();
	for (const epic of TAGGED_EPICS) {
		await expect(jiraReleases.previewed(epic)).toBeVisible();
	}

	// Five items in Jira carry this Release; four of them are here. The fifth is a Story, and
	// membership is read at the level the Portfolio tracks rather than rolled up from children - so
	// work this Portfolio does not track as a Feature is simply not part of the Release.
	expect(await jiraReleases.previewedCount()).toBe(TAGGED_EPICS.length);

	// Up to here nothing has left the form. Saving is where the Release has to survive the trip to the
	// server and back: the Release's own name, the same day Jira holds, the same four Features, and
	// the marker naming what the row now follows.
	const savedDeliveries = await dialog.save();
	const delivery = savedDeliveries.getDeliveryByName(THE_DATED_RELEASE);
	await expect(delivery.container).toBeVisible({
		timeout: THE_SAVED_DELIVERY_COMES_BACK,
	});

	expect(await delivery.getName()).toBe(THE_DATED_RELEASE);
	expect(await delivery.getDeliveryDate()).toBe(
		asShownToTheReader(dateJiraHolds),
	);
	expect(await delivery.getScope()).toBe(TAGGED_EPICS.length);

	await expect(delivery.boundIndicator("Jira Release")).toBeVisible();
});
