import React from "react";
import { Container, Typography } from "@mui/material";

import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";

import ProjectDetails from '../../../../../assets/Tutorial/Project/ProjectDetails.png';
import ProjectForecast from '../../../../../assets/Tutorial/Project/ProjectForecast.png';
import MilestonesDynamicUpdate from '../../../../../assets/Tutorial/Project/MilestonesDynamicUpdate.gif';
import FeatureWIpDynamicUpdate from '../../../../../assets/Tutorial/Project/FeatureWIPDynamicUpdate.gif';

const ProjectDetailOverview: React.FC = () => (
    <TutorialStep
        title="About Project Details"
        description="Details of a Projects including Forecasts"
        imageSrc={ProjectDetails}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Once you have created your Project, you can see all the details on this page.
                
Browse through all the features this project includes and see details like which Teams are involved, how many items are left, and what is the forecasted completion date.
                
Click Next to learn more.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const Features: React.FC = () => (
    <TutorialStep
        title="Features"
        description="Which features are part of this Project?"
        imageSrc={ProjectForecast}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`The feature table gives you an overview of all features that are part of the project.
For each feature, you'll see the name, how much work is remaining on this feature in total, and split by individual teams.

Additionally, you'll see a set of forecasts for when this feature will be completed: from rather certain (95% chance of completion by this date) to more risky (50% chance of completion).
If you have defined Milestones, you'll also see the likelihood you'll manage to deliver this feature before you hit the Milestone.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const Milestones: React.FC = () => (
    <TutorialStep
        title="Milestones"
        description="Which Milestones do we need to hit for this project?"
        imageSrc={MilestonesDynamicUpdate}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Each project can have many milestones. You can dynamically add and remove them, or modify existing ones.
                Maybe that customer visit was moved one week ahead? Let's adjust the milestone and see what we manage to deliver till then.
                
                As soon as you adjust the milestones, the forecasts will automatically update and show the new probabilities for hitting any milestone.
                This also allows you to play around with dates and see what might be a good date to communicate towards your stakeholders.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const InvolvedTeamsFeatureWIP: React.FC = () => (
    <TutorialStep
        title="Involved Teams (Feature WIP)"
        description="On How Many Features In Parallel are our Teams working on?"
        imageSrc={FeatureWIpDynamicUpdate}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Each team works on one or multiple features in parallel. If a team focuses on a few Features, they will get delivered more early, while the next Features will get started later.
                On the other hand, if we have a high Feature WIP, Features that might not have a high priority get started earlier and delievered before the most important features.
                
                You can set the Feature WIP for every involved team and see what happens with the forecast.
                Note that Lighthouse does not infer the Feature WIP from your Work Tracking System, it relies on the setting that is set up here and will use this for any forecast.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const steps = [
    { title: 'About Project Details', component: ProjectDetailOverview },
    { title: 'Features', component: Features },
    { title: 'Milestones', component: Milestones },
    { title: 'Involved Teams (FeatureWIP)', component: InvolvedTeamsFeatureWIP },
];

const ProjectDetailTutorial: React.FC = () => (
    <LighthouseTutorial 
        steps={steps} 
        tutorialTitle="Project Details" 
        finalButtonText="Start Over" 
    />
);

export default ProjectDetailTutorial;
