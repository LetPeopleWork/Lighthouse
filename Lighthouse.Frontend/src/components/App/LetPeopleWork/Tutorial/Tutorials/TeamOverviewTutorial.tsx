import React from "react";
import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";
import { Container, Typography } from "@mui/material";
import { Link, useNavigate } from "react-router-dom";

import TeamForecast from '../../../../../assets/Tutorial/Team/TeamForecast.png';
import TeamOverview from '../../../../../assets/Tutorial/Team/TeamOverview.png';
import DeleteTeam from '../../../../../assets/Tutorial/Team/DeleteTeam.gif';
import TeamDetail from '../../../../../assets/Tutorial/Team/TeamDetail.gif';

const TeamsOverview: React.FC = () => (
    <TutorialStep
        title="About Teams"
        description="Teams are an essential building block for using Lighthouse. You must define at least one team to run any kind of forecasts."
        imageSrc={TeamForecast}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`To start using Lighthouse, you need to create a team. Each team has a throughput, which you can use for your forecasts.
                
                Continue reading to learn how to work with teams in Lighthouse, or directly `}
                <Link to={'/teams/new'}>create a new team</Link>
                {`.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const TeamsList: React.FC = () => (
    <TutorialStep
        title="Teams Overview"
        description="See all your configured teams at a glance."
        imageSrc={TeamOverview}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Once you have at least one team defined, it will appear in a grid view on the `}
                <Link to={"/teams"}>Teams</Link>
                {` page.
                
                From this overview, you can add new teams, delete or modify existing ones, or view team details.
                
                You can also search for specific teams and, if you have defined any projects, see the number of work items and features each team is currently planned to work on.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const DeleteTeamStep: React.FC = () => (
    <TutorialStep
        title="Delete Team"
        description="Delete a team via the Delete button in the overview."
        imageSrc={DeleteTeam}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Please be aware that deleting a team is permanent and cannot be undone. If you need the same team later, you will have to re-add it. Deleting a team removes all information about that team from Lighthouse.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const TeamDetailsStep: React.FC = () => (
    <TutorialStep
        title="Team Details"
        description="Dive into the details of a specific team."
        imageSrc={TeamDetail}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`To see more details for a team, you can click on the team name or the info icon in the overview.

                This will bring you to the specific team page, where you can see which features the team is working on (if any projects are defined), the team's throughput, and run manual forecasts for this specific team.

                Note: You can directly link to a team detail page by storing the specific URL.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const steps = [
    { title: 'About Teams', component: TeamsOverview },
    { title: 'Teams Overview', component: TeamsList },
    { title: 'Delete Team', component: DeleteTeamStep },
    { title: 'Team Details', component: TeamDetailsStep },
];

const TeamOverviewTutorial: React.FC = () => {
    const navigate = useNavigate();

    const createNewTeam = () => {
        navigate('/teams/new');
    };

    return (
        <LighthouseTutorial 
            steps={steps} 
            tutorialTitle="Teams" 
            finalButtonText="Create a new Team" 
            onFinalButtonClick={createNewTeam} 
        />
    );
};

export default TeamOverviewTutorial;
