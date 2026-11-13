import { Box, CircularProgress } from "@mui/material";
import type React from "react";
import { lazy } from "react";
import { Navigate, Route, useLocation, useParams } from "react-router";
import { useRbac } from "../../hooks/useRbac";
import { resolveSettingsLocation } from "../../models/Settings/resolveSettingsLocation";
import {
	EDIT_CONNECTION_ROUTE,
	editConnectionPath,
	NEW_CONNECTION_ROUTE,
	newConnectionPath,
	settingsTabPath,
} from "../../models/Settings/settingsAreas";

const SettingsAreaPage = lazy(() => import("./SettingsAreaPage"));
const EditConnection = lazy(() => import("../Connections/Edit/EditConnection"));

/** An old bookmark to a connection's edit page keeps its connection. */
const OldEditConnectionAddress: React.FC = () => {
	const { id } = useParams();
	return (
		<Navigate to={editConnectionPath(Number.parseInt(id ?? "", 10))} replace />
	);
};

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
		<Route path={NEW_CONNECTION_ROUTE} element={<EditConnection />} />
		<Route path={EDIT_CONNECTION_ROUTE} element={<EditConnection />} />
		<Route
			path="/connections/new"
			element={<Navigate to={newConnectionPath()} replace />}
		/>
		<Route
			path="/connections/:id/edit"
			element={<OldEditConnectionAddress />}
		/>
		<Route path="/settings/:area/:tab?" element={<SettingsAddress />} />
	</>
);
