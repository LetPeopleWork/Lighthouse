import { useTheme } from "@mui/material";
import type React from "react";
import { NavLink } from "react-router-dom";

interface NavigationItemProps {
	path: string;
	text: string;
}

const NavigationItem: React.FC<NavigationItemProps> = ({ path, text }) => {
	const theme = useTheme();

	return (
		<NavLink
			to={path}
			style={({ isActive }) => ({
				color:
					theme.palette.mode === "dark"
						? "#ffffff"
						: theme.palette.primary.main,
				fontWeight: isActive ? "bold" : "normal",
				textDecoration: "none",
				position: "relative",
				padding: "4px 8px",
				transition: "all 0.2s ease",
				borderBottom: isActive
					? `2px solid ${theme.palette.mode === "dark" ? "#ffffff" : theme.palette.primary.main}`
					: "2px solid transparent",
			})}
			// Using NavLink's className to add a custom class we can target with CSS
			className={({ isActive }) =>
				`nav-item ${isActive ? "nav-active" : ""} ${theme.palette.mode === "dark" ? "nav-dark" : ""}`
			}
		>
			{text}
		</NavLink>
	);
};

export default NavigationItem;
