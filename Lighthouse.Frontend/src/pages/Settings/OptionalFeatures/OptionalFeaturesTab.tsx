import BiotechIcon from "@mui/icons-material/Biotech";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import Paper from "@mui/material/Paper";
import Switch from "@mui/material/Switch";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Tooltip from "@mui/material/Tooltip";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { IOptionalFeature } from "../../../models/OptionalFeatures/OptionalFeature";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const OptionalFeaturesTab: React.FC = () => {
	const [optionalFeatures, setOptionalFeatures] = useState<IOptionalFeature[]>(
		[],
	);

	const { optionalFeatureService } = useContext(ApiServiceContext);

	const onToggle = async (toggledFeature: IOptionalFeature) => {
		const updatedFeatures = optionalFeatures.map((feature) =>
			feature.id === toggledFeature.id
				? { ...feature, enabled: !feature.enabled }
				: feature,
		);
		setOptionalFeatures(updatedFeatures);

		await optionalFeatureService.updateFeature({
			...toggledFeature,
			enabled: !toggledFeature.enabled,
		});
	};

	useEffect(() => {
		const fetchData = async () => {
			const optionalFeatureData = await optionalFeatureService.getAllFeatures();
			if (optionalFeatureData) {
				setOptionalFeatures(optionalFeatureData);
			}
		};

		fetchData();
	}, [optionalFeatureService]);

	return (
		<TableContainer component={Paper}>
			<Table>
				<TableHead>
					<TableRow>
						<TableCell>Name</TableCell>
						<TableCell>Description</TableCell>
						<TableCell>Enabled</TableCell>
					</TableRow>
				</TableHead>
				<TableBody>
					{optionalFeatures.map((feature) => (
						<TableRow key={feature.id}>
							<TableCell>
								<Box sx={{ display: "flex", alignItems: "center" }}>
									{feature.name}
									{feature.isPreview && (
										<Tooltip title="This feature is in preview and may change or be removed in future versions">
											<Chip
												icon={<BiotechIcon />}
												label="Preview"
												size="small"
												color="warning"
												sx={{ ml: 1 }}
												data-testid={`${feature.key}-preview-indicator`}
											/>
										</Tooltip>
									)}
								</Box>
							</TableCell>
							<TableCell>{feature.description}</TableCell>
							<TableCell>
								<Switch
									checked={feature.enabled}
									data-testid={`${feature.key}-toggle`}
									onChange={() => onToggle(feature)}
									color="primary"
								/>
							</TableCell>
						</TableRow>
					))}
				</TableBody>
			</Table>
		</TableContainer>
	);
};

export default OptionalFeaturesTab;
