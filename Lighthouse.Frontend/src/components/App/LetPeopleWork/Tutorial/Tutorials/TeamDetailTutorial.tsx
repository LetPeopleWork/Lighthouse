import React from "react";
import LighthouseTutorial from "../LighthouseTutorial";
import TutorialStep from "../TutorialStep";
import { Container, Typography } from "@mui/material";

import TeamDetailsVideo from '../../../../../assets/Tutorial/Team/TeamDetails.gif';
import FeatureImage from '../../../../../assets/Tutorial/Team/Features.png';
import TeamForecastImage from '../../../../../assets/Tutorial/Team/TeamForecast.png';
import ThroughputImage from '../../../../../assets/Tutorial/Team/Throughput.png';

const TeamDetailOverview: React.FC = () => (
    <TutorialStep
        title="About Team Details"
        description="Details of a Team including Forecasts"
        imageSrc={TeamDetailsVideo}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Once you have created your team, you can see all the details on this page.
                
                Browse through all the features from the various projects the team is involved in, and see how many items are left to do for this team in each feature.
                Furthermore, you can inspect the team's throughput (how many items they closed per day) and run manual forecasts.
                
                Click Next to learn more.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const Features: React.FC = () => (
    <TutorialStep
        title="Features"
        description="Which features is this team contributing to?"
        imageSrc={FeatureImage}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`The feature table gives you an overview of all features defined in any project this team is contributing to.
                For each feature, you'll see the name, how much work is remaining on this feature in total and for this specific team (if multiple teams contribute to it).

                Additionally, you'll see a set of forecasts for when this feature will be completed: from rather certain (95% chance of completion by this date) to more risky (50% chance of completion).

                Lastly, you can see which projects each feature is part of and have a direct link to it.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const Forecasts: React.FC = () => (
    <TutorialStep
        title="Forecasts"
        description="Answer 'How Many Items Will Be Done' and 'When Will They Be Done?'"
        imageSrc={TeamForecastImage}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Based on the team's throughput (see next step), you can run Monte Carlo simulations to answer two important questions for many teams and organizations.

                The first question is: When will 'x' items be done? If you have a certain number of items (e.g., remaining for a feature, part of the sprint goal, etc.), you might want to know when you can expect them to be completed.

                The second question is: How many items will you get done by a certain date? You might have an important customer visit coming up, or simply want to know what is a sensible number of items to plan for in your next sprint.

                Both questions will be answered with a forecast, which means you'll get multiple results that vary in certainty, from certain (95%) to risky (50%).
                Additionally, you'll also get the likelihood of closing your specified number of items by the set target date.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const Throughput: React.FC = () => (
    <TutorialStep
        title="Throughput"
        description="What's the historical performance of the team?"
        imageSrc={ThroughputImage}
    >
        <Container>
            <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                {`Throughput is the historical performance of the team, measured in items closed per unit of time, and is the input to any forecast done within Lighthouse.
                
                You can see how many items were closed each day over the last several days (as defined by the team settings).
                The more 'stable' your throughput is, the more accurate your forecast will be.`}
            </Typography>
        </Container>
    </TutorialStep>
);

const steps = [
    { title: 'About Team Details', component: TeamDetailOverview },
    { title: 'Features', component: Features },
    { title: 'Forecasts', component: Forecasts },
    { title: 'Throughput', component: Throughput },
];

const TeamDetailTutorial: React.FC = () => (
    <LighthouseTutorial 
        steps={steps} 
        tutorialTitle="Team Details" 
        finalButtonText="Start Over" 
    />
);

export default TeamDetailTutorial;
