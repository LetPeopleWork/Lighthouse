import { Box, CircularProgress } from "@mui/material";
import type React from "react";
import { lazy } from "react";
import { Navigate, Route, useLocation, useParams } from "react-router";
import { useRbac } from "../../hooks/useRbac";
import { resolveSettingsLocation } from "../../models/Settings/resolveSettingsLocation";
import { settingsTabPath } from "../../models/Settings/settingsAreas";

const SettingsAreaPage = lazy(() => import("./SettingsAreaPage"));

/** Asks the resolver what the address opens, and acts on the answer: wait, move on, or show the area. */
const SettingsAddress: React.FC = () => {
	const { area, tab } = useParams();
	const { search } = useLocation();
	const { isLoading, isSystemAdmin, isRbacEnabled } = useRbac();

	// The old page opened tabs by ?tab=; only the bare address carries it.
	const legacyKey =
		area === undefined
			? (new URLSearchParams(search).get("tab") ?? undefined)
			: undefined;

	const outcome = resolveSettingsLocation(
		{ area, tab, legacyKey },
		{ isLoading, isSystemAdmin, isRbacEnabled },
	);

	switch (outcome.kind) {
		case "wait":
			return (
				<Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
					<CircularProgress />
				</Box>
			);
		case "redirect":
			return <Navigate to={outcome.path} replace />;
		case "show":
			return <SettingsAreaPage area={outcome.area} tab={outcome.tab} />;
	}
};

/** The settings routes, to be placed inside a `Routes` or a layout route. */
export const settingsRoutes = (): React.ReactElement => (
	<>
		<Route path="/settings" element={<SettingsAddress />} />
		<Route
			path="/settings/license"
			element={<Navigate to={settingsTabPath("system", "license")} replace />}
		/>
		<Route path="/settings/:area/:tab?" element={<SettingsAddress />} />
	</>
);
