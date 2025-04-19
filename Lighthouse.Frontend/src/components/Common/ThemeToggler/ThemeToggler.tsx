import DarkMode from "@mui/icons-material/DarkMode";
import LightMode from "@mui/icons-material/LightMode";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import { useTheme as useMuiTheme } from "@mui/material/styles";
import type React from "react";
import { useTheme } from "../../../context/ThemeContext";

const ThemeToggler: React.FC = () => {
	const { mode, toggleTheme } = useTheme();
	const muiTheme = useMuiTheme();

	return (
		<Tooltip title={`Switch to ${mode === "light" ? "dark" : "light"} mode`}>
			<IconButton
				onClick={toggleTheme}
				color="inherit"
				sx={{
					color:
						muiTheme.palette.mode === "dark"
							? "#ffffff"
							: muiTheme.palette.primary.main,
					transition: "all 0.3s ease",
					"&:hover": {
						transform: "rotate(30deg)",
					},
				}}
			>
				{mode === "light" ? <DarkMode /> : <LightMode />}
			</IconButton>
		</Tooltip>
	);
};

export default ThemeToggler;
