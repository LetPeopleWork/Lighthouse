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
import LoginPage from "./components/App/Auth/LoginPage";
import MisconfiguredPage from "./components/App/Auth/MisconfiguredPage";
import SessionExpiredPage from "./components/App/Auth/SessionExpiredPage";
import Footer from "./components/App/Footer/Footer";
import Header from "./components/App/Header/Header";
import SplashScreen from "./components/App/SplashScreen/SplashScreen";
import { useAuthGuard } from "./hooks/useAuthGuard";
import OverviewDashboard from "./pages/Overview/OverviewDashboard";
import Settings from "./pages/Settings/Settings";
import "./App.css";
import EditConnection from "./pages/Connections/Edit/EditConnection";
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
import { hasTauriBackendUrl, isTauriEnv } from "./utils/tauri";

const queryClient = new QueryClient({
	defaultOptions: {
		queries: {
			staleTime: 1000 * 60 * 5,
			gcTime: 1000 * 60 * 30,
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

const initTauriListener = async (
	onReady: () => void,
): Promise<(() => void) | undefined> => {
	try {
		const { listen } = await import("@tauri-apps/api/event");
		return await listen<string>("backend-ready", () => {
			setTimeout(onReady, 200);
		});
	} catch (e) {
		console.error("Failed to initialize Tauri event listener:", e);
		onReady();
		return undefined;
	}
};

const SPLASH_MIN_MS = 5000;

const App: React.FC = () => {
	const theme = useTheme();
	const apiServices: IApiServiceContext = getApiServices();

	// --- 1. Splashscreen State ---
	const isTauri = isTauriEnv() && !hasTauriBackendUrl();
	const [isBackendReady, setIsBackendReady] = useState(false);
	const [minTimeElapsed, setMinTimeElapsed] = useState(!isTauri);

	// Only enforce the minimum display time when showing the splash (Tauri env)
	useEffect(() => {
		if (!isTauri) return;
		const timer = setTimeout(() => setMinTimeElapsed(true), SPLASH_MIN_MS);
		return () => clearTimeout(timer);
	}, [isTauri]);

	useEffect(() => {
		if (!isTauri) {
			setIsBackendReady(true);
			return;
		}

		let unlistenFn: (() => void) | undefined;

		initTauriListener(() => setIsBackendReady(true)).then((unlisten) => {
			unlistenFn = unlisten;
		});

		return () => unlistenFn?.();
	}, [isTauri]);

	// --- 2. Auth Guard ---
	const { shell, loginUrl, misconfigurationMessage, logout } = useAuthGuard(
		apiServices.authService,
	);

	// --- 3. Splashscreen UI ---
	// Show until BOTH the backend is ready AND the minimum display time has passed
	if (!isBackendReady || !minTimeElapsed) {
		return <SplashScreen />;
	}

	// --- 4. Auth Shell Selection ---
	// Ensure protected content does not render before auth state is known
	if (shell === "loading") {
		return <SplashScreen />;
	}

	if (shell === "login") {
		return <LoginPage loginUrl={loginUrl} />;
	}

	if (shell === "misconfigured") {
		return <MisconfiguredPage message={misconfigurationMessage} />;
	}

	if (shell === "session-expired") {
		return <SessionExpiredPage loginUrl={loginUrl} />;
	}

	// shell is "anonymous" or "authenticated" — render the normal app
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
								minHeight: "100vh", // Ensure container fills screen
								display: "flex",
								flexDirection: "column",
							}}
						>
							<CssBaseline />
							<Header
								isAuthenticated={shell === "authenticated"}
								onLogout={logout}
							/>
							<Box
								component="main"
								className="main-content"
								sx={{
									bgcolor: theme.palette.background.default,
									pt: 2,
									pb: 4,
									flex: 1, // Push footer to bottom
								}}
							>
								<Routes>
									<Route path="/" element={<OverviewDashboard />} />
									<Route path="/connections">
										<Route path="new" element={<EditConnection />} />
										<Route path=":id/edit" element={<EditConnection />} />
									</Route>
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
									<Route path="/settings" element={<Settings />} />
								</Routes>
							</Box>
							<Footer />
						</Box>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</Router>
		</QueryClientProvider>
	);
};

export default App;
