import Box from "@mui/material/Box";
import CssBaseline from "@mui/material/CssBaseline";
import { useTheme } from "@mui/material/styles";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type React from "react";
import { useEffect, useState } from "react";
import {
	Navigate,
	Route,
	BrowserRouter as Router,
	Routes,
	useParams,
} from "react-router-dom";
import Footer from "./components/App/Footer/Footer";
import Header from "./components/App/Header/Header";
import OverviewDashboard from "./pages/Overview/OverviewDashboard";
import Settings from "./pages/Settings/Settings";
import "./App.css";
import DemoDialog from "./components/App/Demo/DemoDialog";
import PortfolioDetail from "./pages/Portfolios/Detail/PortfolioDetail";
import EditPortfolio from "./pages/Portfolios/Edit/EditPortfolio";
import TeamDetail from "./pages/Teams/Detail/TeamDetail";
import EditTeam from "./pages/Teams/Edit/EditTeam";
import {
	ApiServiceContext,
	getApiServices,
	type IApiServiceContext,
} from "./services/Api/ApiServiceContext";
import { TerminologyProvider } from "./services/TerminologyContext";

// Create a QueryClient instance with optimized defaults
const queryClient = new QueryClient({
	defaultOptions: {
		queries: {
			staleTime: 1000 * 60 * 5, // 5 minutes default stale time
			gcTime: 1000 * 60 * 30, // 30 minutes garbage collection time
			retry: 2,
			refetchOnWindowFocus: false,
			refetchOnMount: true,
			refetchOnReconnect: true,
		},
	},
});

const TeamEditRedirect: React.FC = () => {
	const { id } = useParams<{ id: string }>();
	return <Navigate to={`/teams/${id}/settings`} replace />;
};

const PortfolioEditRedirect: React.FC = () => {
	const { id } = useParams<{ id: string }>();
	return <Navigate to={`/portfolios/${id}/settings`} replace />;
};

const ProjectEditRedirect: React.FC = () => {
	const { id } = useParams<{ id: string }>();
	return <Navigate to={`/portfolios/${id}/settings`} replace />;
};

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
		<QueryClientProvider client={queryClient}>
			<Router>
				<ApiServiceContext.Provider value={apiServices}>
					<TerminologyProvider>
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
										<Route path=":id/:tab?" element={<TeamDetail />} />
										<Route path="edit/:id" element={<TeamEditRedirect />} />
										<Route path="new" element={<EditTeam />} />
									</Route>
									<Route path="/portfolios">
										<Route path=":id/:tab?" element={<PortfolioDetail />} />
										<Route
											path="edit/:id"
											element={<PortfolioEditRedirect />}
										/>
										<Route path="new" element={<EditPortfolio />} />
									</Route>

									{/* Backward compatibility for old links */}
									<Route path="/projects">
										<Route path=":id/:tab?" element={<PortfolioDetail />} />
										<Route path="edit/:id" element={<ProjectEditRedirect />} />
										<Route path="new" element={<EditPortfolio />} />
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
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</Router>
		</QueryClientProvider>
	);
};

export default App;
