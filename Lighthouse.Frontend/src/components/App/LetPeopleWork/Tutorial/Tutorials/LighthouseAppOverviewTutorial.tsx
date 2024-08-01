import React from "react";
import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";
import { Container, Typography } from "@mui/material";
import { Link, useNavigate } from "react-router-dom";

import OverviewVideo from '../../../../../assets/Tutorial/Overview/LighthouseOverview.gif'
import TeamsOverview from '../../../../../assets/Tutorial/Overview/TeamsOverview.png'
import ProjectOverview from '../../../../../assets/Tutorial/Overview/ProjectOverview.png'

const LighthouseOverviewStep: React.FC = () => (
    <TutorialStep
        title="Welcome"
        description="Lighthouse is a tool that helps you run probabilistic forecasts using Monte Carlo Simulations in a continuous and simple way"
        imageSrc={OverviewVideo}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`It connects to your Work Tracking Tool (currently Jira and Azure DevOps are supported) and will automatically update your team's Throughput and your project's forecasted delivery dates.\n
                You can use it with a single team for doing manual "When" and "How Many" forecasts, as well as for tracking projects with one or multiple teams.\n
                The forecasts are created using `}
                <Link to={"https://medium.com/@benjihuser/an-introduction-and-step-by-step-guide-to-monte-carlo-simulations-4706f675a02f?source=friends_link&sk=5e329d6d9725bbcbf03aad2e970cfae7"}>Monte Carlo Simulations</Link>

                {`.

                Lighthouse is provided free of charge as open-source software by `}
                <Link to={"https://letpeople.work"}>LetPeopleWork</Link>

                {`. If you want assistance with the tool, what we can offer you and your company, or just want to chat, please reach out.\n
                If you just want to get started using Lighthouse, click Next.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const TeamsOverviewStep: React.FC = () => (
    <TutorialStep
        title="Teams"
        description="Define your Teams to run 'How Many' and 'When' Forecasts"
        imageSrc={TeamsOverview}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`A team has a Throughput (how many items was this team finishing over a certain period of time). This Throughput is used to run forecasts.
                After you created a team, you can run individual "How Many" and "When" Forecasts for this team.

                You can create as many teams as you like.
                `}
            </Typography>
        </Container>
    </TutorialStep>
);

const ProjectOverviewStep: React.FC = () => (
    <TutorialStep
        title="Projects"
        description="Create a Project to keep an overview over when a certain set of Features will be done"
        imageSrc={ProjectOverview}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`A Project has Features (for example 'Epics') where one or multiple Teams contribute towards.
                Lighthouse will forecast when those Features are expected to be done, based on the remaining work for each of the involved Teams.

                If you define Milestones, Lighthouse will tell you how likely it is that you manage to close each Feature until this date.
                `}
            </Typography>
        </Container>
    </TutorialStep>
);

const steps = [
    { title: 'Lighthouse Application Overview', component: LighthouseOverviewStep },
    { title: 'Teams', component: TeamsOverviewStep },
    { title: 'Projects', component: ProjectOverviewStep },
];

const LighthouseAppOverviewTutorial: React.FC = () => {

    const navigate = useNavigate();

    const createNewTeam = () => {
        navigate('/teams/new');
    }

    return (
        <LighthouseTutorial steps={steps} tutorialTitle="About Lighthouse" finalButtonText="Create a new Team" onFinalButtonClick={createNewTeam} />
    );
}

export default LighthouseAppOverviewTutorial;