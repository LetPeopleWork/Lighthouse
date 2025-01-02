import { Container, Typography } from "@mui/material";
import type React from "react";
import { Link } from "react-router-dom";
import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";

import LogsOverview from "../../../../../assets/Tutorial/Settings/Logs.png";
import RefreshSettings from "../../../../../assets/Tutorial/Settings/RefreshSettings.png";
import WorkTrackingSystemsVideo from "../../../../../assets/Tutorial/Settings/WorkTrackingSystems.gif";

import InputGroup from "../../../../Common/InputGroup/InputGroup";

const WorkTrackingSystemsStep: React.FC = () => (
	<TutorialStep
		title="Work Tracking Systems"
		description="How to connect to the System that hosts your Work Items?"
		imageSrc={WorkTrackingSystemsVideo}
	>
		<Container maxWidth={false}>
			<Typography
				variant="body1"
				sx={{ whiteSpace: "pre-line", marginBottom: 2 }}
			>
				{`In order for Lighthouse to get the data it needs for forecasting, it needs to connect to your Work Tracking System.
Work Tracking Systems are stored in the Lighthouse Settings and can be reused across Teams and Projects.

You can create, modify, and remove Work Tracking Systems via the settings.

Each connection has a specific name and a type. Depending on the type, different configuration options have to be specified.`}
			</Typography>

			<InputGroup initiallyExpanded={false} title="Jira">
				<Typography variant="body1">
					{
						"In order to connect, you need also to have the URL of your Jira instance. This looks something like this:"
					}
				</Typography>
				<Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
					{"https://letpeoplework.atlassian.net"}
				</Typography>
				<Typography variant="body1">
					{
						"where letpeoplework is your instance name. On top of that, you need to create an API Token for a dedicated user and supply both the username as well as the access token. You can find more information on how to create an Access Token here:"
					}
					<Link
						to="https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/"
						target="_blank"
						rel="noopener"
					>
						Manage API tokens for your Atlassian account
					</Link>
				</Typography>
			</InputGroup>

			<InputGroup initiallyExpanded={false} title="Azure DevOps">
				<Typography variant="body1">
					{`In order to connect, you need to have the URL of your Azure DevOps organization.
If you work in the cloud, this looks something like this:`}
				</Typography>
				<Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
					{"https://dev.azure.com/letpeoplework"}
				</Typography>
				<Typography variant="body1">
					{`where letpeoplework would be your organization name. You don't need to specify any Team Project, this should be part of the query.
On top of that, you need to specify a `}
					<Link
						to="https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows"
						target="_blank"
						rel="noopener"
					>
						Personal Access Token
					</Link>
					{" with read permissions for the Work Items scope."}
				</Typography>
			</InputGroup>

			<Typography variant="body1" sx={{ marginTop: 2 }}>
				{
					"Before you can save any Work Tracking System, the connection has to be validated. Only if the connection could be established with the specified input, you can save it."
				}
			</Typography>
		</Container>
	</TutorialStep>
);

const DefaultTeamSettingsSteps: React.FC = () => (
	<TutorialStep
		title="Default Team Settings"
		description="Simplify Team Creation by Adjusting the Default Values"
	>
		<Container maxWidth={false}>
			<Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
				{`While teams might organize differently to some degree, chances are that some parts are done the same across your organization.
                If you want to add multiple teams, it can be a time-saver if you set up the defaults for your teams here.
                
                You can for example give a default query for the work items, or adjust the Work Item Types used etc.
                
                Once the settings are saved, you will see these defaults next time you will create a team.`}
			</Typography>
		</Container>
	</TutorialStep>
);

const DefaultProjectSettingsStep: React.FC = () => (
	<TutorialStep
		title="Default Project Settings"
		description="Simplify Project Creation by Adjusting the Default Values"
	>
		<Container maxWidth={false}>
			<Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
				{`Similar to the teams, you can also set up default values for new Project creation.
                
                You can for example give a default query for the work items, or adjust the Work Item Types used etc.
                
                Once the settings are saved, you will see these defaults next time you will create a Project.`}
			</Typography>
		</Container>
	</TutorialStep>
);

const PeriodicRefreshSettingsStep: React.FC = () => (
	<TutorialStep
		title="Periodic Refresh Settings"
		description="Configure how frequently Lighthouse is refreshing the data"
		imageSrc={RefreshSettings}
	>
		<Container maxWidth={false}>
			<Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
				{`A key benefit of Lighthouse is the idea of continuous forecasting. In the Refresh Settings, you can specify how often Lighthouse should check for updated data on your Work Tracking System.
                
                You can configure three different refresh cycles:
                - How often the Throughput for each Team should be refreshed?
                - How often the Features of a Project should be refreshed?
                - How often the Forecast for a Project should be refreshed?

                For each of those three categories, you can adjust three individual values:
                - Interval: How often should be checked if an update should happen?
                - Refresh After: After how much time since the last update a new update should be triggered?
                - Start Delay: When should the first check happen after Ligthhouse was started up?
                
                Feel free to adjust the values as needed. Keep in mind that, while we want continuous forecasts, Lighthouse is not a real time tool and you always have the option to manually refresh Throughputs, Features, and Forecasts as needed.
                `}
			</Typography>
		</Container>
	</TutorialStep>
);

const LogsStep: React.FC = () => (
	<TutorialStep
		title="Logs"
		description="See what's going on in Detail"
		imageSrc={LogsOverview}
	>
		<Container maxWidth={false}>
			<Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
				{`While we're trying everything to make sure Lighthouse is running as flawless as possible, it's realistic that occasionally it doesn't

                For this, you can inspect the logs via the built-in log viewer. You can adjust the log level to get more or less details, refresh or download the logs, and directly inspect the logs within Lighthouse.

                This is a great help if you happened to find something not working as expected and would like to report something to us. If you can provide a log file together with an issue report, it will simplify our work to investigate what happened.
                `}
			</Typography>
		</Container>
	</TutorialStep>
);

const steps = [
	{ title: "Work Tracking Systems", component: WorkTrackingSystemsStep },
	{ title: "Default Team Settings", component: DefaultTeamSettingsSteps },
	{ title: "Default Project Settings", component: DefaultProjectSettingsStep },
	{
		title: "Periodic Refresh Settings",
		component: PeriodicRefreshSettingsStep,
	},
	{ title: "Logs", component: LogsStep },
];

const SettingsTutorial: React.FC = () => {
	return (
		<LighthouseTutorial
			steps={steps}
			tutorialTitle="Lighthouse Settings"
			finalButtonText="Start Over"
		/>
	);
};

export default SettingsTutorial;
