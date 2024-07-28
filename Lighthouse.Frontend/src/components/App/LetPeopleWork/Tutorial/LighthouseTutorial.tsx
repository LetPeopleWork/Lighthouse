import React, { useState } from "react";
import { Box, Stepper, Step, StepLabel, Typography, Button, Paper, Container } from "@mui/material";
import InputGroup from "../../../Common/InputGroup/InputGroup";

interface LighthouseTutorialProps {
    tutorialTitle: string;
    steps: React.FC[];
}

const LighthouseTutorial: React.FC<LighthouseTutorialProps> = ({ tutorialTitle, steps }) => {
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

    const StepContent = steps[activeStep];

    return (
        <InputGroup title={tutorialTitle}>            
            <Paper>
                <Stepper activeStep={activeStep}>
                    {steps.map((StepComponent, index) => (
                        <Step key={index}>
                            <StepLabel>{StepComponent.displayName || `Step ${index + 1}`}</StepLabel>
                        </Step>
                    ))}
                </Stepper>
                <Box p={3}>
                    {activeStep === steps.length ? (
                        <Box>
                            <Typography variant="h5" gutterBottom>
                                Tutorial Complete
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
                                    onClick={handleNext}
                                >
                                    {activeStep === steps.length - 1 ? 'Finish' : 'Next'}
                                </Button>
                            </Box>
                        </Box>
                    )}
                </Box>
            </Paper>
        </InputGroup>
    );
};

export default LighthouseTutorial;