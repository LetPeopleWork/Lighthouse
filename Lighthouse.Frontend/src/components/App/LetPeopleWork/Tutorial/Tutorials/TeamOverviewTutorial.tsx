import React from "react";
import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";
import { Container, Typography } from "@mui/material";
import { Link, useNavigate } from "react-router-dom";

import TeamForecast from '../../../../../assets/Tutorial/Team/TeamForecast.png'
import TeamOverview from '../../../../../assets/Tutorial/Team/TeamOverview.png'
import DeleteTeam from '../../../../../assets/Tutorial/Team/DeleteTeam.gif'
import TeamDetail from '../../../../../assets/Tutorial/Team/TeamDetail.gif'

const TeamsOverview: React.FC = () => (
    <TutorialStep
        title="About Teams"
        description="Teams are an essential building block for using Lighthouse. You must define at least one team to run any kind of forecasts."
        imageSrc={TeamForecast}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`If you want to start using Lighthouse, you have to create a Team. With every team, you'll get this Teams Throughput, which you can then use for your Forecasts.
                
                Read on to see how you can work with Teams within Lighthouse or directly `}
                <Link to={'/teams/new'}>Create a new Team</Link>
            </Typography>
        </Container>
    </TutorialStep>
);

const TeamsList: React.FC = () => (
    <TutorialStep
        title="Teams Overview"
        description="See all your configured teams at one glance"
        imageSrc={TeamOverview}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Once you have at least one team defined, they will show up in a grid view when you navigate to                                 `}
                <Link to={"/teams"}>Teams</Link>
                {`.
                
                Via this overview, you can add new teams, delete or modify existing ones, or jump into the Team details.

                You can also search for specific teams and, if you've defined any Project, see the number of work items as well as Features the specific Team is currently planned to work on.
                `}
            </Typography>
        </Container>
    </TutorialStep>
);

const DeleteTeamStep: React.FC = () => (
    <TutorialStep
        title="Delete Team"
        description="Deleting a Team can be done via the Delete Button in the Overview"
        imageSrc={DeleteTeam}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Please be aware that you can't undo the deletion of a Team. If you later on need the same Team again, you will have to re-add it. On delete, all information about a Team is removed from Ligthhouse.
                `}
            </Typography>
        </Container>
    </TutorialStep>
);

const TeamDetailsStep: React.FC = () => (
    <TutorialStep
        title="Team Details"
        description="Dive into the Details of a specific Team"
        imageSrc={TeamDetail}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`If you want to see more details for a Team, you can either click on the Team Name or on the Info Icon in the overview.

                This will bring you to the specific Team page. In there, you can see which Features a Team is working on (if any Projects are defined), the Teams Throughput as well as run manual Forecasts for this specific team.

                Note: You can directly link to a Team Detail page by storing the specific url.
                `}
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
    }

    return (
        <LighthouseTutorial steps={steps} tutorialTitle="Teams" finalButtonText="Create a new Team" onFinalButtonClick={createNewTeam} />
    );
}

export default TeamOverviewTutorial;