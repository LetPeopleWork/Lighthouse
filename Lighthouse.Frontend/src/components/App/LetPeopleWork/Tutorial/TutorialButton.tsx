import React, { useState } from 'react';
import { IconButton, Box, Tooltip } from '@mui/material';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';

interface TutorialButtonProps {
    tutorialComponent: React.ReactNode;
}

const TutorialButton: React.FC<TutorialButtonProps> = ({ tutorialComponent }) => {
    const [showTutorial, setShowTutorial] = useState(false);

    const handleHelpClick = () => {
        setShowTutorial(true);
    };

    const handleCloseTutorial = () => {
        setShowTutorial(false);
    };

    return (
        <>
            <Tooltip title="Help">
                <IconButton
                    onClick={handleHelpClick}
                >
                    <HelpOutlineIcon />
                </IconButton>
            </Tooltip>

            {showTutorial && (
                <Box
                    position="fixed"
                    top={0}
                    left={0}
                    right={0}
                    bottom={0}
                    display="flex"
                    justifyContent="center"
                    alignItems="center"
                    zIndex={1000}
                    onClick={handleCloseTutorial}
                >
                    <Box
                        bgcolor="white"
                        p={3}
                        borderRadius={2}
                        maxWidth="90%"
                        maxHeight="90%"
                        overflow="auto"
                        onClick={(e) => e.stopPropagation()} // Prevent clicks inside the tutorial from closing it
                    >
                        {tutorialComponent}
                    </Box>
                </Box>
            )}
        </>
    );
};

export default TutorialButton;
