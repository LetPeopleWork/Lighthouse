import FormatListBulletedIcon from "@mui/icons-material/FormatListBulleted";
import { Button } from "@mui/material";
import type React from "react";

interface ProgressTitleProps {
	title: string;
	isUsingDefaultFeatureSize: boolean;
	onShowDetails: () => Promise<void>;
}

const ProgressTitle: React.FC<ProgressTitleProps> = ({
	title,
	isUsingDefaultFeatureSize,
	onShowDetails,
}) => {
	if (isUsingDefaultFeatureSize) {
		return title;
	}

	return (
		<Button
			variant="text"
			size="small"
			sx={{
				p: 0,
				minWidth: 0,
				textTransform: "none",
				textDecoration: "none",
				"&:hover": { textDecoration: "underline" },
			}}
			onClick={async (event) => {
				event.stopPropagation();
				await onShowDetails();
			}}
		>
			{title}
			<FormatListBulletedIcon fontSize="inherit" sx={{ ml: 0.5 }} />
		</Button>
	);
};

export default ProgressTitle;
