import { Container, Link, Typography } from "@mui/material";
import type React from "react";

import InputGroup from "../../../../Common/InputGroup/InputGroup";
import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";

import DefaultFeatureSizeImage from "../../../../../assets/Tutorial/Project/DefaultFeatureSize.png";
import GeneralConfigurationImage from "../../../../../assets/Tutorial/Project/GeneralConfiguration.png";
import MilestonesImage from "../../../../../assets/Tutorial/Project/Milestones.gif";
import UnparentedWorkItemsImage from "../../../../../assets/Tutorial/Project/UnparentedWorkItems.png";
import WorkItemTypesImage from "../../../../../assets/Tutorial/Project/WorkItemTypes.gif";

import { States, WorkTrackingSystems } from "./SharedSteps";

const GeneralConfiguration: React.FC = () => (
	<TutorialStep
		title="General Configuration"
		description="Mandatory Configuration Options for Your Projects"
		imageSrc={GeneralConfigurationImage}
	>
		<Container maxWidth={false}>
			<Typography
				variant="body1"
				sx={{ whiteSpace: "pre-line", marginBottom: 2 }}
			>
				{`Enter the name of the project you want to create as well as the Work Item Query.

The Work Item Query is the query that is executed against your Work Tracking System to get the Features for this Project.
A Feature is a "parent" item (for example an "Epic") which has the more detailed work items that the teams work on as child items.
The query should fetch all items that "belong" to this project and the specific syntax depends on the Work Tracking System you are using.`}
			</Typography>

			<InputGroup initiallyExpanded={false} title="Jira Example">
				<Typography variant="body1">
					{"Queries for Jira are written in "}
					<Link
						href="https://www.atlassian.com/blog/jira-software/jql-the-most-flexible-way-to-search-jira-14"
						target="_blank"
						rel="noopener"
					>
						Jira Query Language (JQL)
					</Link>
					{`. An example Query for a project called "Dart Release", where all issues for this project belong to a release in Jira could look like this:`}
				</Typography>
				<Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
					{`project = "LGHTHSDMO" AND fixVersion = "Dart Release"`}
				</Typography>
				<Typography variant="body1">
					{`You can use any kind of filtering you'd like and that is valid according to the JQL specification. An extended query that would exclude certain states would look like this:`}
				</Typography>
				<Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
					{`project = "LGHTHSDMO" AND fixVersion = "Dart Release" AND status NOT IN (Canceled)`}
				</Typography>
			</InputGroup>

			<InputGroup initiallyExpanded={false} title="Azure DevOps Example">
				<Typography variant="body1">
					{"Queries for Azure DevOps are written in the "}
					<Link
						href="https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops"
						target="_blank"
						rel="noopener"
					>
						Work Item Query Language (WIQL)
					</Link>
					{`. An example Query for a Project called "Release 1.33.7" in the Team Project "Lighthouse Demo" where all items contain a tag with the project name could look like this:`}
				</Typography>
				<Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
					{`[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Release 1.33.7"`}
				</Typography>
				<Typography variant="body1">
					{`You can use any kind of filtering you'd like and that is valid according to the WIQL language. An extended query that could include items based on the area path and exclude certain items based on their tags would look like this:`}
				</Typography>
				<Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
					{`[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Release 1.33.7" AND [System.Tags] NOT CONTAINS "Technical Debt"`}
				</Typography>
			</InputGroup>

			<Typography
				variant="body1"
				sx={{ whiteSpace: "pre-line", marginBottom: 2 }}
			>
				{`Note that you don't have to filter for the Work Item Types, as you can define this in one of the next steps.`}
			</Typography>
		</Container>
	</TutorialStep>
);

const WorkItemTypes: React.FC = () => (
	<TutorialStep
		title="Work Item Types"
		description="Which types of Work Items are in your Project?"
		imageSrc={WorkItemTypesImage}
	>
		<Container maxWidth={false}>
			<Typography
				variant="body1"
				sx={{ whiteSpace: "pre-line", marginBottom: 2 }}
			>
				{`In order to properly collect your Features for the Project, Lighthouse needs to know which item types you are using.
We want to get the items that are the "direct parent" of the work items that your teams are working on.

For example: If you use a hierarchy that is Epic --> Feature --> User Story, and your teams focus on User Stories, you want to set the type to Feature.

You can define any type of item. Note that you have to type the exact type name as it's used in your Work Tracking System.
You can remove types by hitting the remove icon, and add new ones by typing them in.`}
			</Typography>
		</Container>
	</TutorialStep>
);

const Milestones: React.FC = () => (
	<TutorialStep
		title="Milestones"
		description="What are important target dates?"
		imageSrc={MilestonesImage}
	>
		<Container maxWidth={false}>
			<Typography
				variant="body1"
				sx={{ whiteSpace: "pre-line", marginBottom: 2 }}
			>
				{`While sometimes we just want to know "When will our Features be done", in other scenarios we have special events happening at a certain date.
You can track such events (like Sprint Reviews, an important customer visit, that conference where you wanted to share the new version of your app, etc.) as Milestones.

Milestones have a name and a date, and they will be displayed in the Project Details with the likelihood of completing each Feature.`}
			</Typography>
		</Container>
	</TutorialStep>
);

const UnparentedWorkItems: React.FC = () => (
	<TutorialStep
		title="Unparented Work Items"
		description="Deal with Items that are not related to any Feature"
		imageSrc={UnparentedWorkItemsImage}
	>
		<Container maxWidth={false}>
			<Typography
				variant="body1"
				sx={{ whiteSpace: "pre-line", marginBottom: 2 }}
			>
				{`Sometimes you might have a "bunch of items" that are not connected to any Feature, but you still consider them part of the Project.
An example for this might be a collection of bugs you wanted to deliver with a new version. That you don't have to create "dummy features" just for grouping those bugs, you can use an "Unparented Work Items Query".
This query will go through all "Team Backlogs" and see if any item is matching the query that is not part of a feature of this project. All items that are found will be grouped in a "virtual Feature" called "Unparented - Project Name".`}
			</Typography>
		</Container>
	</TutorialStep>
);
const FeatureSize: React.FC = () => (
	<TutorialStep
		title="Default Feature Size"
		description="Fine-tune your Forecast, even if you don't all the details yet"
		imageSrc={DefaultFeatureSizeImage}
	>
		<Container maxWidth={false}>
			<Typography
				variant="body1"
				sx={{ whiteSpace: "pre-line", marginBottom: 2 }}
			>
				{`As we don't want to waste time breaking down Features into details too early, you might have the situation where you want to plan for a Feature, but it does not have any child items yet, as it has not been refined in more detail.
For those scenarios, you can specify a "Default Number of Items per Feature", use your historical feature size, and a "Size Estimation Field" that will be used in order to forecast.

The default number of items is a fixed number that is used if no child items and no estimation is found. If you opt for the historical feature size, you must specify a query so Lighthouse knows which features to get. For example you can filter for all features, or just features are in a specific state, or features that were closed within the last 180 days.
From all features that match your query, the number of child items is extracted, and based on the percentile value you specify, the default size is calculated. This works dynamically every time the features or a projects are refreshed. As an example, if you set the percentile to 80, it means the following: "80% of all Features had x items or less", and x will be used as default.

If you specify an estimation field, Lighthouse will check the value from that field and assume it's the expected number of items for this feature. You might arrive at this number by doing some comparison to previous Features.
In case the field is not specified, or there is no value defined, Lighthouse will fall back to the value defined as default. That way you can get a more realistic forecast without too much effort.

Furthermore, you can specify which states of Features should ignore the real number of items and still fall-back to our default size. This might be useful if you just started refining, and have some items added, but not all yet, and you want to rely on the default still.
If no overriding state is defined, it means that as soon as you have a single child item for a Feature, the default won't be used anymore. It's only applied if the number of child items is 0.`}
			</Typography>
		</Container>
	</TutorialStep>
);

const steps = [
	{ title: "General Configuration", component: GeneralConfiguration },
	{ title: "Work Item Types", component: WorkItemTypes },
	{ title: "Work Tracking Systems", component: WorkTrackingSystems },
	{ title: "States", component: States },
	{ title: "Milestones", component: Milestones },
	{ title: "Unparented Work Items", component: UnparentedWorkItems },
	{ title: "Default Feature Size", component: FeatureSize },
];

const ProjectConfigurationTutorial: React.FC = () => (
	<LighthouseTutorial
		steps={steps}
		tutorialTitle="Project Configuration"
		finalButtonText="Start Over"
	/>
);

export default ProjectConfigurationTutorial;
