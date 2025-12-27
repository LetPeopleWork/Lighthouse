import FeedbackIcon from "@mui/icons-material/Feedback";
import HandshakeIcon from "@mui/icons-material/Handshake";
import HelpIcon from "@mui/icons-material/Help";
import MenuIcon from "@mui/icons-material/Menu";
import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Drawer from "@mui/material/Drawer";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import { useTheme } from "@mui/material/styles";
import Toolbar from "@mui/material/Toolbar";
import Tooltip from "@mui/material/Tooltip";
import useMediaQuery from "@mui/material/useMediaQuery";
import type React from "react";
import { useState } from "react";
import LicenseStatusIcon from "../../Common/LicenseStatus/LicenseStatusIcon";
import ThemeToggler from "../../Common/ThemeToggler/ThemeToggler";
import LighthouseLogo from "../LetPeopleWork/LighthouseLogo";
import ExternalLinkButton from "./ExternalLinkButton";
import FeedbackDialog from "./FeedbackDialog";
import NavigationItem from "./NavigationItem";
import UpdateAllButton from "./UpdateAllButton";

const Header: React.FC = () => {
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("md"));
	const [mobileOpen, setMobileOpen] = useState(false);
	const [feedbackDialogOpen, setFeedbackDialogOpen] = useState(false);

	const handleDrawerToggle = () => {
		setMobileOpen(!mobileOpen);
	};

	const handleFeedbackClick = () => {
		setFeedbackDialogOpen(true);
	};

	const handleFeedbackClose = () => {
		setFeedbackDialogOpen(false);
	};

	const navigationLinks = [
		{ path: "/", text: "Overview" },
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
			<ListItem>
				<LicenseStatusIcon />
			</ListItem>
			<ListItem>
				<UpdateAllButton />
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
							<LicenseStatusIcon />
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
							<UpdateAllButton />
							<LicenseStatusIcon />
							<ThemeToggler />
							<ExternalLinkButton
								link="https://docs.lighthouse.letpeople.work/contributions/contributions.html"
								icon={HandshakeIcon}
								tooltip="Contributors"
							/>
							<Tooltip title="Provide Feedback" arrow>
								<IconButton
									size="large"
									color="inherit"
									onClick={handleFeedbackClick}
									aria-label="Provide Feedback"
									data-testid="feedback-button"
								>
									<FeedbackIcon style={{ color: theme.palette.primary.main }} />
								</IconButton>
							</Tooltip>
							<ExternalLinkButton
								link="https://docs.lighthouse.letpeople.work"
								icon={HelpIcon}
								tooltip="Documentation"
							/>
						</Box>
					</>
				)}
			</Toolbar>
			<FeedbackDialog open={feedbackDialogOpen} onClose={handleFeedbackClose} />
		</AppBar>
	);
};

export default Header;
