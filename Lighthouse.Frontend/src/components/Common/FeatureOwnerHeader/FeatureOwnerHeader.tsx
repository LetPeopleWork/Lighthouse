import { Typography } from "@mui/material";
import type React from "react";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";

interface FeatureOwnerHeaderProps {
	featureOwner: IFeatureOwner;
}

const FeatureOwnerHeader: React.FC<FeatureOwnerHeaderProps> = ({
	featureOwner,
}) => {
	return (
		<div>
			<Typography variant="h3">{featureOwner.name}</Typography>

			<Typography variant="h6">
				Last Updated on{" "}
				<LocalDateTimeDisplay
					utcDate={featureOwner.lastUpdated}
					showTime={true}
				/>
			</Typography>
		</div>
	);
};

export default FeatureOwnerHeader;
