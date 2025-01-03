import HelpOutlineIcon from "@mui/icons-material/HelpOutline";
import { Box, IconButton, Tooltip } from "@mui/material";
import type React from "react";
import { useState } from "react";

interface TutorialButtonProps {
	tutorialComponent: React.ReactNode;
}

const TutorialButton: React.FC<TutorialButtonProps> = ({
	tutorialComponent,
}) => {
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
					style={{ color: "rgba(48, 87, 78, 1)" }}
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
				>
					<Box
						position="absolute"
						top={0}
						left={0}
						right={0}
						bottom={0}
						bgcolor="rgba(0, 0, 0, 0.5)"
						onClick={handleCloseTutorial}
					/>

					<Box
						bgcolor="white"
						p={3}
						borderRadius={2}
						maxWidth="90%"
						maxHeight="90%"
						overflow="auto"
						zIndex={1100}
						onClick={(e) => e.stopPropagation()}
					>
						{tutorialComponent}
					</Box>
				</Box>
			)}
		</>
	);
};

export default TutorialButton;
