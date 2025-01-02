import {
	Paper,
	Switch,
	Table,
	TableBody,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
} from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { IPreviewFeature } from "../../../models/Preview/PreviewFeature";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const PreviewFeaturesTab: React.FC = () => {
	const [previewFeatures, setPreviewFeatures] = useState<IPreviewFeature[]>([]);

	const { previewFeatureService } = useContext(ApiServiceContext);

	const onToggle = async (toggledFeature: IPreviewFeature) => {
		const updatedFeatures = previewFeatures.map((feature) =>
			feature.id === toggledFeature.id
				? { ...feature, enabled: !feature.enabled }
				: feature,
		);
		setPreviewFeatures(updatedFeatures);

		await previewFeatureService.updateFeature({
			...toggledFeature,
			enabled: !toggledFeature.enabled,
		});
	};

	useEffect(() => {
		const fetchData = async () => {
			const previewFeatureData = await previewFeatureService.getAllFeatures();
			if (previewFeatureData) {
				setPreviewFeatures(previewFeatureData);
			}
		};

		fetchData();
	}, [previewFeatureService]);

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
					{previewFeatures.map((feature) => (
						<TableRow key={feature.id}>
							<TableCell>{feature.name}</TableCell>
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

export default PreviewFeaturesTab;
