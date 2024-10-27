import { Container, Typography, Link } from "@mui/material";
import TutorialStep from "../TutorialStep";
import InputGroup from "../../../../Common/InputGroup/InputGroup";
import ImageComponent from "../../../../Common/ImageComponent/ImageComponent";

import WorkTrackingSystemsImage from '../../../../../assets/Tutorial/Team/WorkTrackingSystems.gif';
import StatesPicture from '../../../../../assets/Tutorial/States/States.png'
import JiraStates from '../../../../../assets/Tutorial/States/Jira.png'
import AdoStates from '../../../../../assets/Tutorial/States/ADO.png'

export const WorkTrackingSystems: React.FC = () => (
    <TutorialStep
        title="Work Tracking Systems"
        description="How to connect to the System that hosts your Work Items?"
        imageSrc={WorkTrackingSystemsImage}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line', marginBottom: 2 }}>
                {`In order for Lighthouse to get the data it needs for forecasting, it needs to connect to your Work Tracking System.
Work Tracking Systems are stored in the Lighthouse Settings and can be reused across Teams and Projects.

When creating or modifying both Teams or Projects, you can either choose an existing connection or create a new one.

Each connection has a specific name and a type. Depending on the type, different configuration options have to be specified.`}
            </Typography>

            <InputGroup initiallyExpanded={false} title="Jira">
                <Typography variant="body1">
                    {`In order to connect, you need also to have the URL of your Jira instance. This looks something like this:`}
                </Typography>
                <Typography variant="body2" fontStyle="italic" sx={{ marginY: 1 }}>
                    {`https://letpeoplework.atlassian.net`}
                </Typography>
                <Typography variant="body1">
                    {`where letpeoplework is your instance name. On top of that, you need to create an API Token for a dedicated user and supply both the username as well as the access token. You can find more information on how to create an Access Token here:`}
                    <Link href="https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/" target="_blank" rel="noopener">
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
                    {`https://dev.azure.com/letpeoplework`}
                </Typography>
                <Typography variant="body1">
                    {`where letpeoplework would be your organization name. You don't need to specify any Team Project, this should be part of the query.
On top of that, you need to specify a `}
                    <Link href="https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows" target="_blank" rel="noopener">
                        Personal Access Token
                    </Link>
                    {` with read permissions for the Work Items scope.`}
                </Typography>
            </InputGroup>

            <Typography variant="body1" sx={{ marginTop: 2 }}>
                {`Before you can save any Work Tracking System, the connection has to be validated. Only if the connection could be established with the specified input, you can save it.`}
            </Typography>
        </Container>
    </TutorialStep>
);

export const States: React.FC = () => (
    <TutorialStep
        title="States"
        description="How to define what is in progress and done?"
        imageSrc={StatesPicture}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line', marginBottom: 2 }}>
                {`In order for Lighthouse to know which items to consider, it needs to know in which category the items are. Lighthouse categorizes items as "To Do" (no active work has been done yet), "Doing" (active work is ongoing) and "Done" (the work is completed).
As every organization, project, and team is potentially using a different workflow, you must "map" the states you use to one of the 3 categories.

If you don't add a state, it will be ignored. This can be useful for states like "Removed" or "Won't Do".

Please make sure to only define states that exist and that you make sure the spelling and casing is similar to what your work tracking system is using.`}
            </Typography>

            <InputGroup initiallyExpanded={false} title="Jira">
                <Typography variant="body1">
                    {`Check your Jira instance and make sure to map every relevant state for the work item types you've selected.`}
                </Typography>
                <Typography variant="body1" fontWeight="bold">
                    Important: Make sure to not add any states that don't exist in your Jira instance, as otherwise there will be an error on fetching any items!
                </Typography>
                <ImageComponent src={JiraStates} alt="Jira States" />
            </InputGroup>

            <InputGroup initiallyExpanded={false} title="Azure DevOps">
                <Typography variant="body1">
                    {`Check Azure DevOps instance and make sure to map every relevant state for the work item types you've selected.`}
                </Typography>
                <ImageComponent src={AdoStates} alt="Azure DevOps States" />
            </InputGroup>
        </Container>
    </TutorialStep>
);