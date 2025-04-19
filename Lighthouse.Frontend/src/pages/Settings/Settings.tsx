import ArticleIcon from "@mui/icons-material/Article";
import BiotechIcon from "@mui/icons-material/Biotech";
import FolderIcon from "@mui/icons-material/Folder";
import GroupWorkIcon from "@mui/icons-material/GroupWork";
import RefreshIcon from "@mui/icons-material/Refresh";
import SettingsIcon from "@mui/icons-material/Settings";
import StorageIcon from "@mui/icons-material/Storage";
import TabContext from "@mui/lab/TabContext";
import TabList from "@mui/lab/TabList";
import TabPanel from "@mui/lab/TabPanel";
import {
	Box,
	Container,
	Paper,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import Tab from "@mui/material/Tab";
import type React from "react";
import { useEffect, useState } from "react";
import WorkTrackingSystemConnectionSettings from "./Connections/WorkTrackingSystemConnectionSettings";
import DataRetentionSettingsTab from "./DataRetention/DataRetentionSettingsTab";
import DefaultProjectSettings from "./DefaultProjectSettings/DefaultProjectSettings";
import DefaultTeamSettings from "./DefaultTeamSettings/DefaultTeamSettings";
import LogSettings from "./LogSettings/LogSettings";
import PreviewFeaturesTab from "./PreviewFeatures/PreviewFeaturesTab";
import RefreshSettingsTab from "./Refresh/RefreshSettingsTab";

const Settings: React.FC = () => {
	const [value, setValue] = useState("1");
	const [mounted, setMounted] = useState(false);
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("md"));

	useEffect(() => {
		setMounted(true);
	}, []);

	const handleChange = (_event: React.SyntheticEvent, newValue: string) => {
		setValue(newValue);
	};

	const tabConfig = [
		{
			value: "1",
			label: "Work Tracking",
			testId: "work-tracking-tab",
			panelTestId: "work-tracking-panel",
			icon: <SettingsIcon />,
			component: <WorkTrackingSystemConnectionSettings />,
		},
		{
			value: "2",
			label: "Default Teams",
			testId: "default-team-settings-tab",
			panelTestId: "default-team-settings-panel",
			icon: <GroupWorkIcon />,
			component: <DefaultTeamSettings />,
		},
		{
			value: "3",
			label: "Default Projects",
			testId: "default-project-settings-tab",
			panelTestId: "default-project-settings-panel",
			icon: <FolderIcon />,
			component: <DefaultProjectSettings />,
		},
		{
			value: "4",
			label: "Refresh Settings",
			testId: "periodic-refresh-settings-tab",
			panelTestId: "periodic-refresh-settings-panel",
			icon: <RefreshIcon />,
			component: <RefreshSettingsTab />,
		},
		{
			value: "5",
			label: "Data Retention",
			testId: "data-retention-settings-tab",
			panelTestId: "data-retention-settings-panel",
			icon: <StorageIcon />,
			component: <DataRetentionSettingsTab />,
		},
		{
			value: "80",
			label: "Preview Features",
			testId: "preview-features-tab",
			panelTestId: "preview-features-panel",
			icon: <BiotechIcon />,
			component: <PreviewFeaturesTab />,
		},
		{
			value: "99",
			label: "Logs",
			testId: "logs-tab",
			panelTestId: "logs-panel",
			icon: <ArticleIcon />,
			component: <LogSettings />,
		},
	];

	return (
		<Container maxWidth={false}>
			<Box sx={{ mb: 4, mt: 2 }}>
				<Typography
					variant="h4"
					component="h1"
					sx={{
						fontWeight: 600,
						color: theme.palette.primary.main,
						mb: 4,
						textAlign: isMobile ? "center" : "left",
						opacity: 0,
						animation: "fadeIn 0.5s forwards",
						animationDelay: "0.2s",
						"@keyframes fadeIn": {
							"0%": { opacity: 0, transform: "translateY(10px)" },
							"100%": { opacity: 1, transform: "translateY(0)" },
						},
					}}
				>
					Settings
				</Typography>

				<Paper
					elevation={3}
					sx={{
						borderRadius: 2,
						overflow: "hidden",
						transition: "all 0.3s ease",
						opacity: mounted ? 1 : 0,
						transform: mounted ? "translateY(0)" : "translateY(20px)",
					}}
				>
					<TabContext value={value}>
						<Box
							sx={{
								borderBottom: 1,
								borderColor: "divider",
								bgcolor:
									theme.palette.mode === "dark"
										? "rgba(255,255,255,0.05)"
										: "rgba(0,0,0,0.02)",
							}}
						>
							<TabList
								onChange={handleChange}
								variant={isMobile ? "scrollable" : "standard"}
								scrollButtons={isMobile ? "auto" : false}
								allowScrollButtonsMobile
								sx={{
									".MuiTabs-flexContainer": {
										justifyContent: isMobile ? "flex-start" : "space-between",
									},
									".MuiTab-root": {
										minHeight: isMobile ? "48px" : "64px",
										fontSize: isMobile ? "0.75rem" : "0.875rem",
										minWidth: isMobile ? "120px" : "140px",
									},
								}}
							>
								{tabConfig.map((tab) => (
									<Tab
										key={tab.value}
										label={tab.label}
										icon={tab.icon}
										value={tab.value}
										data-testid={tab.testId}
										iconPosition={isMobile ? "start" : "top"}
										sx={{
											transition: "all 0.2s ease",
											"&.Mui-selected": {
												color: theme.palette.primary.main,
												fontWeight: "bold",
											},
										}}
									/>
								))}
							</TabList>
						</Box>

						{tabConfig.map((tab) => (
							<TabPanel
								key={tab.value}
								value={tab.value}
								data-testid={tab.panelTestId}
								sx={{
									p: { xs: 2, md: 3 },
									height: "100%",
									transition: "opacity 0.3s ease",
									opacity: value === tab.value ? 1 : 0,
								}}
							>
								{tab.component}
							</TabPanel>
						))}
					</TabContext>
				</Paper>
			</Box>
		</Container>
	);
};

export default Settings;
