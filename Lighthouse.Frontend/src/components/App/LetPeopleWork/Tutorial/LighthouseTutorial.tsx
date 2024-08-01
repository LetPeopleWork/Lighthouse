import React, { useState } from "react";
import { Box, Stepper, Step, StepLabel, Typography, Button, Paper, Container } from "@mui/material";

interface StepData {
    title: string;
    component: React.FC;
}

interface LighthouseTutorialProps {
    tutorialTitle: string;
    steps: StepData[];
    finalButtonText?: string;
    onFinalButtonClick?: () => void;
}

const LighthouseTutorial: React.FC<LighthouseTutorialProps> = ({
    tutorialTitle,
    steps,
    finalButtonText = "Finish",
    onFinalButtonClick
}) => {
    const [activeStep, setActiveStep] = useState(0);

    const handleNext = () => {
        setActiveStep((prevActiveStep) => prevActiveStep + 1);
    };

    const handleBack = () => {
        setActiveStep((prevActiveStep) => prevActiveStep - 1);
    };

    const handleReset = () => {
        setActiveStep(0);
    };

    const handleFinalAction = () => {
        if (onFinalButtonClick) {
            onFinalButtonClick();
        } else {
            handleReset();
        }
    };

    const StepContent = steps[activeStep].component;

    return (
        <Container>
            <Typography variant="h4" align="center">{tutorialTitle}</Typography>
            <Paper>
                <Stepper activeStep={activeStep}>
                    {steps.map((step, index) => (
                        <Step key={index}>
                            <StepLabel>{step.title}</StepLabel>
                        </Step>
                    ))}
                </Stepper>
                <Box p={3}>
                    {activeStep === steps.length ? (
                        <Box>
                            <Typography variant="h5" gutterBottom>
                                {`${tutorialTitle} Complete`}
                            </Typography>
                            <Typography variant="body1">
                                You've completed the tutorial. You can now use the application with the knowledge you've gained.
                            </Typography>
                            <Button onClick={handleReset} sx={{ mt: 2 }}>
                                Reset
                            </Button>
                        </Box>
                    ) : (
                        <Box>
                            <StepContent />
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 2 }}>
                                <Button disabled={activeStep === 0} onClick={handleBack}>
                                    Back
                                </Button>
                                <Button
                                    variant="contained"
                                    color="primary"
                                    onClick={activeStep === steps.length - 1 ? handleFinalAction : handleNext}
                                >
                                    {activeStep === steps.length - 1 ? finalButtonText : 'Next'}
                                </Button>
                            </Box>
                        </Box>
                    )}
                </Box>
            </Paper>
        </Container>
    );
};

export default LighthouseTutorial;
