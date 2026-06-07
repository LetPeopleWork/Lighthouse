import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import CssBaseline from "@mui/material/CssBaseline";
import { useTheme } from "@mui/material/styles";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type React from "react";
import { lazy, Suspense, useEffect, useState } from "react";
import {
	Navigate,
	Route,
	BrowserRouter as Router,
	Routes,
	useParams,
} from "react-router-dom";
import BlockedPage from "./components/App/Auth/BlockedPage";
import LoginPage from "./components/App/Auth/LoginPage";
import MisconfiguredPage from "./components/App/Auth/MisconfiguredPage";
import SessionExpiredPage from "./components/App/Auth/SessionExpiredPage";
import Footer from "./components/App/Footer/Footer";
import Header from "./components/App/Header/Header";
import SplashScreen from "./components/App/SplashScreen/SplashScreen";
import { useAuthGuard } from "./hooks/useAuthGuard";
import "./App.css";
import SurveyNudge from "./components/SurveyNudge/SurveyNudge";
import {
	ApiServiceContext,
	getApiServices,
	type IApiServiceContext,
} from "./services/Api/ApiServiceContext";
import { TerminologyProvider } from "./services/TerminologyContext";
import { notifyBackendReady } from "./utils/backendUrl";
import { hasTauriBackendUrl, isTauriEnv } from "./utils/tauri";

const OverviewDashboard = lazy(
	() => import("./pages/Overview/OverviewDashboard"),
);
const Settings = lazy(() => import("./pages/Settings/Settings"));
const OAuthPopupComplete = lazy(
	() => import("./components/Common/Connections/OAuthPopupComplete"),
);
const EditConnection = lazy(
	() => import("./pages/Connections/Edit/EditConnection"),
);
const PortfolioDetail = lazy(
	() => import("./pages/Portfolios/Detail/PortfolioDetail"),
);
const EditPortfolio = lazy(
	() => import("./pages/Portfolios/Edit/EditPortfolio"),
);
const TeamDetail = lazy(() => import("./pages/Teams/Detail/TeamDetail"));
const EditTeam = lazy(() => import("./pages/Teams/Edit/EditTeam"));

const RouteFallback: React.FC = () => (
	<Box
		sx={{
			display: "flex",
			justifyContent: "center",
			alignItems: "center",
			py: 8,
		}}
	>
		<CircularProgress />
	</Box>
);

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

const SPLASH_MIN_MS = 5000;

const App: React.FC = () => {
	const theme = useTheme();
	const apiServices: IApiServiceContext = getApiServices();

	const isTauri = isTauriEnv() && !hasTauriBackendUrl();
	const [isBackendReady, setIsBackendReady] = useState(!isTauri);
	const [minTimeElapsed, setMinTimeElapsed] = useState(!isTauri);

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

		const initTauriListener = async () => {
			try {
				const { listen } = await import("@tauri-apps/api/event");
				unlistenFn = await listen<string>("backend-ready", (event) => {
					notifyBackendReady(event.payload);
					setIsBackendReady(true);
				});
			} catch (e) {
				console.error("Failed to initialize Tauri event listener:", e);
			}
		};

		initTauriListener();

		return () => unlistenFn?.();
	}, [isTauri]);

	const { shell, loginUrl, misconfigurationMessage, logout, currentUser } =
		useAuthGuard(apiServices.authService);

	const handleBlockedLicenseImported = () => {
		globalThis.location.reload();
	};

	if (!isBackendReady || !minTimeElapsed) {
		return <SplashScreen />;
	}

	if (shell === "loading") {
		// Server mode returns null (not a spinner) to avoid a flash before auth resolves
		return isTauri ? <SplashScreen /> : null;
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

	if (shell === "blocked") {
		return (
			<BlockedPage
				licensingService={apiServices.licensingService}
				onLicenseImported={handleBlockedLicenseImported}
				onLogout={logout}
			/>
		);
	}

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
								minHeight: "100vh",
								display: "flex",
								flexDirection: "column",
							}}
						>
							<CssBaseline />
							<Header
								isAuthenticated={shell === "authenticated"}
								currentUserDisplayName={currentUser?.displayName}
								onLogout={logout}
							/>
							<Box
								component="main"
								className="main-content"
								sx={{
									bgcolor: theme.palette.background.default,
									pt: 2,
									pb: 4,
									flex: 1,
								}}
							>
								<Suspense fallback={<RouteFallback />}>
									<Routes>
										<Route path="/" element={<OverviewDashboard />} />
										<Route
											path="/oauth/popup-complete"
											element={<OAuthPopupComplete />}
										/>
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
								</Suspense>
							</Box>
							<Footer />
							<SurveyNudge />
						</Box>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</Router>
		</QueryClientProvider>
	);
};

export default App;
