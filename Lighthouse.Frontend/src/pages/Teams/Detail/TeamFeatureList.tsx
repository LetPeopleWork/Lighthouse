import {
	Box,
	FormControlLabel,
	Paper,
	Switch,
	TableContainer,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useContext, useEffect, useMemo, useState } from "react";
import DataGridBase from "../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../components/Common/DataGrid/types";
import FeatureName from "../../../components/Common/FeatureName/FeatureName";
import ForecastInfoList from "../../../components/Common/Forecasts/ForecastInfoList";
import LocalDateTimeDisplay from "../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import ParentWorkItemCell from "../../../components/Common/ParentWorkItemCell/ParentWorkItemCell";
import ProgressIndicator from "../../../components/Common/ProgressIndicator/ProgressIndicator";
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import { useParentWorkItems } from "../../../hooks/useParentWorkItems";
import type { IFeature } from "../../../models/Feature";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";

interface FeatureListProps {
	team: Team;
}

const TeamFeatureList: React.FC<FeatureListProps> = ({ team }) => {
	const { teamMetricsService, featureService } = useContext(ApiServiceContext);

	const [featuresInProgress, setFeaturesInProgress] = useState<string[]>([]);
	const [features, setFeatures] = useState<IFeature[]>([]);
	const [hideCompletedFeatures, setHideCompletedFeatures] =
		useState<boolean>(false);

	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);

	const parentMap = useParentWorkItems(features);

	// Storage key for toggle
	const storageKey = `lighthouse_hide_completed_features_team_${team.id}`;

	// Load features
	useEffect(() => {
		const fetchFeatures = async () => {
			const featureIds = team.features.map((fr) => fr.id);
			const featureData = await featureService.getFeaturesByIds(featureIds);
			setFeatures(featureData);
		};

		fetchFeatures();
	}, [team.features, featureService]);

	// Load toggle preference from localStorage
	useEffect(() => {
		const storedPreference = localStorage.getItem(storageKey);
		if (storedPreference !== null) {
			setHideCompletedFeatures(storedPreference === "true");
		}
	}, [storageKey]);

	// Fetch features in progress
	useEffect(() => {
		const fetchFeaturesInProgress = async () => {
			const features = await teamMetricsService.getFeaturesInProgress(team.id);
			setFeaturesInProgress(features.map((feature) => feature.referenceId));
		};

		fetchFeaturesInProgress();
	}, [team, teamMetricsService]);

	const handleToggleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		const newValue = event.target.checked;
		setHideCompletedFeatures(newValue);
		localStorage.setItem(storageKey, newValue.toString());
	};

	// Filter features based on the "hide completed" setting
	const filteredFeatures = useMemo(() => {
		return hideCompletedFeatures
			? features.filter((feature) => feature.stateCategory !== "Done")
			: features;
	}, [features, hideCompletedFeatures]);

	// Define columns
	const columns: DataGridColumn<IFeature & GridValidRowModel>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: `${featureTerm} Name`,
				width: 300,
				flex: 1,
				hideable: false,
				renderCell: ({ row }) => (
					<FeatureName
						name={getWorkItemName(row)}
						url={row.url ?? ""}
						stateCategory={row.stateCategory}
						isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
						teamsWorkIngOnFeature={
							featuresInProgress.includes(row.referenceId) ? [team] : []
						}
					/>
				),
			},
			{
				field: "progress",
				headerName: "Progress",
				width: 400,
				sortable: false,
				renderCell: ({ row }) => (
					<Box sx={{ width: "100%" }}>
						<ProgressIndicator
							title="Total"
							progressableItem={{
								remainingWork: row.getRemainingWorkForFeature(),
								totalWork: row.getTotalWorkForFeature(),
							}}
						/>
						<ProgressIndicator
							title={team.name}
							progressableItem={{
								remainingWork: row.getRemainingWorkForTeam(team.id),
								totalWork: row.getTotalWorkForTeam(team.id),
							}}
						/>
					</Box>
				),
			},
			{
				field: "forecasts",
				headerName: "Forecasts",
				width: 200,
				sortable: false,
				renderCell: ({ row }) => (
					<ForecastInfoList title={""} forecasts={row.forecasts} />
				),
			},
			{
				field: "parent",
				headerName: "Parent",
				width: 300,
				sortable: false,
				renderCell: ({ row }) => (
					<ParentWorkItemCell
						parentReference={row.parentWorkItemReference}
						parentMap={parentMap}
					/>
				),
			},
			{
				field: "projects",
				headerName: portfoliosTerm,
				width: 200,
				sortable: false,
				renderCell: ({ row }) => (
					<Box>
						{row.projects.map((project) => (
							<Box key={project.id}>
								<StyledLink to={`/projects/${project.id}`}>
									{project.name}
								</StyledLink>
							</Box>
						))}
					</Box>
				),
			},
			{
				field: "lastUpdated",
				headerName: "Updated On",
				width: 200,
				type: "dateTime",
				valueGetter: (value: Date | string) => {
					return value instanceof Date ? value : new Date(value);
				},
				renderCell: ({ row }) => (
					<LocalDateTimeDisplay utcDate={row.lastUpdated} showTime={true} />
				),
			},
		],
		[featureTerm, team, featuresInProgress, parentMap, portfoliosTerm],
	);

	return (
		<TableContainer component={Paper}>
			<Box sx={{ display: "flex", justifyContent: "flex-end", p: 2, gap: 2 }}>
				<FormControlLabel
					control={
						<Switch
							checked={hideCompletedFeatures}
							onChange={handleToggleChange}
							color="primary"
							data-testid="hide-completed-features-toggle"
						/>
					}
					label={`Hide Completed ${featuresTerm}`}
				/>
			</Box>
			<DataGridBase
				rows={filteredFeatures as (IFeature & GridValidRowModel)[]}
				columns={columns}
				storageKey={`team-features-${team.id}`}
				loading={features.length === 0}
			/>
		</TableContainer>
	);
};

export default TeamFeatureList;
