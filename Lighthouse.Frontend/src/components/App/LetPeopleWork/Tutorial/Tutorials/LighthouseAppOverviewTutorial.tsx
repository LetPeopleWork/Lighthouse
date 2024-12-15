import React from "react";
import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";
import { Container, Typography } from "@mui/material";
import { Link, useNavigate } from "react-router-dom";

import OverviewVideo from '../../../../../assets/Tutorial/Overview/LighthouseOverview.gif';
import TeamsOverview from '../../../../../assets/Tutorial/Overview/TeamsOverview.png';
import ProjectOverview from '../../../../../assets/Tutorial/Overview/ProjectOverview.png';
import SettingsOverview from '../../../../../assets/Tutorial/Overview/Settings.gif';

const LighthouseOverviewStep: React.FC = () => (
    <TutorialStep
        title="Overview"
        description="Lighthouse is a tool that helps you run probabilistic forecasts using Monte Carlo Simulations in a continuous and simple way."
        imageSrc={OverviewVideo}
    >
        <Container maxWidth={false}>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`It connects to your work tracking tool (currently Jira and Azure DevOps are supported) and will automatically update your team's throughput and your project's forecasted delivery dates.\n
                You can use it with a single team for manual "When" and "How Many" forecasts, as well as for tracking projects with one or multiple teams.\n
                The forecasts are created using `}
                <Link to={"https://www.letpeople.work/post/an-introduction-and-step-by-step-guide-to-monte-carlo-simulations"}>Monte Carlo Simulations</Link>

                {`. Lighthouse is provided free of charge as open-source software by `}
                <Link to={"https://letpeople.work"}>Let People Work</Link>

                {`. If you need assistance with the tool, want to know more about what we can offer you and your company, or just want to chat, please reach out.\n
                If you're ready to start using Lighthouse, click Next.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const TeamsOverviewStep: React.FC = () => (
    <TutorialStep
        title="Teams"
        description="Define your teams to run 'How Many' and 'When' forecasts."
        imageSrc={TeamsOverview}
    >
        <Container maxWidth={false}>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`A team has a throughput (the number of items the team finishes over a certain period). This throughput is used to run forecasts.
                After you create a team, you can run individual "How Many" and "When" forecasts for this team.\n
                You can create as many teams as you like.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const ProjectOverviewStep: React.FC = () => (
    <TutorialStep
        title="Projects"
        description="Create a project to keep track of when a certain set of features will be done."
        imageSrc={ProjectOverview}
    >
        <Container maxWidth={false}>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`A project consists of features (for example, 'Epics') that one or multiple teams contribute towards.
                Lighthouse will forecast when these features are expected to be completed, based on the remaining work for each of the involved teams.\n
                If you define milestones, Lighthouse will tell you how likely it is to complete each feature by the specified date.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const SettingsOverviewStep: React.FC = () => (
    <TutorialStep
        title="Settings"
        description="Fine-tune Lighthouse to match your needs."
        imageSrc={SettingsOverview}
    >
        <Container maxWidth={false}>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`You can adjust various settings, from the default values for creating new teams and projects, to the connections to your work tracking systems like Jira and Azure DevOps, and the frequency at which Lighthouse updates the data for your teams, projects, and forecasts.\n
                If something doesn't work, you can also find additional logs. These logs can be useful for identifying and solving specific issues.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const steps = [
    { title: 'Overview', component: LighthouseOverviewStep },
    { title: 'Teams', component: TeamsOverviewStep },
    { title: 'Projects', component: ProjectOverviewStep },
    { title: 'Settings', component: SettingsOverviewStep },
];

const LighthouseAppOverviewTutorial: React.FC = () => {
    const navigate = useNavigate();

    const createNewTeam = () => {
        navigate('/teams/new');
    };

    return (
        <LighthouseTutorial 
            steps={steps} 
            tutorialTitle="About Lighthouse" 
            finalButtonText="Create a new Team" 
            onFinalButtonClick={createNewTeam} 
        />
    );
};

export default LighthouseAppOverviewTutorial;
