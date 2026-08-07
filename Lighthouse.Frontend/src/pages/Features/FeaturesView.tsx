import { Container, Typography } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { DataGridColumn } from "../../components/Common/DataGrid/types";
import {
	createForecastsColumn,
	createNameColumn,
	createStateColumn,
} from "../../components/Common/FeatureListDataGrid/columns";
import FeatureListDataGrid from "../../components/Common/FeatureListDataGrid/FeatureListDataGrid";
import type { IFeature } from "../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import { useTerminology } from "../../services/TerminologyContext";

const FeaturesView: React.FC = () => {
	const { featureService } = useContext(ApiServiceContext);

	const [features, setFeatures] = useState<IFeature[]>([]);
	const [isLoading, setIsLoading] = useState(true);

	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);

	const fetchFeatures = useCallback(async () => {
		const featureData = await featureService.getAllFeatures();
		setFeatures(featureData);
		setIsLoading(false);
	}, [featureService]);

	useEffect(() => {
		fetchFeatures();
	}, [fetchFeatures]);

	const columns: DataGridColumn<IFeature & GridValidRowModel>[] = useMemo(
		() => [
			createNameColumn(featureTerm),
			{
				field: "projects",
				headerName: portfoliosTerm,
				width: 250,
				sortable: false,
				renderCell: ({ row }) => (
					<span>{row.projects.map((project) => project.name).join(", ")}</span>
				),
			},
			createForecastsColumn(),
			createStateColumn(),
		],
		[featureTerm, portfoliosTerm],
	);

	return (
		<Container maxWidth={false}>
			<Typography variant="h4" sx={{ mb: 1 }}>
				{featuresTerm}
			</Typography>
			<Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
				{`Lighthouse forecasts ${featuresTerm} in this order.`}
			</Typography>
			<FeatureListDataGrid
				features={features}
				columns={columns}
				storageKey="all-features"
				hideCompletedStorageKey="lighthouse_hide_completed_features_all"
				loading={isLoading}
				emptyStateMessage={`No ${featuresTerm} found`}
				showPosition
				// A move renumbers the whole instance, so the places every row shows are re-read, not patched.
				onOrderChanged={fetchFeatures}
			/>
		</Container>
	);
};

export default FeaturesView;
