import ArticleIcon from "@mui/icons-material/Article";
import FolderIcon from "@mui/icons-material/Folder";
import GroupWorkIcon from "@mui/icons-material/GroupWork";
import SettingsIcon from "@mui/icons-material/Settings";
import SettingsSystemDaydreamIcon from "@mui/icons-material/SettingsSystemDaydream";
import TabContext from "@mui/lab/TabContext";
import TabList from "@mui/lab/TabList";
import TabPanel from "@mui/lab/TabPanel";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Paper from "@mui/material/Paper";
import { useTheme } from "@mui/material/styles";
import Tab from "@mui/material/Tab";
import Typography from "@mui/material/Typography";
import useMediaQuery from "@mui/material/useMediaQuery";
import type React from "react";
import { useEffect, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";
import WorkTrackingSystemConnectionSettings from "./Connections/WorkTrackingSystemConnectionSettings";
import DefaultProjectSettings from "./DefaultProjectSettings/DefaultProjectSettings";
import DefaultTeamSettings from "./DefaultTeamSettings/DefaultTeamSettings";
import LogSettings from "./LogSettings/LogSettings";
import SystemSettingsTab from "./System/SystemSettingsTab";

const Settings: React.FC = () => {
	const [value, setValue] = useState("10");
	const [mounted, setMounted] = useState(false);
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("md"));

	const { getTerm } = useTerminology();
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const workTrackingSystemConnectionSettingsTerm = getTerm(
		TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS,
	);

	useEffect(() => {
		setMounted(true);
	}, []);

	const handleChange = (_event: React.SyntheticEvent, newValue: string) => {
		setValue(newValue);
	};

	const tabConfig = [
		{
			value: "10",
			label: workTrackingSystemConnectionSettingsTerm,
			testId: "work-tracking-tab",
			panelTestId: "work-tracking-panel",
			icon: <SettingsIcon />,
			component: <WorkTrackingSystemConnectionSettings />,
		},
		{
			value: "20",
			label: "System Settings",
			testId: "system-settings-tab",
			panelTestId: "system-settings-panel",
			icon: <SettingsSystemDaydreamIcon />,
			component: <SystemSettingsTab />,
		},
		{
			value: "30",
			label: `Default ${teamsTerm}`,
			testId: "default-team-settings-tab",
			panelTestId: "default-team-settings-panel",
			icon: <GroupWorkIcon />,
			component: <DefaultTeamSettings />,
		},
		{
			value: "40",
			label: "Default Projects",
			testId: "default-project-settings-tab",
			panelTestId: "default-project-settings-panel",
			icon: <FolderIcon />,
			component: <DefaultProjectSettings />,
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
								bgcolor: theme.palette.action.hover,
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
