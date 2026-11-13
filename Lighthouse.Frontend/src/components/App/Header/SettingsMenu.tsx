import ArrowDropDownIcon from "@mui/icons-material/ArrowDropDown";
import Button from "@mui/material/Button";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import { useTheme } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useState } from "react";
import { useLocation, useNavigate } from "react-router";
import { useRbac } from "../../../hooks/useRbac";
import {
	isSettingsPath,
	type SettingsAccess,
	settingsAreaPath,
	visibleAreasOf,
} from "../../../models/Settings/settingsAreas";
import { reportFailedNavigation } from "../../../utils/navigation";
import NavigationItem from "./NavigationItem";

/** The areas the header offers. Until access is known nobody is treated as a System Admin, so Organization stays out. */
const useOfferedAreas = () => {
	const rbac = useRbac();
	const access: SettingsAccess = {
		isLoading: rbac.isLoading,
		isSystemAdmin: rbac.isSystemAdmin && !rbac.isLoading,
		isRbacEnabled: rbac.isRbacEnabled,
	};
	return visibleAreasOf(access);
};

/** The header entry on a wide screen: a Settings button opening a menu of the areas. */
export const SettingsMenu: React.FC = () => {
	const theme = useTheme();
	const navigate = useNavigate();
	const { pathname } = useLocation();
	const areas = useOfferedAreas();
	const [anchor, setAnchor] = useState<HTMLElement | null>(null);
	const isActive = isSettingsPath(pathname);

	const open = (path: string) => {
		setAnchor(null);
		reportFailedNavigation(navigate(path));
	};

	return (
		<>
			<Button
				data-testid="settings-menu"
				aria-haspopup="menu"
				aria-expanded={anchor !== null}
				onClick={(event) => setAnchor(event.currentTarget)}
				endIcon={<ArrowDropDownIcon />}
				disableRipple
				sx={{
					color: theme.palette.text.primary,
					textTransform: "none",
					fontSize: "inherit",
					fontWeight: isActive ? "bold" : "normal",
					borderRadius: 0,
					padding: "4px 8px",
					borderBottom: isActive
						? `2px solid ${theme.palette.primary.main}`
						: "2px solid transparent",
				}}
			>
				Settings
			</Button>
			<Menu
				anchorEl={anchor}
				open={anchor !== null}
				onClose={() => setAnchor(null)}
			>
				{areas.map((area) => (
					<MenuItem
						key={area.slug}
						data-testid={area.menuTestId}
						onClick={() => open(settingsAreaPath(area.slug))}
					>
						<ListItemText primary={area.label} secondary={area.description} />
					</MenuItem>
				))}
			</Menu>
		</>
	);
};

/** The same entry in the mobile drawer: a caption that leads nowhere, with the areas indented below it. */
export const SettingsDrawerGroup: React.FC<{ onNavigate: () => void }> = ({
	onNavigate,
}) => {
	const areas = useOfferedAreas();

	return (
		<>
			<Typography
				sx={{ px: 2, pt: 1, fontWeight: "bold", color: "text.primary" }}
			>
				Settings
			</Typography>
			{areas.map((area) => (
				<Typography
					component="div"
					key={area.slug}
					sx={{ pl: 4, py: 0.5 }}
					onClick={onNavigate}
				>
					<NavigationItem
						path={settingsAreaPath(area.slug)}
						text={area.label}
					/>
				</Typography>
			))}
		</>
	);
};
