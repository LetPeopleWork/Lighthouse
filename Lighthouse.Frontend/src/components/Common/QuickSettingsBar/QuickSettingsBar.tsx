import { Stack } from "@mui/material";
import type React from "react";

type QuickSettingsBarProps = {
	children?: React.ReactNode;
};

const QuickSettingsBar: React.FC<QuickSettingsBarProps> = ({ children }) => {
	return (
		<Stack
			direction="row"
			spacing={1}
			alignItems="center"
			sx={{
				display: "flex",
				justifyContent: "center",
			}}
		>
			{children}
		</Stack>
	);
};

export default QuickSettingsBar;
