import { Button, useTheme } from "@mui/material";
import type React from "react";
import { Link } from "react-router-dom";
import lightLogo from "../../../assets/LetPeopleWorkLogo.png";
import darkLogo from "../../../assets/logo_black.png";

const LetPeopleWorkLogo: React.FC = () => {
	const theme = useTheme();
	// Use theme.assets.logoVariant to determine which logo to use
	const logo = theme.assets?.logoVariant === "dark" ? darkLogo : lightLogo;

	return (
		<Button
			component={Link}
			to="https://letpeople.work"
			className="nav-link"
			target="_blank"
			rel="noopener noreferrer"
		>
			<img src={logo} alt="Let People Work Logo" width={70} />
		</Button>
	);
};

export default LetPeopleWorkLogo;
