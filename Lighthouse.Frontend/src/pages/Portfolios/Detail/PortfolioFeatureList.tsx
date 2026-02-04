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
import ParentWorkItemCell from "../../../components/Common/ParentWorkItemCell/ParentWorkItemCell";
import ProgressIndicator from "../../../components/Common/ProgressIndicator/ProgressIndicator";
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import { useParentWorkItems } from "../../../hooks/useParentWorkItems";
import type { IFeature } from "../../../models/Feature";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";

interface PortfolioFeatureListProps {
	portfolio: IPortfolio;
}

const PortfolioFeatureList: React.FC<PortfolioFeatureListProps> = ({
	portfolio,
}) => {
	const { teamMetricsService, featureService } = useContext(ApiServiceContext);

	const [featuresInProgress, setFeaturesInProgress] = useState<
		Record<string, string[]>
	>({});
	const [features, setFeatures] = useState<IFeature[]>([]);
	const [hideCompletedFeatures, setHideCompletedFeatures] =
		useState<boolean>(true);

	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	const parentMap = useParentWorkItems(features);

	// Storage key for toggle
	const storageKey = `lighthouse_hide_completed_features_portfolio_${portfolio.id}`;

	// Load features
	useEffect(() => {
		const fetchFeatures = async () => {
			const featureIds = portfolio.features.map((fr) => fr.id);
			const featureData = await featureService.getFeaturesByIds(featureIds);
			setFeatures(featureData);
		};

		fetchFeatures();
	}, [portfolio.features, featureService]);

	// Load toggle preference from localStorage
	useEffect(() => {
		const storedPreference = localStorage.getItem(storageKey);
		if (storedPreference === null) {
			// Set default value in localStorage if not present
			localStorage.setItem(storageKey, "true");
		} else {
			setHideCompletedFeatures(storedPreference === "true");
		}
	}, [storageKey]);

	// Fetch features in progress
	useEffect(() => {
		const fetchFeaturesInProgress = async () => {
			const featuresByTeam: Record<string, string[]> = {};

			for (const team of portfolio.involvedTeams) {
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
	}, [portfolio.involvedTeams, teamMetricsService]);

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
							isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
							teamsWorkIngOnFeature={portfolio.involvedTeams.filter((team) =>
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
							{portfolio.involvedTeams
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
				{
					field: "state",
					headerName: "State",
					width: 150,
					sortable: true,
					renderCell: ({ row }) => {
						return <span>{row.state}</span>;
					},
				},
			];
			return baseColumns;
		}, [featureTerm, portfolio.involvedTeams, featuresInProgress, parentMap]);

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
				storageKey={`portfolio-features-${portfolio.id}`}
				loading={features.length === 0}
			/>
		</TableContainer>
	);
};

export default PortfolioFeatureList;
