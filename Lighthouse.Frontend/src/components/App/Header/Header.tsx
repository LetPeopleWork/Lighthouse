import BugReport from "@mui/icons-material/BugReport";
import HandshakeIcon from "@mui/icons-material/Handshake";
import HelpIcon from "@mui/icons-material/Help";
import MenuIcon from "@mui/icons-material/Menu";
import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Drawer from "@mui/material/Drawer";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import Toolbar from "@mui/material/Toolbar";
import useMediaQuery from "@mui/material/useMediaQuery";
import { useTheme } from "@mui/material/styles";
import type React from "react";
import { useState } from "react";
import ThemeToggler from "../../Common/ThemeToggler/ThemeToggler";
import LighthouseLogo from "../LetPeopleWork/LighthouseLogo";
import ExternalLinkButton from "./ExternalLinkButton";
import NavigationItem from "./NavigationItem";

const Header: React.FC = () => {
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("md"));
	const [mobileOpen, setMobileOpen] = useState(false);

	const handleDrawerToggle = () => {
		setMobileOpen(!mobileOpen);
	};

	const navigationLinks = [
		{ path: "/", text: "Overview" },
		{ path: "/teams", text: "Teams" },
		{ path: "/projects", text: "Projects" },
		{ path: "/settings", text: "Settings" },
	];

	const drawer = (
		<List>
			{navigationLinks.map((link) => (
				<ListItem key={link.text} onClick={() => setMobileOpen(false)}>
					<NavigationItem path={link.path} text={link.text} />
				</ListItem>
			))}
			<ListItem>
				<ThemeToggler />
			</ListItem>
		</List>
	);

	return (
		<AppBar
			position="static"
			className="header"
			style={{ backgroundColor: theme.palette.background.paper }}
			elevation={1}
		>
			<Toolbar className="toolbar">
				<Box className="logo">
					<LighthouseLogo />
				</Box>

				{isMobile ? (
					<>
						<Box display="flex" alignItems="center">
							<ThemeToggler />
							<IconButton
								color="inherit"
								aria-label="open drawer"
								edge="end"
								onClick={handleDrawerToggle}
								sx={{ color: theme.palette.text.primary }}
							>
								<MenuIcon />
							</IconButton>
						</Box>
						<Drawer
							anchor="right"
							open={mobileOpen}
							onClose={handleDrawerToggle}
							ModalProps={{
								keepMounted: true, // Better open performance on mobile
							}}
						>
							{drawer}
						</Drawer>
					</>
				) : (
					<>
						<Box className="nav-links">
							{navigationLinks.map((link) => (
								<NavigationItem
									key={link.text}
									path={link.path}
									text={link.text}
								/>
							))}
						</Box>
						<Box sx={{ display: "flex", alignItems: "center" }}>
							<ThemeToggler />
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
					</>
				)}
			</Toolbar>
		</AppBar>
	);
};

export default Header;
