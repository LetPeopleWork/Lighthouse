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
import { useNavigate } from "react-router";
import { useRbac } from "../../hooks/useRbac";
import {
	areaBySlug,
	type SettingsAreaSlug,
	type SettingsTabSlug,
	settingsTabPath,
	visibleTabsOf,
} from "../../models/Settings/settingsAreas";
import { reportFailedNavigation } from "../../utils/navigation";
import { settingsPanels } from "./settingsPanels";

interface SettingsAreaPageProps {
	area: SettingsAreaSlug;
	tab: SettingsTabSlug;
}

/** One settings area with the tabs the person may open; the address, not local state, says which tab is open. */
const SettingsAreaPage: React.FC<SettingsAreaPageProps> = ({ area, tab }) => {
	const [mounted, setMounted] = useState(false);
	const rbac = useRbac();
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("md"));
	const navigate = useNavigate();

	useEffect(() => {
		setMounted(true);
	}, []);

	const handleChange = (_event: React.SyntheticEvent, newTab: string) => {
		reportFailedNavigation(
			navigate(settingsTabPath(area, newTab as SettingsTabSlug), {
				replace: true,
			}),
		);
	};

	const visibleTabs = visibleTabsOf(area, rbac);

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
					Settings › {areaBySlug(area)?.label}
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
					<TabContext value={tab}>
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
								{visibleTabs.map((each) => (
									<Tab
										key={each.slug}
										label={each.label}
										icon={settingsPanels[each.slug].icon}
										value={each.slug}
										data-testid={each.testId}
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

						{visibleTabs.map((each) => (
							<TabPanel
								key={each.slug}
								value={each.slug}
								data-testid={each.panelTestId}
								sx={{
									p: { xs: 2, md: 3 },
									height: "100%",
									transition: "opacity 0.3s ease",
									opacity: tab === each.slug ? 1 : 0,
								}}
							>
								{settingsPanels[each.slug].panel}
							</TabPanel>
						))}
					</TabContext>
				</Paper>
			</Box>
		</Container>
	);
};

export default SettingsAreaPage;
