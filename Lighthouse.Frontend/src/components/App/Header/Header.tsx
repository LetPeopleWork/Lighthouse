import { BugReport } from "@mui/icons-material";
import HandshakeIcon from "@mui/icons-material/Handshake";
import HelpIcon from "@mui/icons-material/Help";
import { AppBar, Box, Toolbar } from "@mui/material";
import type React from "react";
import LighthouseLogo from "../LetPeopleWork/LighthouseLogo";
import ExternalLinkButton from "./ExternalLinkButton";
import NavigationItem from "./NavigationItem";

const Header: React.FC = () => {
	return (
		<AppBar
			position="static"
			className="header"
			style={{ backgroundColor: "white" }}
		>
			<Toolbar className="toolbar">
				<Box className="logo">
					<LighthouseLogo />
				</Box>
				<Box className="nav-links">
					<NavigationItem path="/" text="Overview" />
					<NavigationItem path="/teams" text="Teams" />
					<NavigationItem path="/projects" text="Projects" />
					<NavigationItem path="/settings" text="Settings" />
				</Box>
				<Box sx={{ display: { xs: "none", md: "flex" } }}>
					<ExternalLinkButton
						link="https://docs.lighthouse.letpeople.work/contributions/contributions.html"
						icon={HandshakeIcon}
						tooltip="Contributors"
					/>
					<ExternalLinkButton
						link="https://github.com/LetPeopleWork/Lighthouse/issues"
						icon={BugReport}
						tooltip="Report an Issue"
					/>
					<ExternalLinkButton
						link="https://docs.lighthouse.letpeople.work"
						icon={HelpIcon}
						tooltip="Documentation"
					/>
				</Box>
			</Toolbar>
		</AppBar>
	);
};

export default Header;
