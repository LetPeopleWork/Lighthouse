import React from "react";
import LighthouseTutorial from "./LighthouseTutorial";
import TutorialStep from "./TutorialStep";
import { List, ListItem } from "@mui/material";
import { Link } from "react-router-dom";

import OverviewVideo from '../../../../assets/Tutorial/Overview/Overview.gif'
import TeamsOverview from '../../../../assets/Tutorial/Overview/TeamsOverview.png'

export const LighthouseOverviewStep: React.FC = () => (
    <TutorialStep
        title="Start Page"
        description="Here you see an overview over Lighthouse with a gif"
        imageSrc={OverviewVideo}
    >
        <List>
            <ListItem>
                This is some more explanation
            </ListItem>
            <ListItem>
                And a bit more.
            </ListItem>
        </List>
    </TutorialStep>
);

export const TeamsOverviewStep: React.FC = () => (
    <TutorialStep
        title="Teams"
        description="The teams area shows you all your existing teams."
        imageSrc={TeamsOverview}
    >
        <List>
            <ListItem>
                <Link to={'/teams/new'}>
                    You can create new teams
                </Link>
            </ListItem>
            <ListItem>
                Or edit existing ones
            </ListItem>
        </List>
    </TutorialStep>
);

export const ProjectOverviewStep: React.FC = () => (
    <TutorialStep
        title="Projects"
        description="The projects area shows you all your existing Projects."
    >
        <List>
            <ListItem>
                <Link to={'/projects/new'}>
                    You can create new Projects
                </Link>
            </ListItem>
            <ListItem>
                Also here is no picture, don't ask.
            </ListItem>
        </List>
    </TutorialStep>
);

const steps = [
    LighthouseOverviewStep,
    TeamsOverviewStep,
    ProjectOverviewStep,
];


const LighthouseFullTutorial : React.FC = () => {
    return (
        <LighthouseTutorial steps={steps} tutorialTitle="Lighthouse Overview" />
    );
}

export default LighthouseFullTutorial;