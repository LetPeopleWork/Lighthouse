import { Link } from "@mui/material";
import type React from "react";

type FeatureNameProps = {
	name: string;
	url: string;
};

const FeatureName: React.FC<FeatureNameProps> = ({ name, url }) => {
	return (
		<span>
			{url ? (
				<Link
					href={url}
					target="_blank"
					rel="noopener noreferrer"
					sx={{
						textDecoration: "none",
						color: (theme) => theme.palette.primary.main,
						fontWeight: 500,
						"&:hover": {
							textDecoration: "underline",
							opacity: 0.9,
						},
					}}
				>
					{name}
				</Link>
			) : (
				name
			)}
		</span>
	);
};

export default FeatureName;
