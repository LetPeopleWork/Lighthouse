import React from "react";
import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";
import { Container, Typography, Link } from "@mui/material";
import GeneralConfigurationImage from '../../../../../assets/Tutorial/Team/GeneralConfiguration.png';
import WorkItemTypesImage from '../../../../../assets/Tutorial/Team/WorkItemTypes.gif';
import AdvancedConfigurationImage from '../../../../../assets/Tutorial/Team/AdvancedConfiguration.png';
import { WorkTrackingSystems } from "./SharedSteps";
import InputGroup from "../../../../Common/InputGroup/InputGroup";

const GeneralConfiguration: React.FC = () => (
    <TutorialStep
        title="General Configuration"
        description="Mandatory Configuration Options for Your Teams"
        imageSrc={GeneralConfigurationImage}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line', marginBottom: 2 }}>
                {`Enter the name of the team you want to create as well as the Throughput History. This is the number of days of the past you want to include when running forecasts for this team.
In general this should be more than 10 days, and represent a period where this team was somewhat working in a stable fashion. Common values are 30 to 90 days.

The Work Item Query is the query that is executed against your Work Tracking System to get the teams backlog.
The query should fetch all items that "belong" to this team and the specific syntax depends on the Work Tracking System you are using`}
            </Typography>

            <InputGroup initiallyExpanded={false} title="Jira Example">
                <Typography variant="body1">
                    {`Queries for Jira are written in `}
                    <Link href="https://www.atlassian.com/blog/jira-software/jql-the-most-flexible-way-to-search-jira-14" target="_blank" rel="noopener">
                        Jira Query Language (JQL)
                    </Link>
                    {`. An example Query for a Team called "Lagunitas", where all issues for this Team are labeled with their team name, could look like this:`}
                </Typography>
                <Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
                    {`project = "LGHTHSDMO" AND labels = "Lagunitas"`}
                </Typography>
                <Typography variant="body1">
                    {`You can use any kind of filtering you'd like and that is valid according to the JQL specification. An extended query that would exclude certain states would look like this:`}
                </Typography>                
                <Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
                    {`project = "LGHTHSDMO" AND labels = "Lagunitas" AND status NOT IN (Canceled)`}
                </Typography>
            </InputGroup>

            <InputGroup initiallyExpanded={false} title="Azure DevOps Example">
                <Typography variant="body1">
                    {`Queries for Azure DevOps are written in the `}
                    <Link href="https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops" target="_blank" rel="noopener">
                        Work Item Query Language (WIQL)
                    </Link>
                    {`. An example Query for a Team called "Binary Blazers" in the Team Project "Lighthouse Demo" could look like this:`}
                </Typography>
                <Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
                    {`[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers"`}
                </Typography>
                <Typography variant="body1">
                    {`You can use any kind of filtering you'd like and that is valid according to the WIQL language. An extended query that would exclude certain items based on their tags would look like this:`}
                </Typography>
                <Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
                    {`[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers" AND [System.Tags] NOT CONTAINS "Automation"`}
                </Typography>
            </InputGroup>
        </Container>
    </TutorialStep>
);

const WorkItemTypes: React.FC = () => (
    <TutorialStep
        title="Work Item Types"
        description="Which types of Work Items are in your Teams Backlog'"
        imageSrc={WorkItemTypesImage}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line', marginBottom: 2 }}>
                {`In order to properly forecast, Lighthouse needs to know which items your team works on that are relevant for the forecast.
Thus you can define the item types that should be taken into account for this specific team.

You can remove types by hitting the remove icon, and add new ones by typing them in.

Note that you have to type the exact type name as it's used in your Work Tracking System.

Common examples for item types on team level are "User Story", "Bug", and "Product Backlog Item" for Azure DevOps, and "Story" as well as "Bug" for Jira.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const AdvancedConfiguration: React.FC = () => (
    <TutorialStep
        title="Advanced Configuration"
        description="Advanced Stuff to fine-tune your teams"
        imageSrc={AdvancedConfigurationImage}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line', marginBottom: 2 }}>
                {`If your team is working on multiple Features at the same time, you want to adjust the Feature WIP to this number.
This will impact your forecasts for projects, and will lead to different predicted delivery times.

In order to establish a relation between a Feature and a team, Lighthouse assumes that the Feature is set as a parent for the work item.
If this is not the case, you can specify an additional field that is containing the ID of the Feature in the Teams Work Items. That way, you can let Lighthouse know how the relation between Feature and Work Items are established.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const steps = [
    { title: 'General Configuration', component: GeneralConfiguration },
    { title: 'Work Item Types', component: WorkItemTypes },
    { title: 'Work Tracking Systems', component: WorkTrackingSystems },
    { title: 'Advanced Configuration', component: AdvancedConfiguration },
];

const TeamConfigurationTutorial: React.FC = () => (
    <LighthouseTutorial
        steps={steps}
        tutorialTitle="Team Configuration"
        finalButtonText="Start Over"
    />
);

export default TeamConfigurationTutorial;