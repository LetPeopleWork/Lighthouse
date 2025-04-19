import { Box, CssBaseline, useTheme } from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import { Route, BrowserRouter as Router, Routes } from "react-router-dom";
import Footer from "./components/App/Footer/Footer";
import Header from "./components/App/Header/Header";
import OverviewDashboard from "./pages/Overview/OverviewDashboard";
import ProjectsOverview from "./pages/Projects/Overview/ProjectsOverview";
import Settings from "./pages/Settings/Settings";
import TeamsOverview from "./pages/Teams/Overview/TeamsOverview";
import "./App.css";
import DemoDialog from "./components/App/Demo/DemoDialog";
import ProjectDetail from "./pages/Projects/Detail/ProjectDetail";
import EditProject from "./pages/Projects/Edit/EditProject";
import TeamDetail from "./pages/Teams/Detail/TeamDetail";
import EditTeam from "./pages/Teams/Edit/EditTeam";
import {
	ApiServiceContext,
	type IApiServiceContext,
	getApiServices,
} from "./services/Api/ApiServiceContext";

const App: React.FC = () => {
	const theme = useTheme();
	const [isDemoDialogOpen, setIsDemoDialogOpen] = useState(false);
	const apiServices: IApiServiceContext = getApiServices();

	useEffect(() => {
		const isDemoMode = import.meta.env.VITE_API_SERVICE_TYPE === "DEMO";
		const demoDialogSeen = localStorage.getItem("demoDialogSeen") === "true";

		if (isDemoMode && !demoDialogSeen) {
			setIsDemoDialogOpen(true);
		}
	}, []);

	const handleCloseDemoDialog = () => {
		setIsDemoDialogOpen(false);
	};

	const handleDontShowAgain = () => {
		localStorage.setItem("demoDialogSeen", "true");
		setIsDemoDialogOpen(false);
	};

	return (
		<Router>
			<ApiServiceContext.Provider value={apiServices}>
				<Box
					className="container"
					sx={{
						bgcolor: theme.palette.background.default,
						color: theme.palette.text.primary,
						transition: "background-color 0.3s ease, color 0.3s ease",
					}}
				>
					<CssBaseline />
					<Header />
					<Box
						component="main"
						className="main-content"
						sx={{
							bgcolor: theme.palette.background.default,
							pt: 2,
							pb: 4,
						}}
					>
						<Routes>
							<Route path="/" element={<OverviewDashboard />} />
							<Route path="/teams">
								<Route index element={<TeamsOverview />} />
								<Route path=":id" element={<TeamDetail />} />
								<Route path="edit/:id" element={<EditTeam />} />
								<Route path="new" element={<EditTeam />} />
							</Route>
							<Route path="/projects">
								<Route index element={<ProjectsOverview />} />
								<Route path=":id" element={<ProjectDetail />} />
								<Route path="edit/:id" element={<EditProject />} />
								<Route path="new" element={<EditProject />} />
							</Route>
							<Route path="/settings" element={<Settings />} />
						</Routes>
					</Box>
					<Footer />
				</Box>
				<DemoDialog
					open={isDemoDialogOpen}
					onClose={handleCloseDemoDialog}
					onDontShowAgain={handleDontShowAgain}
				/>
			</ApiServiceContext.Provider>
		</Router>
	);
};

export default App;
