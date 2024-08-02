import React from "react";
import { Container, Typography } from "@mui/material";

import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";

import ProjectDetails from '../../../../../assets/Tutorial/Project/ProjectDetails.png';
import ProjectForecast from '../../../../../assets/Tutorial/Project/ProjectForecast.png';

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

const steps = [
    { title: 'About Project Details', component: ProjectDetailOverview },
    { title: 'Features', component: Features },
];

const ProjectDetailTutorial: React.FC = () => (
    <LighthouseTutorial 
        steps={steps} 
        tutorialTitle="Project Details" 
        finalButtonText="Start Over" 
    />
);

export default ProjectDetailTutorial;
