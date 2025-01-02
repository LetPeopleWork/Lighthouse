import { Box, Paper, Typography } from "@mui/material";
import type React from "react";
import ImageComponent from "../../../Common/ImageComponent/ImageComponent";

interface TutorialStepProps {
	title: string;
	description: string;
	imageSrc?: string;
	children: React.ReactNode;
}

const TutorialStep: React.FC<TutorialStepProps> = ({
	title,
	description,
	imageSrc,
	children,
}) => {
	return (
		<Paper elevation={3} sx={{ padding: 2, margin: 2 }}>
			<Typography variant="h5" gutterBottom align="center">
				{title}
			</Typography>
			<Typography
				variant="subtitle1"
				fontWeight={"bold"}
				gutterBottom
				align="center"
				sx={{ whiteSpace: "pre-line" }}
			>
				{description}
			</Typography>
			{imageSrc && <ImageComponent src={imageSrc} alt="Tutorial step image" />}
			<Box sx={{ marginTop: 2 }}>{children}</Box>
		</Paper>
	);
};

export default TutorialStep;
