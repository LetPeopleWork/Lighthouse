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
import type { IPortfolio } from "../../../models/Project/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";

interface ProjectFeatureListProps {
	project: IPortfolio;
}

const ProjectFeatureList: React.FC<ProjectFeatureListProps> = ({ project }) => {
	const { teamMetricsService, featureService } = useContext(ApiServiceContext);

	const [featuresInProgress, setFeaturesInProgress] = useState<
		Record<string, string[]>
	>({});
	const [features, setFeatures] = useState<IFeature[]>([]);
	const [hideCompletedFeatures, setHideCompletedFeatures] =
		useState<boolean>(false);

	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	const parentMap = useParentWorkItems(features);

	// Storage key for toggle
	const storageKey = `lighthouse_hide_completed_features_project_${project.id}`;

	// Load features
	useEffect(() => {
		const fetchFeatures = async () => {
			const featureIds = project.features.map((fr) => fr.id);
			const featureData = await featureService.getFeaturesByIds(featureIds);
			setFeatures(featureData);
		};

		fetchFeatures();
	}, [project.features, featureService]);

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
			const featuresByTeam: Record<string, string[]> = {};

			for (const team of project.involvedTeams) {
				try {
					const features = await teamMetricsService.getFeaturesInProgress(
						team.id,
					);
					featuresByTeam[team.id] = features.map(
						(feature) => feature.referenceId,
					);
				} catch (error) {
					console.error(`Failed to fetch features for team ${team.id}:`, error);
					featuresByTeam[team.id] = [];
				}
			}

			setFeaturesInProgress(featuresByTeam);
		};

		fetchFeaturesInProgress();
	}, [project.involvedTeams, teamMetricsService]);

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
	const columns: DataGridColumn<IFeature & GridValidRowModel>[] =
		useMemo(() => {
			const baseColumns: DataGridColumn<IFeature & GridValidRowModel>[] = [
				{
					field: "name",
					headerName: `${featureTerm} Name`,
					hideable: false,
					width: 300,
					flex: 1,
					renderCell: ({ row }) => (
						<FeatureName
							name={getWorkItemName(row)}
							url={row.url ?? ""}
							stateCategory={row.stateCategory}
							isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
							teamsWorkIngOnFeature={project.involvedTeams.filter((team) =>
								featuresInProgress[team.id]?.includes(row.referenceId),
							)}
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
								title="Overall Progress"
								progressableItem={{
									remainingWork: row.getRemainingWorkForFeature(),
									totalWork: row.getTotalWorkForFeature(),
								}}
							/>
							{project.involvedTeams
								.filter((team) => row.getTotalWorkForTeam(team.id) > 0)
								.map((team) => (
									<Box key={team.id}>
										<ProgressIndicator
											title={
												<StyledLink to={`/teams/${team.id}`}>
													{team.name}
												</StyledLink>
											}
											progressableItem={{
												remainingWork: row.getRemainingWorkForTeam(team.id),
												totalWork: row.getTotalWorkForTeam(team.id),
											}}
										/>
									</Box>
								))}
						</Box>
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
					field: "forecasts",
					headerName: "Forecasts",
					width: 200,
					sortable: false,
					renderCell: ({ row }) => (
						<ForecastInfoList title={""} forecasts={row.forecasts} />
					),
				},
			];

			// Add Updated On column
			baseColumns.push({
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
			});
			return baseColumns;
		}, [featureTerm, project.involvedTeams, featuresInProgress, parentMap]);

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
				storageKey={`portfolio-features-${project.id}`}
				loading={features.length === 0}
			/>
		</TableContainer>
	);
};

export default ProjectFeatureList;
